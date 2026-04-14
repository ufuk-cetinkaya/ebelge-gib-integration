using Application.Contracts;
using Application.DTOs;
using Application.DTOs.UBL;
using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using SelectPdf;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _docRepo;

    public DocumentService(IDocumentRepository docRepo)
    {
        _docRepo = docRepo;
    }

    public async Task LoadDocument(byte[] content)
    {
        ValidateSchema(content);
        DocumentTypes packageType = GetPackageType(content);
        Document document = CreateDocument(packageType, content);
        int count = await _docRepo.Count(document.SupplierIdentifier, document.DocumentId, document.Uuid);
        if (count > 0) throw new DocumentException("Belge sistemde mevcut.");
        document.Type = packageType;
        document.Content = content;
        document.CreatedAt = DateTime.Now;
        document.Status = Status.LOAD;
        document.SubStatus = SubStatus.SUCCEED;
        document.Direction = Direction.OUT;
        await _docRepo.Add(document);
        await _docRepo.SaveChanges();
    }

    public async Task CancelDocument(Guid uuid)
    {
        Document? document = await _docRepo.GetDocToCancel(uuid);
        if (document == null)
        {
            throw new DocumentException("İptal edilecek belge bulunamadı.");
        }
        else
        {
            document.CancelFlag = true;
            document.CancelDate = DateTime.Now;
            document.Status = Status.SIGN;
            document.SubStatus = SubStatus.SUCCEED;
            document.CancelReportId = document.ReportId;
            document.ReportId = null;
            await _docRepo.SaveChanges();
        }
    }

    public async Task<Page<DocumentDto>> GetDocuments(DocumentFilter filter)
    {
        int recordCount = await _docRepo
           .GetDocumentCount(filter.StartDate,
           filter.EndDate,
           filter.DocumentType,
           filter.Direction);

        Page<DocumentDto> page = new(recordCount, filter.PageSize, filter.Page);

        List<Document> documents = await _docRepo
            .GetDocuments(filter.StartDate,
            filter.EndDate,
            filter.DocumentType,
            filter.Direction,
            page.Skip,
            page.Fetch);

        List<DocumentDto> dto = [];
        foreach (Document document in documents)
        {
            dto.Add(Mapper.Map(document));
        }
        page.Data = dto;
        return page;
    }

    public async Task<byte[]> GetXmlContent(Guid uuid)
    {
        return await GetContent(uuid);
    }

    public async Task<byte[]> GetHtmlContent(Guid uuid)
    {
        byte[] xml = await GetContent(uuid);
        byte[] xslt = ExtractXslt(xml);
        byte[] html = XmlToHtml(xml, xslt);
        return html;
    }

    public async Task<byte[]> GetPdfContent(Guid uuid)
    {
        byte[] xml = await GetContent(uuid);
        byte[] xslt = ExtractXslt(xml);
        byte[] html = XmlToHtml(xml, xslt);
        byte[] pdf = HtmlToPdf(html);
        return pdf;
    }

    private async Task<byte[]> GetContent(Guid uuid)
    {
        return await _docRepo.GetContent(uuid) ??
            throw new DocumentException("Belge içeriği alınamadı.");
    }

    private static byte[] ExtractXslt(byte[] bytes)
    {
        MemoryStream ms = new(bytes);
        XmlDocument xml = new();
        xml.Load(ms);
        XmlNamespaceManager ns = new(xml.NameTable);
        ns.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        ns.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
        string xpath = "//cac:AdditionalDocumentReference[cbc:DocumentType='XSLT']/cac:Attachment/cbc:EmbeddedDocumentBinaryObject";
        string? xslt = xml.SelectSingleNode(xpath, ns)?.InnerText ??
                throw new DocumentException("Xslt alınamadı.");
        return Convert.FromBase64String(xslt);
    }

    private static byte[] XmlToHtml(byte[] xmlBytes, byte[] xsltBytes)
    {
        XslCompiledTransform xslCompiledTransform = new();
        using MemoryStream input = new(xsltBytes);
        using XmlReader stylesheet = XmlReader.Create(input);
        xslCompiledTransform.Load(stylesheet);
        using MemoryStream input2 = new(xmlBytes);
        using XmlReader input3 = XmlReader.Create(input2);
        using MemoryStream memoryStream = new();
        using XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xslCompiledTransform.OutputSettings);
        xslCompiledTransform.Transform(input3, xmlWriter);
        xmlWriter.Flush();
        return memoryStream.ToArray();
    }

    private static byte[] HtmlToPdf(byte[] htmlBytes)
    {
        HtmlToPdf html = new();
        string text = Encoding.UTF8.GetString(htmlBytes);
        PdfDocument pdf = html.ConvertHtmlString(text);
        using MemoryStream memoryStream = new();
        pdf.Save(memoryStream);
        pdf.Close();
        return memoryStream.ToArray();
    }

    private static DocumentTypes GetPackageType(byte[] content)
    {
        XmlDocument xml = Util.BytesToXml(content);
        string docType = xml.DocumentElement?.LocalName
            ?? throw new DocumentException("Belge tipi alınamadı.");
        return Enum.Parse<DocumentTypes>(docType, true);
    }

    private static Document CreateDocument(DocumentTypes packageType, byte[] content)
    {
        return packageType switch
        {
            DocumentTypes.INVOICE => Mapper.Map(Util.Deserialize<InvoiceType>(content)),
            DocumentTypes.APPLICATIONRESPONSE => Mapper.Map(Util.Deserialize<ApplicationResponseType>(content)),
            DocumentTypes.DESPATCHADVICE => Mapper.Map(Util.Deserialize<DespatchAdviceType>(content)),
            DocumentTypes.RECEIPTADVICE => Mapper.Map(Util.Deserialize<ReceiptAdviceType>(content)),
            DocumentTypes.CREDITNOTE => Mapper.Map(Util.Deserialize<CreditNoteType>(content)),
            _ => throw new DocumentException("Geçersiz belge tipi.")
        };
    }

    private static void ValidateSchema(byte[] content)
    {
        try
        {
            Util.ValidateSchema(content, "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader", "xsd/Envelope/PackageProxy_1_2.xsd");
        }
        catch (Exception ex)
        {
            throw new DocumentException($"XML SEMA KONTROLUNDEN GECEMEDI({ex.Message})");
        }
    }
}
