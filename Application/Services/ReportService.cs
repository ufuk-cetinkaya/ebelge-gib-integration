using Application.Contracts;
using Application.DTOs;
using Application.DTOs.EArsiv;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using EArsivWsService;
using Microsoft.Extensions.Options;
using SignerWs;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Application.Services;

public class ReportService: IReportService
{
    private readonly IReportRepository _reportRepo;
    private readonly IDocumentRepository _docRepo;
    private readonly EArsivWs _gibClient;
    private readonly ISignerWs _signerClient;
    private readonly string _documentApiUrl;
    private readonly string _entegretorVkn;

    public ReportService(IReportRepository reportRepo,
        IDocumentRepository docRepo,
        EArsivWs gibClient,
        ISignerWs signerClient,
        IOptions<ServiceConfig> options)
    {
        _reportRepo = reportRepo;
        _docRepo = docRepo;
        _gibClient = gibClient;
        _signerClient = signerClient;
        _documentApiUrl = options.Value.DocumentApiUrl;
        _entegretorVkn = options.Value.EntegratorVkn;
    }

    public async Task LoadReport()
    {
        DateTime minDate = await _docRepo.GetMinDoc();
        if (minDate == DateTime.MinValue) return;
        DateTime donemBaslangic = new(minDate.Year, minDate.Month, 1);
        DateTime donemBitis = donemBaslangic.AddMonths(1).AddDays(-1);
        List<string> suppliers = await _docRepo.GetSuppliers();
        foreach (string supplier in suppliers)
        {
            List<Document> docs = await _docRepo.GetSignedDocs(supplier, donemBaslangic, donemBitis);
            if (docs.Count == 0) continue;
            DateTime bolumBaslangic = docs.Min(d => d.IssueDate);
            DateTime bolumBitis = docs.Max(d => d.IssueDate);
            int bolumNo = await _reportRepo.GetMaxBolumNo(supplier, donemBaslangic, donemBitis) + 1;
            Report report = new()
            {
                Hazirlayan = _entegretorVkn,
                Mukellef = supplier,
                Status = Status.LOAD,
                SubStatus = SubStatus.SUCCEED,
                DonemBaslangic = donemBaslangic,
                DonemBitis = donemBitis,
                BolumBaslangic = bolumBaslangic,
                BolumBitis = bolumBitis,
                BelgeSayisi = docs.Count,
                BolumNo = bolumNo,
                CreatedAt = DateTime.Now,
                RaporNo = Guid.NewGuid().ToString(),
                Content = [],
                Documents = docs
            };
            await _reportRepo.Add(report);
            await _docRepo.SaveChanges();
        }
    }

    public async Task PackageReport()
    {
        List<Report> reports = await _reportRepo.GetReportsByStatus(Status.LOAD, SubStatus.SUCCEED);
        await ToProcessing(reports, Status.PACKAGE);
        foreach (Report report in reports)
        {
            try
            {
                eArsivRaporu rapor = new()
                {
                    baslik = Baslik(report),
                    ItemsElementName = new ItemsChoiceType3[report.BelgeSayisi],
                    Items = new object[report.BelgeSayisi]
                };
                List<Document> docs = await _docRepo.GetDocsByReportId(report.Id);
                int i = 0;
                foreach (Document doc in docs)
                {
                    string sha256 = Convert.ToHexString(SHA256.HashData(doc.Content));
                    rapor.ItemsElementName[i] = ItemType(doc.Type, doc.ProfileId, doc.CancelFlag);
                    if (doc.Type == DocumentTypes.INVOICE)
                    {
                        DTOs.UBL.InvoiceType ubl = Util.Deserialize<DTOs.UBL.InvoiceType>(doc.Content);
                        if (rapor.ItemsElementName[i] == ItemsChoiceType3.fatura)
                            rapor.Items[i] = Fatura(doc.SigningTime, sha256, ubl);

                        else if (rapor.ItemsElementName[i] == ItemsChoiceType3.faturaIptal)
                            rapor.Items[i] = FaturaIptal(ubl.ID.Value, doc.CancelDate, ubl.LegalMonetaryTotal.LineExtensionAmount.Value);

                        else if (rapor.ItemsElementName[i] == ItemsChoiceType3.serbestMeslekMakbuz)
                            rapor.Items[i] = Smm(doc.SigningTime, sha256, ubl);

                        else if (rapor.ItemsElementName[i] == ItemsChoiceType3.serbestMeslekMakbuzIptal)
                            rapor.Items[i] = SmmIptal(ubl.ID.Value, doc.CancelDate, ubl.LegalMonetaryTotal.LineExtensionAmount.Value);
                    }
                    else if (doc.Type == DocumentTypes.CREDITNOTE)
                    {
                        DTOs.UBL.CreditNoteType ubl = Util.Deserialize<DTOs.UBL.CreditNoteType>(doc.Content);
                        if (rapor.ItemsElementName[i] == ItemsChoiceType3.mustahsilMakbuz)
                            rapor.Items[i] = Mustahsil(doc.SigningTime, sha256, ubl);

                        else if (rapor.ItemsElementName[i] == ItemsChoiceType3.mustahsilMakbuzIptal)
                            rapor.Items[i] = MustahsilIptal(ubl.ID.Value, doc.CancelDate, ubl.LegalMonetaryTotal.LineExtensionAmount.Value);
                    }
                    doc.Status = Status.PACKAGE;
                    doc.SubStatus = SubStatus.SUCCEED;
                    i++;
                }
                XmlDocument xml = Util.Serialize(rapor);
                report.Content = Encoding.UTF8.GetBytes(xml.OuterXml);
                report.SubStatus = SubStatus.SUCCEED;
            }
            catch (System.Exception ex)
            {
                report.SubStatus = SubStatus.FAILED;
                report.ErrorDesc = ex.Message.Length > 255 ? ex.Message[..255] : ex.Message;
            }
            report.UpdatedAt = DateTime.Now;
            await _reportRepo.SaveChanges();
        }
    }

