using Application.Contracts;
using Application.DTOs;
using Application.DTOs.UBL;
using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using EFaturaWsService;
using SignerWs;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Application.Services;

public class EnvelopeService : IEnvelopeService
{
    private readonly IEnvelopeRepository _envRepo;
    private readonly IDocumentRepository _docRepo;
    private readonly EFaturaPortType _gibClient;
    private readonly IGibUserClient _gibUserClient;
    private readonly ISignerWs _signerClient;

    public EnvelopeService(IEnvelopeRepository envRepo,
        IDocumentRepository docRepo,
        EFaturaPortType gibClient,
        IGibUserClient gibUserClient,
        ISignerWs signerClient)
    {
        _envRepo = envRepo;
        _docRepo = docRepo;
        _gibClient = gibClient;
        _gibUserClient = gibUserClient;
        _signerClient = signerClient;
    }

    public async Task ExtractEnvelope()
    {
        List<Envelope> envelopes = await _envRepo.GetReceivedEnv();
        await ToProcessing(envelopes, Status.RECEIVE);
        foreach (Envelope envelope in envelopes)
        {
            try
            {
                ValidateSchema(envelope.Content);
                StandardBusinessDocument sbd = Util.Deserialize<StandardBusinessDocument>(envelope.Content);
                Package package = Util.Deserialize<Package>(Encoding.UTF8.GetBytes(sbd.Any.OuterXml));
                DocumentTypes packageType = Enum.Parse<DocumentTypes>(package.Elements[0].ElementType);
                envelope.PackageType = packageType;
                List <Document> documents = GetDocuments(packageType, package);
                if (envelope.Type == EnvelopeType.SYSTEMENVELOPE)
                    await UpdateSubStatusRefEnv(envelope.SenderIdentifier, envelope.SenderAlias, documents[0].RefId, documents[0].ResponseCode);
                await _docRepo.AddRange(documents);
                envelope.Documents = documents;
                envelope.ResponseCode = 1200;
                envelope.ResponseDesc = "ZARF BASARIYLA ISLENDI";
                envelope.SubStatus = SubStatus.SUCCEED;
            }
            catch (EnvelopeException ex)
            {
                string[] split = ParseMessage(ex.Message);
                envelope.ResponseCode = int.Parse(split[0]);
                envelope.ResponseDesc = split[1];
                envelope.SubStatus = SubStatus.FAILED;
            }
            catch (System.Exception ex)
            {
                envelope.ResponseCode = 1195;
                envelope.ResponseDesc = $"SISTEM HATASI({ex.Message})";
                envelope.SubStatus = SubStatus.FAILED;
            }
            if (envelope.Type != EnvelopeType.SYSTEMENVELOPE)
            {
                ApplicationResponseType appResponse = Mapper.Map(envelope);
                Document document = CreateAppResponse(appResponse);
                await _docRepo.Add(document);
            }
            envelope.ModifyDate = DateTime.Now;
            await _envRepo.SaveChanges();
        }
    }

    public async Task SignDocuments()
    {
        List<Document> documents = await _docRepo.GetLoadedDocs();
        await ToProcessing(documents, Status.SIGN);
        foreach (Document document in documents)
        {
            try
            {
                signDocumentRequest request = new();
                request.arg0 = document.Content;
                signDocumentResponse response = await _signerClient.signDocumentAsync(request);
                document.Content = response.@return;
                document.SubStatus = SubStatus.SUCCEED;
            }
            catch
            {
                document.SubStatus = SubStatus.FAILED;
            }
            document.SigningTime = DateTime.Now;
            await _docRepo.SaveChanges();
        }
    }

    public async Task CreateEnvelope()
    {
        List<Document> documents = await _docRepo.GetSignedDocs();
        await ToProcessing(documents, Status.PACKAGE);
        foreach (Document document in documents)
        {
            try
            {
                XmlElement package = CreatePackage(document.Type.ToString(), document.Content);
                StandardBusinessDocument sbd = new();
                sbd.StandardBusinessDocumentHeader = await CreateSbdHeader(document);
                XmlDocument envXml = Util.Serialize(sbd);
                envXml.DocumentElement?.AppendChild(envXml.ImportNode(package, true));
                Envelope envelope = Mapper.Map(sbd.StandardBusinessDocumentHeader);
                envelope.CreateDate = DateTime.Now;
                envelope.Status = Status.PACKAGE;
                envelope.SubStatus = SubStatus.SUCCEED;
                envelope.PackageType = document.Type;
                envelope.Direction = Direction.OUT;
                envelope.Content = Encoding.UTF8.GetBytes(envXml.OuterXml);
                await _envRepo.Add(envelope);
                document.Envelope = envelope;
                document.SubStatus = SubStatus.SUCCEED;
            }
            catch (System.Exception ex)
            {
                document.SubStatus = SubStatus.FAILED;
                document.ErrorDesc = ex.Message.Length > 255 ? ex.Message[..255] : ex.Message;
            }
            await _docRepo.SaveChanges();
        }
    }

