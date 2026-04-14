using Application.DTOs.UBL;
using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs;

internal static class Mapper
{
    public static DocumentDto Map(Document document)
    {
        var status = GetStatus(document.Status, document.SubStatus);
        DocumentDto dto = new();
        {
            dto.BelgeNumarasi = document.DocumentId;
            dto.Ettn = document.Uuid;
            dto.Senaryo = document.ProfileId;
            dto.BelgeTipi = document.TypeCode;
            dto.DurumKodu = status.Item1;
            dto.DurumAciklamasi = status.Item2;
            dto.BelgeTarihi = document.IssueDate;
            dto.OlusturmaTarihi = document.CreatedAt;
            dto.Yon = document.Direction.ToString() == "IN" ? "GELEN" : "GIDEN";
            dto.OdenecekTutar = document.PayableAmount;
            dto.ParaBirimi = document.Currency;
            dto.GondericiVergiNo = document.SupplierIdentifier;
            dto.GondericiUnvan = document.SupplierTitle;
            dto.MusteriVergiNo = document.CustomerIdentifier;
            dto.MusteriUnvan = document.CustomerTitle;
            dto.IptalMi = document.CancelFlag ? "Evet" : "Hayır";
            dto.IptalTarihi = document.CancelDate;
            dto.ReferansNo = document.RefId;
            dto.YanitKodu = document.ResponseCode;
            dto.YanitAciklamasi = document.ResponseDesc;
        }
        return dto;
    }

    public static ApplicationResponseType Map(Envelope envelope)
    {
        ApplicationResponseType appResponse = Init.AppResponse();
        appResponse.ID.Value = Guid.NewGuid().ToString();
        appResponse.UUID.Value = Guid.NewGuid().ToString();
        appResponse.IssueDate.Value = DateTime.Now;
        appResponse.IssueTime.Value = DateTime.Now;
        appResponse.SenderParty.PartyIdentification[0].ID.Value = envelope.ReceiverIdentifier;
        appResponse.SenderParty.PartyIdentification[0].ID.schemeID = envelope.ReceiverIdentifier?.Length == 10 ? "VKN" : "TCKN";
        appResponse.SenderParty.PartyName.Name.Value = envelope.ReceiverTitle;
        appResponse.ReceiverParty.PartyIdentification[0].ID.Value = envelope.SenderIdentifier;
        appResponse.ReceiverParty.PartyIdentification[0].ID.schemeID = envelope.SenderIdentifier?.Length == 10 ? "VKN" : "TCKN";
        appResponse.ReceiverParty.PartyName.Name.Value = envelope.SenderTitle;
        appResponse.DocumentResponse[0].Response.ReferenceID.Value = Guid.NewGuid().ToString();
        appResponse.DocumentResponse[0].DocumentReference.ID.Value = envelope.InstanceIdentifier;
        appResponse.DocumentResponse[0].DocumentReference.IssueDate.Value = envelope.CreatedAt;
        appResponse.DocumentResponse[0].DocumentReference.DocumentType.Value = envelope.Type.ToString();
        appResponse.DocumentResponse[0].DocumentReference.DocumentTypeCode.Value = envelope.Type.ToString();
        appResponse.DocumentResponse[0].LineResponse[0].LineReference.DocumentReference.ID.Value = envelope.InstanceIdentifier;
        appResponse.DocumentResponse[0].LineResponse[0].LineReference.DocumentReference.IssueDate.Value = envelope.CreatedAt;
        appResponse.DocumentResponse[0].LineResponse[0].Response[0].ReferenceID.Value = Guid.NewGuid().ToString();
        appResponse.DocumentResponse[0].LineResponse[0].Response[0].ResponseCode.Value = envelope.ResponseCode.ToString();
        appResponse.DocumentResponse[0].LineResponse[0].Response[0].Description[0].Value = envelope.ResponseDesc;
        return appResponse;
    }

    public static Envelope Map(StandardBusinessDocumentHeader header)
    {
        Envelope envelope = new();
        envelope.Type = Enum.Parse<EnvelopeType>(header.DocumentIdentification.Type);
        envelope.InstanceIdentifier = header.DocumentIdentification.InstanceIdentifier;
        envelope.SenderAlias = header.Sender[0].Identifier.Value;
        envelope.SenderIdentifier = header.Sender[0].ContactInformation.Single(r => r.ContactTypeIdentifier == "VKN_TCKN").Contact;
        envelope.SenderTitle = header.Sender[0].ContactInformation.Single(r => r.ContactTypeIdentifier == "UNVAN").Contact;
        envelope.ReceiverAlias = header.Receiver[0].Identifier.Value;
        envelope.ReceiverIdentifier = header.Receiver[0].ContactInformation.Single(r => r.ContactTypeIdentifier == "VKN_TCKN").Contact;
        envelope.ReceiverTitle = header.Receiver[0].ContactInformation.Single(r => r.ContactTypeIdentifier == "UNVAN").Contact;
        return envelope;
    }

    public static Document Map(InvoiceType ubl)
    {
        Document document = new();
        document.DocumentId = ubl.ID.Value;
        document.Uuid = ubl.UUID.Value;
        document.ProfileId = ubl.ProfileID.Value;
        document.TypeCode = ubl.InvoiceTypeCode.Value;
        document.IssueDate = ubl.IssueDate.Value;
        document.PayableAmount = ubl.LegalMonetaryTotal.PayableAmount.Value;
        document.Currency = ubl.DocumentCurrencyCode.Value;
        document.SupplierIdentifier = ubl.AccountingSupplierParty.Party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.SupplierTitle = GetTitle(ubl.AccountingSupplierParty.Party);
        document.CustomerIdentifier = ubl.AccountingCustomerParty.Party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.CustomerTitle = GetTitle(ubl.AccountingCustomerParty.Party);
        return document;
    }