    public async Task SignReport()
    {
        List<Report> reports = await _reportRepo.GetReportsByStatus(Status.PACKAGE, SubStatus.SUCCEED);
        await ToProcessing(reports, Status.SIGN);
        foreach (Report report in reports)
        {
            try
            {
                signReportRequest request = new();
                request.arg0 = report.Content;
                signReportResponse response = await _signerClient.signReportAsync(request);
                report.Content = response.@return;
                report.SubStatus = SubStatus.SUCCEED;
            }
            catch (System.Exception ex)
            {
                report.SubStatus = SubStatus.FAILED;
                report.ErrorDesc = ex.Message.Length > 255 ? ex.Message[..255] : ex.Message;
            }
            report.UpdatedAt = DateTime.Now;
            await _reportRepo.SaveChanges();
        }
    }

    public async Task SendReport()
    {
        List<Report> reports = await _reportRepo.GetReportsByStatus(Status.SIGN, SubStatus.SUCCEED);
        await ToProcessing(reports, Status.SEND);
        foreach (Report report in reports)
        {
            sendDocumentFile request = new();
            request.Attachment = new();
            try
            {
                request.Attachment.fileName = $"{report.RaporNo}.zip";
                request.Attachment.binaryData = Util.Zip(report.Content, $"{report.RaporNo}.xml");
                sendDocumentFileResponse response = await _gibClient.sendDocumentFileAsync(request);
                if (response.@return == "Dosya Kaydedildi")
                    report.SubStatus = SubStatus.WAIT_GIB_RESPONSE;
                else
                    report.SubStatus = SubStatus.FAILED;
                report.ResponseDesc = response.@return;
            }
            catch (System.Exception ex)
            {
                report.SubStatus = SubStatus.FAILED;
                report.ErrorDesc = ex.Message.Length > 255 ? ex.Message[..255] : ex.Message;
            }
            report.UpdatedAt = DateTime.Now;
            await _reportRepo.SaveChanges();
        }
    }

    public async Task CheckStatus()
    {
        List<Report> reports = await _reportRepo.GetReportsByStatus(Status.SEND, SubStatus.WAIT_GIB_RESPONSE);
        foreach (Report report in reports)
        {
            try
            {
                getBatchStatus request = new();
                request.paketId = report.RaporNo;
                getBatchStatusResponse response = await _gibClient.getBatchStatusAsync(request);
                int code = response.@return.durumKodu;
                if (code == 30)
                    report.SubStatus = SubStatus.SUCCEED;
                else if (code == 10 || code == 15)
                    report.SubStatus = SubStatus.WAIT_GIB_RESPONSE;
                else
                    report.SubStatus = SubStatus.FAILED;
                report.ResponseCode = code;
                report.ResponseDesc = response.@return.durumAciklama;
            }
            catch (System.Exception ex)
            {
                report.SubStatus = SubStatus.FAILED;
                report.ErrorDesc = ex.Message.Length > 255 ? ex.Message[..255] : ex.Message;
            }
            report.UpdatedAt = DateTime.Now;
            await _reportRepo.SaveChanges();
        }
    }

