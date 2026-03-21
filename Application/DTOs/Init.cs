using Application.DTOs.UBL;
using System.Xml;

namespace Application.DTOs;

internal static class Init
{
    public static ResponseType Response()
    {
        ResponseType response = new();
        response.ReferenceID = new();
        response.ResponseCode = new();
        response.Description = new DescriptionType[1];
        response.Description[0] = new();
        return response;
    }

    public static ApplicationResponseType AppResponse()
    {
        ApplicationResponseType response = new();
        response.UBLVersionID = new();
        response.UBLVersionID.Value = "2.1";
        response.CustomizationID = new();
        response.CustomizationID.Value = "TR1.2";
        response.ProfileID = new();
        response.ProfileID.Value = "UBL-TR-PROFILE-1";
        response.ID = new();
        response.UUID = new();
        response.IssueDate = new();
        response.IssueTime = new();
        response.SenderParty = new();
        response.SenderParty.PartyIdentification = new PartyIdentificationType[1];
        response.SenderParty.PartyIdentification[0] = new();
        response.SenderParty.PartyIdentification[0].ID = new();
        response.SenderParty.PartyName = new();
        response.SenderParty.PartyName.Name = new();
        response.SenderParty.PostalAddress = new();
        response.SenderParty.PostalAddress.CitySubdivisionName = new();
        response.SenderParty.PostalAddress.CitySubdivisionName.Value = "";
        response.SenderParty.PostalAddress.CityName = new();
        response.SenderParty.PostalAddress.CityName.Value = "";
        response.SenderParty.PostalAddress.Country = new();
        response.SenderParty.PostalAddress.Country.IdentificationCode = new();
        response.SenderParty.PostalAddress.Country.IdentificationCode.Value = "TR";
        response.SenderParty.PostalAddress.Country.Name = new();
        response.SenderParty.PostalAddress.Country.Name.Value = "Türkiye";
        response.ReceiverParty = new();
        response.ReceiverParty.PartyIdentification = new PartyIdentificationType[1];
        response.ReceiverParty.PartyIdentification[0] = new();
        response.ReceiverParty.PartyIdentification[0].ID = new();
        response.ReceiverParty.PartyName = new();
        response.ReceiverParty.PartyName.Name = new();
        response.ReceiverParty.PostalAddress = new();
        response.ReceiverParty.PostalAddress.CitySubdivisionName = new();
        response.ReceiverParty.PostalAddress.CitySubdivisionName.Value = "";
        response.ReceiverParty.PostalAddress.CityName = new();
        response.ReceiverParty.PostalAddress.CityName.Value = "";
        response.ReceiverParty.PostalAddress.Country = new();
        response.ReceiverParty.PostalAddress.Country.IdentificationCode = new();
        response.ReceiverParty.PostalAddress.Country.IdentificationCode.Value = "TR";
        response.ReceiverParty.PostalAddress.Country.Name = new();
        response.ReceiverParty.PostalAddress.Country.Name.Value = "Türkiye";
        response.DocumentResponse = new DocumentResponseType[1];
        response.DocumentResponse[0] = new();
        response.DocumentResponse[0].Response = new();
        response.DocumentResponse[0].Response.ReferenceID = new();
        response.DocumentResponse[0].Response.ResponseCode = new();
        response.DocumentResponse[0].Response.ResponseCode.Value = "S_APR";
        response.DocumentResponse[0].Response.Description = new DescriptionType[1];
        response.DocumentResponse[0].Response.Description[0] = new();
        response.DocumentResponse[0].Response.Description[0].Value = "SystemApplicationResponse";
        response.DocumentResponse[0].DocumentReference = new();
        response.DocumentResponse[0].DocumentReference.ID = new();
        response.DocumentResponse[0].DocumentReference.IssueDate = new();
        response.DocumentResponse[0].DocumentReference.DocumentType = new();
        response.DocumentResponse[0].DocumentReference.DocumentTypeCode = new();
        response.DocumentResponse[0].LineResponse = new LineResponseType[1];
        response.DocumentResponse[0].LineResponse[0] = new();
        response.DocumentResponse[0].LineResponse[0].LineReference = new();
        response.DocumentResponse[0].LineResponse[0].LineReference.LineID = new();
        response.DocumentResponse[0].LineResponse[0].LineReference.LineID.Value = "0";
        response.DocumentResponse[0].LineResponse[0].LineReference.DocumentReference = new();
        response.DocumentResponse[0].LineResponse[0].LineReference.DocumentReference.ID = new();
        response.DocumentResponse[0].LineResponse[0].LineReference.DocumentReference.IssueDate = new();
        response.DocumentResponse[0].LineResponse[0].Response = new ResponseType[1];
        response.DocumentResponse[0].LineResponse[0].Response[0] = new();
        response.DocumentResponse[0].LineResponse[0].Response[0].ReferenceID = new();
        response.DocumentResponse[0].LineResponse[0].Response[0].ResponseCode = new();
        response.DocumentResponse[0].LineResponse[0].Response[0].Description = new DescriptionType[1];
        response.DocumentResponse[0].LineResponse[0].Response[0].Description[0] = new();
        return response;
    }

    public static StandardBusinessDocumentHeader SbdHeader()
    {
        StandardBusinessDocumentHeader header = new();
        header.Sender = new Partner[1];
        header.Sender[0] = Partner();
        header.Receiver = new Partner[1];
        header.Receiver[0] = Partner();
        header.HeaderVersion = "1.2";
        header.DocumentIdentification = new();
        header.DocumentIdentification.Standard = "UBLTR";
        header.DocumentIdentification.TypeVersion = "1.2";
        return header;
    }

    public static Package Package()
    {
        Package package = new();
        package.Elements = new PackageElements[1];
        package.Elements[0] = new();
        package.Elements[0].ElementList = new();
        package.Elements[0].ElementCount = 1;
        package.Elements[0].ElementList.Any = new XmlElement[1];
        return package;
    }

    private static Partner Partner()
    {
        Partner partner = new();
        partner.Identifier = new();
        partner.ContactInformation = new ContactInformation[2];
        partner.ContactInformation[0] = new();
        partner.ContactInformation[0].ContactTypeIdentifier = "VKN_TCKN";
        partner.ContactInformation[1] = new();
        partner.ContactInformation[1].ContactTypeIdentifier = "UNVAN";
        return partner;
    }
}