    public static Document Map(DespatchAdviceType ubl)
    {
        Document document = new();
        document.DocumentId = ubl.ID.Value;
        document.Uuid = ubl.UUID.Value;
        document.ProfileId = ubl.ProfileID.Value;
        document.TypeCode = ubl.DespatchAdviceTypeCode.Value;
        document.IssueDate = ubl.IssueDate.Value;
        document.SupplierIdentifier = ubl.DespatchSupplierParty.Party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.SupplierTitle = GetTitle(ubl.DespatchSupplierParty.Party);
        document.CustomerIdentifier = ubl.DeliveryCustomerParty.Party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.CustomerTitle = GetTitle(ubl.DeliveryCustomerParty.Party);
        return document;
    }

    public static Document Map(ReceiptAdviceType ubl)
    {
        Document document = new();
        document.DocumentId = ubl.ID.Value;
        document.Uuid = ubl.UUID.Value;
        document.ProfileId = ubl.ProfileID.Value;
        document.TypeCode = ubl.ReceiptAdviceTypeCode.Value;
        document.IssueDate = ubl.IssueDate.Value;
        document.SupplierIdentifier = ubl.DespatchSupplierParty.Party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.SupplierTitle = GetTitle(ubl.DespatchSupplierParty.Party);
        document.CustomerIdentifier = ubl.DeliveryCustomerParty.Party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.CustomerTitle = GetTitle(ubl.DeliveryCustomerParty.Party);
        document.RefId = ubl.DespatchDocumentReference.ID.Value;
        return document;
    }

    public static Document Map(ApplicationResponseType ubl)
    {
        Document document = new();
        document.DocumentId = ubl.ID.Value;
        document.Uuid = ubl.UUID.Value;
        document.ProfileId = ubl.ProfileID.Value;
        document.IssueDate = ubl.IssueDate.Value;
        document.SupplierIdentifier = ubl.SenderParty.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.SupplierTitle = GetTitle(ubl.SenderParty);
        document.CustomerIdentifier = ubl.ReceiverParty.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.CustomerTitle = GetTitle(ubl.ReceiverParty);
        document.RefId = ubl.DocumentResponse[0].DocumentReference.ID.Value;
        document.ResponseCode = ubl.DocumentResponse[0].LineResponse[0].Response[0].ResponseCode.Value;
        document.ResponseDesc = ubl.DocumentResponse[0].LineResponse[0].Response[0].Description[0].Value;
        return document;
    }

    public static Document Map(CreditNoteType ubl)
    {
        Document document = new();
        document.DocumentId = ubl.ID.Value;
        document.Uuid = ubl.UUID.Value;
        document.ProfileId = ubl.ProfileID.Value;
        document.TypeCode = ubl.CreditNoteTypeCode.Value;
        document.IssueDate = ubl.IssueDate.Value;
        document.PayableAmount = ubl.LegalMonetaryTotal.PayableAmount.Value;
        document.Currency = ubl.DocumentCurrencyCode.Value;
        document.SupplierIdentifier = ubl.AccountingSupplierParty.Party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.SupplierTitle = GetTitle(ubl.AccountingSupplierParty.Party);
        document.CustomerIdentifier = ubl.AccountingCustomerParty.Party.PartyIdentification.Single(c => c.ID.schemeID == "VKN" || c.ID.schemeID == "TCKN").ID.Value;
        document.CustomerTitle = GetTitle(ubl.AccountingCustomerParty.Party);
        return document;
    }

    private static string GetTitle(PartyType party)
    {
        string? title = party.PartyName?.Name.Value;
        title ??= $"{party.Person?.FirstName.Value} {party.Person?.FamilyName.Value}";
        return title;
    }

    private static (int,string) GetStatus(Status status, SubStatus subStatus)
    {
        int code = 0;
        string desc = "";
        if (status == Status.LOAD)
        {
            if (subStatus == SubStatus.SUCCEED)
            {
                code = 10;
                desc = "Yüklendi";
            }
        }
        else if (status == Status.SIGN)
        {
            if (subStatus == SubStatus.PROCESSING)
            {
                code = 18;
                desc = "İmzalanıyor";
            }
            else if (subStatus == SubStatus.FAILED)
            {
                code = 19;
                desc = "İmzalanamadı";
            }
            else if (subStatus == SubStatus.SUCCEED)
            {
                code = 20;
                desc = "İmzalandı";
            }
        }
        else if (status == Status.PACKAGE)
        {
            if (subStatus == SubStatus.PROCESSING)
            {
                code = 28;
                desc = "Paketleniyor";
            }
            else if (subStatus == SubStatus.FAILED)
            {
                code = 29;
                desc = "Paketlenemedi";
            }
            else if (subStatus == SubStatus.SUCCEED)
            {
                code = 30;
                desc = "Paketlendi";
            }
        }
        else if (status == Status.RECEIVE)
        {
            if (subStatus == SubStatus.PROCESSING)
            {
                code = 38;
                desc = "Alınıyor";
            }
            else if (subStatus == SubStatus.FAILED)
            {
                code = 39;
                desc = "Alınamadı";
            }
            else if (subStatus == SubStatus.SUCCEED)
            {
                code = 40;
                desc = "Alındı";
            }
        }
        return (code, desc);
    }
}