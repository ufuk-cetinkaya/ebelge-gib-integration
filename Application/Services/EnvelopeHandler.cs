using Application.Contracts;
using Application.DTOs;
using Application.DTOs.UBL;
using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Application.Services;

public class EnvelopeHandler : IEnvelopeHandler
{
    private readonly IDocumentRepository _docRepo;
    private readonly IEnvelopeRepository _envRepo;

    public EnvelopeHandler(IEnvelopeRepository envRepo,
        IDocumentRepository docRepo)
    {
        _docRepo = docRepo;
        _envRepo = envRepo;
    }

    public async Task<bool> Exists(string instanceidentifier)
    {
        int i = await _envRepo.GetEnvelopeCount(instanceidentifier);
        if (i > 0) return true;
        else return false;
    }

    public async Task<string> Enqueue(string instanceIdentifier, byte[] content)
    {
        content = await TryUnzip(content, instanceIdentifier);
        StandardBusinessDocument sbd = Util.Deserialize<StandardBusinessDocument>(content);
        Envelope envelope = Mapper.Map(sbd.StandardBusinessDocumentHeader);
        envelope.Content = content;
        envelope.CreatedAt = DateTime.Now;
        envelope.Direction = Direction.IN;
        envelope.Status = Status.RECEIVE;
        envelope.SubStatus = SubStatus.NEW;
        envelope.StatusCheck = StatusCheck.P;
        envelope.ResponseCode = 1000;
        envelope.ResponseDesc = "ZARF KUYRUGA EKLENDI";
        await _envRepo.Add(envelope);
        await _envRepo.SaveChanges();
        return "Döküman başarıyla alındı.";
    }

    public async Task<string?> CreateAppResponse(string instanceIdentifier)
    {
        string? response;
        int? id = await _docRepo.GetRefEnvId(instanceIdentifier);
        if (id == null)
        {
            Envelope? envelope = await _envRepo.GetEnvelope(instanceIdentifier, Direction.IN);
            if (envelope == null) response = null;
            else response = GetAppResponse(envelope);
        }
        else
        {
            byte[] content = await _envRepo.GetContent(id);
            response = Encoding.UTF8.GetString(content);
        }
        return response;
    }

    private static string GetAppResponse(Envelope envelope)
    {
        StandardBusinessDocument sbd = new();
        sbd.StandardBusinessDocumentHeader = Init.SbdHeader();
        sbd.StandardBusinessDocumentHeader.DocumentIdentification.InstanceIdentifier = Guid.NewGuid().ToString();
        sbd.StandardBusinessDocumentHeader.DocumentIdentification.CreationDateAndTime = DateTime.Now;
        sbd.StandardBusinessDocumentHeader.DocumentIdentification.Type = EnvelopeType.SYSTEMENVELOPE.ToString();
        sbd.StandardBusinessDocumentHeader.Sender[0].Identifier.Value = envelope.ReceiverAlias;
        sbd.StandardBusinessDocumentHeader.Sender[0].ContactInformation[0].Contact = envelope.ReceiverIdentifier;
        sbd.StandardBusinessDocumentHeader.Sender[0].ContactInformation[1].Contact = envelope.ReceiverTitle;
        sbd.StandardBusinessDocumentHeader.Receiver[0].Identifier.Value = envelope.SenderAlias;
        sbd.StandardBusinessDocumentHeader.Receiver[0].ContactInformation[0].Contact = envelope.SenderIdentifier;
        sbd.StandardBusinessDocumentHeader.Receiver[0].ContactInformation[1].Contact = envelope.SenderTitle;

        ApplicationResponseType appResponse = Mapper.Map(envelope);
        XmlDocument responseXml = Util.Serialize(appResponse);
        Package package = Init.Package();
        package.Elements[0].ElementType = DocumentTypes.APPLICATIONRESPONSE.ToString();
        package.Elements[0].ElementList.Any[0] = responseXml.DocumentElement;
        XmlDocument packageXml = Util.Serialize(package);
        sbd.Any = packageXml.DocumentElement;
        return Util.Serialize(sbd).OuterXml;
    }

    private static async Task<byte[]> TryUnzip(byte[] zipBytes, string entryName)
    {
        if (!(zipBytes.Length >= 4 &&
            zipBytes[0] == 0x50 &&
            zipBytes[1] == 0x4B &&
            zipBytes[2] == 0x03 &&
            zipBytes[3] == 0x04))
            throw new EnvelopeException("1110:ZIP DOSYASI DEGIL");

        ZipArchive? archive = null;
        using MemoryStream ms1 = new(zipBytes);
        try
        {
            archive = new ZipArchive(ms1, ZipArchiveMode.Read);
        }
        catch
        {
            archive?.Dispose();
            throw new EnvelopeException("1130:ZIP ACILAMADI");
        }

        if (archive.Entries.Count != 1)
            throw new EnvelopeException("1131:ZIP BIR DOSYA ICERMELI");

        if (!archive.Entries[0].Name.ToLowerInvariant().EndsWith(".xml"))
            throw new EnvelopeException("1132:XML DOSYASI DEGIL");

        ZipArchiveEntry entry = archive.GetEntry($"{entryName}.xml") ??
            throw new EnvelopeException("1133:ZARF ID VE XML DOSYASININ ADI AYNI OLMALI");

        try
        {
            using Stream entryStream = await entry.OpenAsync();
            using MemoryStream ms2 = new();
            entryStream.CopyTo(ms2);
            return ms2.ToArray();
        }
        catch
        {
            throw new EnvelopeException("1140:DOKUMAN AYRISTIRILAMADI");
        }
    }
}