    private async Task ToProcessing(List<Report> reports, Status status)
    {
        foreach (Report report in reports)
        {
            report.Status = status;
            report.SubStatus = SubStatus.PROCESSING;
            report.UpdatedAt = DateTime.Now;
        }
        await _reportRepo.SaveChanges();
    }

    private eArsivRaporuFatura Fatura(DateTime? imzaZamani, string hash, DTOs.UBL.InvoiceType ubl)
    {
        return new eArsivRaporuFatura
        {
            faturaNo = ubl.ID.Value,
            faturaUUID = ubl.UUID.Value,
            faturaTip = Enum.Parse<faturaTipEnum>(ubl.InvoiceTypeCode.Value),
            faturaUrl = $"{_documentApiUrl}/preview/{ubl.UUID.Value}/xml",
            duzenlenmeTarihi = ubl.IssueDate.Value,
            duzenlenmeZamani = ubl.IssueTime.Value,
            gonderimSekli = ArsivSendingType(ubl.AdditionalDocumentReference),
            dosyaAdi = $"{ubl.UUID.Value}.xml",
            ozetDeger = hash,
            imzaZamani = Convert.ToDateTime(imzaZamani),
            paraBirimi = Enum.Parse<currencyCode>(ubl.DocumentCurrencyCode.Value),
            dovizKuru = ubl.PaymentExchangeRate?.CalculationRate.Value ?? 1,
            toplamTutar = ubl.LegalMonetaryTotal.LineExtensionAmount.Value,
            toplamIskonto = ubl.LegalMonetaryTotal.AllowanceTotalAmount.Value,
            odenecekTutar = ubl.LegalMonetaryTotal.PayableAmount.Value,
            vergiBilgisi = new vergiBilgisiType
            {
                vergilerToplami = ubl.TaxTotal[0].TaxAmount.Value,
                vergi = Vergi(ubl.TaxTotal[0].TaxSubtotal),
                tevkifat = Tevkifat(ubl.WithholdingTaxTotal?[0].TaxSubtotal)
            },
            aliciBilgileri = Alici(ubl.AccountingCustomerParty.Party),
            internetSatisBilgi = null,
            ynOkcFisBilgisi = null,
            sarj = null,
            sarjAnlik = null,
            ytbBilgileri = null
        };
    }

    private static eArsivRaporuFaturaIptal FaturaIptal(string faturaNo, DateTime? iptalTarihi, decimal toplamTutar)
    {
        return new eArsivRaporuFaturaIptal
        {
            faturaNo = faturaNo,
            iptalTarihi = Convert.ToDateTime(iptalTarihi),
            toplamTutar = toplamTutar
        };
    }

    private eArsivRaporuMustahsilMakbuz Mustahsil(DateTime? imzaZamani, string hash, DTOs.UBL.CreditNoteType ubl)
    {
        return new eArsivRaporuMustahsilMakbuz
        {
            makbuzNo = ubl.ID.Value,
            duzenlenmeTarihi = ubl.IssueDate.Value,
            duzenlenmeZamani = ubl.IssueTime.Value,
            mustahsilMakbuzUrl = $"{_documentApiUrl}/preview/{ubl.UUID.Value}/xml",
            dosyaAdi = $"{ubl.UUID.Value}.xml",
            ozetDeger = hash,
            imzaZamani = Convert.ToDateTime(imzaZamani),
            paraBirimi = Enum.Parse<currencyCode>(ubl.DocumentCurrencyCode.Value),
            toplamTutar = ubl.LegalMonetaryTotal.LineExtensionAmount.Value,
            odenecekTutar = ubl.LegalMonetaryTotal.PayableAmount.Value,
            vergiBilgisi = new vergiBilgisiType
            {
                tevkifat = null,
                vergi = Vergi(ubl.TaxTotal[0].TaxSubtotal),
                vergilerToplami = ubl.TaxTotal[0].TaxAmount.Value
            },
            mustahsilBilgileri = AliciMin(ubl.AccountingCustomerParty.Party),
            ynOkcFisBilgisi = null
        };
    }