    public async Task SendEnvelope()
    {
        List<Envelope> envelopes = await _envRepo.GetPackagedEnv();
        await ToProcessing(envelopes, Status.SEND);
        foreach (Envelope envelope in envelopes)
        {
            sendDocument request = new();
            request.documentRequest = new();
            request.documentRequest.binaryData = new();
            try
            {
                request.documentRequest.fileName = $"{envelope.InstanceIdentifier}.zip";
                request.documentRequest.binaryData.Value = Util.Zip(envelope.Content, $"{envelope.InstanceIdentifier}.xml");
                request.documentRequest.hash = Convert.ToHexString(MD5.HashData(request.documentRequest.binaryData.Value));
                sendDocumentResponse response = await _gibClient.sendDocumentAsync(request);
                if (response.documentResponse.msg == "Döküman GIB tarafından alındı.")
                {
                    envelope.StatusCheck = StatusCheck.N;
                    envelope.SubStatus = SubStatus.WAIT_GIB_RESPONSE;
                }
                else envelope.SubStatus = SubStatus.FAILED;
                envelope.ResponseDesc = response.documentResponse.msg;
            }
            catch (System.Exception ex)
            {
                string[] split = ParseMessage(ex.Message);
                int code = int.Parse(split[0]);
                envelope.ResponseCode = code;
                envelope.ResponseDesc = split[1];
                envelope.SubStatus = SubStatusByCode(code);
                envelope.StatusCheck = StatusCheckByCode(code);
            }
            envelope.ModifyDate = DateTime.Now;
            await _envRepo.SaveChanges();
        }
    }

    public async Task CheckStatus()
    {
        List<Envelope> envelopes = await _envRepo.GetEnvForStatusCheck();
        foreach (Envelope envelope in envelopes)
        {
            try
            {
                getApplicationResponse request = new();
                request.getAppRespRequest = new();
                request.getAppRespRequest.instanceIdentifier = envelope.InstanceIdentifier;
                getApplicationResponseResponse appResponse = await _gibClient.getApplicationResponseAsync(request);
                XmlDocument xmlDoc = new();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.LoadXml(appResponse.getAppRespResponse.applicationResponse);
                ResponseType response = ParseResponse(xmlDoc);
                int code = int.Parse(response.ResponseCode.Value);
                envelope.ResponseCode = code;
                envelope.ResponseDesc = response.Description[0].Value;
                envelope.SubStatus = SubStatusByCode(code);
                envelope.StatusCheck = StatusCheckByCode(code);
            }
            catch (System.Exception ex)
            {
                string[] split = ParseMessage(ex.Message);
                int code = int.Parse(split[0]);
                envelope.ResponseCode = code;
                envelope.ResponseDesc = split[1];
                envelope.SubStatus = SubStatusByCode(code);
                envelope.StatusCheck = StatusCheckByCode(code);
            }
            envelope.ModifyDate = DateTime.Now;
            await _envRepo.SaveChanges();
        }
    }

    private static XmlElement CreatePackage(string type, byte[] content)
    {
        Package package = Init.Package();
        package.Elements[0].ElementType = type;
        package.Elements[0].ElementList.Any[0] = Util.BytesToXml(content).DocumentElement;
        XmlDocument packageXml = Util.Serialize(package);
        return packageXml.DocumentElement ?? 
            throw new EnvelopeException("1143:GECERSIZ BELGE TIPI"); ;
    }

    private static List<Document> GetDocuments(DocumentTypes docType, Package package)
    {
        List<Document> documents = [];
        for (int i = 0; i < package.Elements[0].ElementCount; i++)
        {
            string docXml = package.Elements[0].ElementList.Any[i].OuterXml;
            Document document = CreateDocument(docType, docXml);
            document.Status = Status.RECEIVE;
            document.SubStatus = SubStatus.SUCCEED;
            document.Type = docType;
            document.Direction = Direction.IN;
            document.CreateDate = DateTime.Now;
            document.Content = Encoding.UTF8.GetBytes(docXml);
            documents.Add(document);
        }
        return documents;
    }