    private static eArsivRaporuMustahsilMakbuzIptal MustahsilIptal(string makbuzNo, DateTime? iptalTarihi, decimal toplamTutar)
    {
        return new eArsivRaporuMustahsilMakbuzIptal
        {
            makbuzNo = makbuzNo,
            iptalTarihi = Convert.ToDateTime(iptalTarihi),
            toplamTutar = toplamTutar
        };
    }

    private eArsivRaporuSerbestMeslekMakbuz Smm(DateTime? imzaZamani, string hash, DTOs.UBL.InvoiceType ubl)
    {
        return new eArsivRaporuSerbestMeslekMakbuz
        {
            makbuzNo = ubl.ID.Value,
            gonderimSekli = SmmSendingType(ubl.AdditionalDocumentReference),
            duzenlenmeTarihi = ubl.IssueDate.Value,
            duzenlenmeZamani = ubl.IssueTime.Value,
            duzenlenmeZamaniSpecified = true,
            serbestMeslekMakbuzUrl = $"{_documentApiUrl}/preview/{ubl.UUID.Value}/xml",
            dosyaAdi = $"{ubl.UUID.Value}.xml",
            ozetDeger = hash,
            imzaZamani = Convert.ToDateTime(imzaZamani),
            paraBirimi = Enum.Parse<currencyCode>(ubl.DocumentCurrencyCode.Value),
            dovizKuru = ubl.PaymentExchangeRate?.CalculationRate.Value ?? 1,
            toplamTutar = ubl.LegalMonetaryTotal.LineExtensionAmount.Value,
            odenecekTutar = ubl.LegalMonetaryTotal.PayableAmount.Value,
            vergiBilgisi = new vergiBilgisiType
            {
                vergilerToplami = ubl.TaxTotal[0].TaxAmount.Value,
                vergi = Vergi(ubl.TaxTotal[0].TaxSubtotal),
                tevkifat = Tevkifat(ubl.WithholdingTaxTotal?[0].TaxSubtotal)
            },
            aliciBilgileri = AliciMin(ubl.AccountingCustomerParty.Party),
            ynOkcFisBilgisi = null
        };
    }

    private static eArsivRaporuSerbestMeslekMakbuzIptal SmmIptal(string makbuzNo, DateTime? iptalTarihi, decimal toplamTutar)
    {
        return new eArsivRaporuSerbestMeslekMakbuzIptal
        {
            makbuzNo = makbuzNo,
            iptalTarihi = Convert.ToDateTime(iptalTarihi),
            toplamTutar = toplamTutar
        };
    }

    private static ItemsChoiceType3 ItemType(DocumentTypes documentType, string profileId, bool cancelFlag)
    {
        ItemsChoiceType3 type = ItemsChoiceType3.fatura;
        if (cancelFlag)
        {
            if (documentType == DocumentTypes.INVOICE && profileId == "EARSIVFATURA")
                type = ItemsChoiceType3.faturaIptal;

            else if (documentType == DocumentTypes.INVOICE && profileId == "EARSIVBELGE")
                type = ItemsChoiceType3.serbestMeslekMakbuzIptal;

            else if (documentType == DocumentTypes.CREDITNOTE && profileId == "EARSIVBELGE")
                type = ItemsChoiceType3.mustahsilMakbuzIptal;
        }
        else
        {
            if (documentType == DocumentTypes.INVOICE && profileId == "EARSIVFATURA")
                type = ItemsChoiceType3.fatura;

            else if (documentType == DocumentTypes.INVOICE && profileId == "EARSIVBELGE")
                type = ItemsChoiceType3.serbestMeslekMakbuz;

            else if (documentType == DocumentTypes.CREDITNOTE && profileId == "EARSIVBELGE")
                type = ItemsChoiceType3.mustahsilMakbuz;
        }
        return type;
    }

    private static baslikType Baslik(Report report)
    {
        return new baslikType
        {
            versiyon = "1.0",
            hazirlayan = VknTckn(report.Hazirlayan),
            mukellef = VknTckn(report.Mukellef),
            raporNo = report.RaporNo,
            donemBaslangicTarihi = report.DonemBaslangic,
            donemBitisTarihi = report.DonemBitis,
            bolumBaslangicTarihi = report.BolumBaslangic,
            bolumBitisTarihi = report.BolumBitis,
            bolumNo = report.BolumNo,
            Signature = null
        };
    }