    private async Task UpdateSubStatusRefEnv(string senderIdentifier, string senderAlias, string? refId, string? responseCode)
    {
        Envelope envelope = await _envRepo.GetEnvelope(refId, Direction.OUT)
            ?? throw new EnvelopeException("1191:Gönderilen sistem yanıtı daha önce gönderilen bir zarfa referans değildir.");
        if (responseCode == "1200")
        {
            if (senderIdentifier == GIB.Identifier && senderAlias == GIB.Alias)
            {
                envelope.SubStatus = SubStatus.WAIT_SYSTEM_RESPONSE;
            }
            else if (senderIdentifier == envelope.ReceiverIdentifier && senderAlias == envelope.ReceiverAlias)
            {
                envelope.SubStatus = SubStatus.SUCCEED;
            }
        }
        else envelope.SubStatus = SubStatus.FAILED;
        envelope.ModifyDate = DateTime.Now;
        await _envRepo.SaveChanges();
    }

    private static Document CreateDocument(DocumentTypes packageType, string docXml)
    {
        byte[] content = Encoding.UTF8.GetBytes(docXml);
        return packageType switch
        {
            DocumentTypes.INVOICE => Mapper.Map(Util.Deserialize<InvoiceType>(content)),
            DocumentTypes.APPLICATIONRESPONSE => Mapper.Map(Util.Deserialize<ApplicationResponseType>(content)),
            DocumentTypes.DESPATCHADVICE => Mapper.Map(Util.Deserialize<DespatchAdviceType>(content)),
            DocumentTypes.RECEIPTADVICE => Mapper.Map(Util.Deserialize<ReceiptAdviceType>(content)),
            _ => throw new EnvelopeException("1143:GECERSIZ BELGE TIPI")
        };
    }

    private static Document CreateAppResponse(ApplicationResponseType appResponse)
    {
        Document document = Mapper.Map(appResponse);
        document.Status = Status.SIGN;
        document.SubStatus = SubStatus.SUCCEED;
        document.Type = DocumentTypes.APPLICATIONRESPONSE;
        document.Direction = Direction.OUT;
        document.CreateDate = DateTime.Now;
        XmlDocument xml = Util.Serialize(appResponse);
        document.Content = Encoding.UTF8.GetBytes(xml.OuterXml);
        return document;
    }

    private async Task ToProcessing(List<Document> documents, Status status)
    {
        foreach (Document document in documents)
        {
            document.Status = status;
            document.SubStatus = SubStatus.PROCESSING;
        }
        await _docRepo.SaveChanges();
    }

    private async Task ToProcessing(List<Envelope> envelopes, Status status)
    {
        foreach (Envelope envelope in envelopes)
        {
            envelope.Status = status;
            envelope.SubStatus = SubStatus.PROCESSING;
            envelope.ModifyDate = DateTime.Now;
            if (status == Status.RECEIVE)
            {
                envelope.ResponseCode = 1100;
                envelope.ResponseDesc = "ZARF ISLENIYOR";
                envelope.StatusCheck = StatusCheck.P;
            }
        }
        await _envRepo.SaveChanges();
    }

    private async Task<StandardBusinessDocumentHeader> CreateSbdHeader(Document document)
    {
        StandardBusinessDocumentHeader header = Init.SbdHeader();
        EnvelopeType envType = EnvelopeByPackage(document.Type, document.ProfileId);
        if (envType == EnvelopeType.SENDERENVELOPE)
        {
            GetGibUserRequest request = new(document.SupplierIdentifier, document.Type.ToString(), "GB");
            GibUserDto sender = await _gibUserClient.GetAsync(request)
                ?? throw new EnvelopeException("1171:GONDERICI BIRIM YETKISI YOK.");
            header.Sender[0].Identifier.Value = sender.Alias;
            header.Sender[0].ContactInformation[0].Contact = sender.Identifier;
            header.Sender[0].ContactInformation[1].Contact = sender.Title;
            request = new(document.CustomerIdentifier, document.Type.ToString(), "PK");
            GibUserDto? receiver = await _gibUserClient.GetAsync(request);
            if (receiver == null)
            {
                if (document.Type == DocumentTypes.DESPATCHADVICE)
                {
                    header.Receiver[0].Identifier.Value = GIB.DespatchAlias;
                    header.Receiver[0].ContactInformation[0].Contact = GIB.DespatchIdentifier;
                    header.Receiver[0].ContactInformation[1].Contact = GIB.DespatchTitle;
                }
                else throw new EnvelopeException("1172:POSTA KUTUSU YETKISI YOK");
            }
            else
            {
                header.Receiver[0].Identifier.Value = receiver.Alias;
                header.Receiver[0].ContactInformation[0].Contact = receiver.Identifier;
                header.Receiver[0].ContactInformation[1].Contact = receiver.Title;
            }
        }
        else if (envType == EnvelopeType.SYSTEMENVELOPE || envType == EnvelopeType.POSTBOXENVELOPE)
        {
            Envelope refEnv = await _envRepo.GetEnvelope(document.RefId, Direction.IN)
                ?? throw new EnvelopeException("1191:Gönderilen sistem yanıtı daha önce gönderilen bir zarfa referans değildir.");
            header.Sender[0].Identifier.Value = refEnv.ReceiverAlias;
            header.Sender[0].ContactInformation[0].Contact = refEnv.ReceiverIdentifier;
            header.Sender[0].ContactInformation[1].Contact = refEnv.ReceiverTitle;
            header.Receiver[0].Identifier.Value = refEnv.SenderAlias;
            header.Receiver[0].ContactInformation[0].Contact = refEnv.SenderIdentifier;
            header.Receiver[0].ContactInformation[1].Contact = refEnv.SenderTitle;
        }
        header.DocumentIdentification.Type = envType.ToString();
        header.DocumentIdentification.InstanceIdentifier = Guid.NewGuid().ToString();
        header.DocumentIdentification.CreationDateAndTime = DateTime.Now;
        return header;
    }

    private static ResponseType ParseResponse(XmlDocument xmlDocument)
    {
        ResponseType response = Init.Response();
        XmlNamespaceManager ns = new(xmlDocument.NameTable);
        ns.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        ns.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
        response.ResponseCode.Value = xmlDocument.SelectSingleNode("//cac:LineResponse/cac:Response/cbc:ResponseCode", ns)?.InnerText;
        response.Description[0].Value = xmlDocument.SelectSingleNode("//cac:LineResponse/cac:Response/cbc:Description", ns)?.InnerText;
        return response;
    }

    private static EnvelopeType EnvelopeByPackage(DocumentTypes packageType, string profileId)
    {
        EnvelopeType envType;
        if (packageType == DocumentTypes.INVOICE || packageType == DocumentTypes.DESPATCHADVICE)
            envType = EnvelopeType.SENDERENVELOPE;

        else if (packageType == DocumentTypes.APPLICATIONRESPONSE && profileId == "UBL-TR-PROFILE-1")
            envType = EnvelopeType.SYSTEMENVELOPE;

        else if (packageType == DocumentTypes.APPLICATIONRESPONSE && profileId == "TICARIFATURA")
            envType = EnvelopeType.POSTBOXENVELOPE;

        else if (packageType == DocumentTypes.RECEIPTADVICE)
            envType = EnvelopeType.POSTBOXENVELOPE;

        else throw new EnvelopeException("1143:GECERSIZ BELGE TIPI");
        return envType;
    }

    private static string[] ParseMessage(string message)
    {
        string[] split = message.Split(':');
        if (split.Length == 2 && int.TryParse(split[0], out _))
        {
            return split;
        }
        else
        {
            split = new string[2];
            split[0] = "0";
            split[1] = message;
            return split;
        }
    }

    private static StatusCheck StatusCheckByCode(int code)
    {
        StatusCheck check;
        if (code == 1000 || code == 1100 || code == 1200 || code == 1210 || code == 1220) check = StatusCheck.P;
        else if (code > 1100 && code < 1200) check = StatusCheck.Y;
        else if (code == 1215 || code == 1230 || code == 1300) check = StatusCheck.Y;
        else check = StatusCheck.N;
        return check;
    }

    private static SubStatus SubStatusByCode(int code)
    {
        SubStatus subStatus;
        if (code == 1000 || code == 1100 || code == 1200 || code == 1210) subStatus = SubStatus.WAIT_GIB_RESPONSE;
        else if (code > 1100 && code < 1200) subStatus = SubStatus.FAILED;
        else if (code == 1215 || code == 1230 || code >= 2000) subStatus = SubStatus.FAILED;
        else if (code == 1220) subStatus = SubStatus.WAIT_SYSTEM_RESPONSE;
        else if (code == 1300) subStatus = SubStatus.SUCCEED;
        else subStatus = SubStatus.FAILED;
        return subStatus;
    }

    private static void ValidateSchema(byte[] content)
    {
        try
        {
            Util.ValidateSchema(content, "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader", "xsd/Envelope/PackageProxy_1_2.xsd");
        }
        catch (System.Exception ex)
        {
            throw new EnvelopeException($"1160:XML SEMA KONTROLUNDEN GECEMEDI{ex.Message})");
        }
    }
}