    private static eArsivRaporuFaturaGonderimSekli ArsivSendingType(DTOs.UBL.DocumentReferenceType[] docRef)
    {
        return Enum.Parse<eArsivRaporuFaturaGonderimSekli>(docRef.Single(a =>
            a.DocumentTypeCode?.Value == "SendingType").DocumentType.Value);
    }

    private static eArsivRaporuSerbestMeslekMakbuzGonderimSekli SmmSendingType(DTOs.UBL.DocumentReferenceType[] docRef)
    {
        return Enum.Parse<eArsivRaporuSerbestMeslekMakbuzGonderimSekli>(docRef.Single(a =>
            a.DocumentTypeCode?.Value == "SendingType").DocumentType.Value);
    }

    private static vknTcknType VknTckn(string vknTckn)
    {
        vknTcknType vknTcknType = new();
        vknTcknType.ItemElementName = ItemChoiceType.vkn;

        if (vknTckn.Length == 10)
            vknTcknType.ItemElementName = ItemChoiceType.vkn;

        else vknTcknType.ItemElementName = ItemChoiceType.tckn;

        vknTcknType.Item = vknTckn;
        return vknTcknType;
    }

    private static aliciType Alici(DTOs.UBL.PartyType party)
    {
        string vknTckn = party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.schemeID;
        aliciType alici = new();
        if (vknTckn == "VKN")
        {
            aliciTypeTuzelKisi tuzel = new();
            tuzel.vkn = party.PartyIdentification.Single(c => c.ID.schemeID == "VKN").ID.Value;
            tuzel.unvan = party.PartyName?.Name.Value;
            alici.tuzelKisi = tuzel;
        }
        else if(vknTckn == "TCKN")
        {
            aliciTypeGercekKisi gercek = new();
            gercek.tckn = party.PartyIdentification.Single(c => c.ID.schemeID == "TCKN").ID.Value;
            gercek.adiSoyadi = $"{party.Person.FirstName.Value} {party.Person.FamilyName.Value}";
            alici.gercekKisi = gercek;
        }
        return alici;
    }

    private static aliciTypeMin AliciMin(DTOs.UBL.PartyType party)
    {
        string vknTckn = party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.schemeID;
        aliciTypeMin alici = new();
        if (vknTckn == "VKN")
        {
            aliciTypeMinTuzelKisi tuzel = new();
            tuzel.vkn = party.PartyIdentification.Single(c => c.ID.schemeID == "VKN").ID.Value;
            tuzel.unvan = party.PartyName?.Name.Value;
            alici.tuzelKisi = tuzel;
        }
        else if (vknTckn == "TCKN")
        {
            aliciTypeMinGercekKisi gercek = new();
            gercek.tckn = party.PartyIdentification.Single(c => c.ID.schemeID == "TCKN").ID.Value;
            gercek.adiSoyadi = $"{party.Person?.FirstName.Value} {party.Person?.FamilyName.Value}";
            alici.gercekKisi = gercek;
        }
        return alici;
    }

    private static vergiBilgisiTypeVergi[] Vergi(DTOs.UBL.TaxSubtotalType[] taxsub)
    {
        vergiBilgisiTypeVergi[] vergi = new vergiBilgisiTypeVergi[taxsub.Length];
        for (int i = 0; i < taxsub.Length; i++)
        {
            vergi[i] = new vergiBilgisiTypeVergi
            {
                vergiKodu = Enum.Parse<vergiKodEnum>(taxsub[i].TaxCategory.TaxScheme.TaxTypeCode.Value),
                vergiOrani = taxsub[i].Percent.Value,
                matrah = taxsub[i].TaxableAmount.Value,
                vergiTutari = taxsub[i].TaxAmount.Value
            };
        }
        return vergi;
    }

    private static vergiBilgisiTypeTevkifat[]? Tevkifat(DTOs.UBL.TaxSubtotalType[]? taxsub)
    {
        vergiBilgisiTypeTevkifat[]? vergi = null;
        if (taxsub != null && taxsub.Length > 0)
        {
            vergi = new vergiBilgisiTypeTevkifat[taxsub.Length];
            for (int i = 0; i < taxsub.Length; i++)
            {
                vergi[i] = new vergiBilgisiTypeTevkifat
                {
                    tevkifatOrani = taxsub[i].Percent.Value,
                    tevkifatKodu = taxsub[i].TaxCategory.TaxScheme.TaxTypeCode.Value,
                    tevkifatTutari = taxsub[i].TaxAmount.Value,
                };
            }
        }
        return vergi;
    }
}
