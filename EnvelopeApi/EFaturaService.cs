using Application.Contracts;
using Application.Exceptions;
using EFaturaWsService;
using System.Security.Cryptography;
using System.ServiceModel;

namespace EnvelopeApi;

public class EFaturaService : EFaturaPortType
{
    private readonly IEnvelopeHandler _envelope;

    public EFaturaService(IEnvelopeHandler envelope)
    {
        _envelope = envelope;
    }

    public async Task<getApplicationResponseResponse> getApplicationResponseAsync(getApplicationResponse request)
    {
        try
        {
            string appResponse = await _envelope.CreateAppResponse(
                request.getAppRespRequest.instanceIdentifier)
                ?? throw new EFaturaException("2004:ZARF ID BULUNAMADI");

            getApplicationResponseResponse response = new();
            response.getAppRespResponse = new();
            response.getAppRespResponse.applicationResponse = appResponse;
            return response;
        }
        catch (EFaturaException ex)
        {
            string[] split = ex.Message.Split(':');
            EFaturaFaultType fault = new();
            fault.codeSpecified = true;
            fault.code = int.Parse(split[0]);
            fault.msg = split[1];
            throw new FaultException<EFaturaFaultType>(fault, ex.Message);
        }
        catch (Exception)
        {
            EFaturaFaultType fault = new();
            fault.codeSpecified = true;
            fault.code = 2005;
            fault.msg = "SISTEM HATASI";
            throw new FaultException<EFaturaFaultType>(fault, "2005:SISTEM HATASI");
        }
    }

    public async Task<sendDocumentResponse> sendDocumentAsync(sendDocument request)
    {
        try
        {
            CheckName(request.documentRequest.fileName);

            string instanceIdentifier = Path.GetFileNameWithoutExtension(request.documentRequest.fileName);
            if (await _envelope.Exists(instanceIdentifier))
                throw new EFaturaException("2001:ZARF ID SISTEMDE MEVCUT");

            CheckHash(request.documentRequest.binaryData.Value, request.documentRequest.hash);

            CheckSize(request.documentRequest.binaryData.Value);

            string result = await _envelope.Enqueue(instanceIdentifier, request.documentRequest.binaryData.Value);
            if (result != "Döküman başarıyla alındı.")
                throw new EFaturaException("2003:ZARF KUYRUGA EKLENEMEDI");

            sendDocumentResponse response = new();
            response.documentResponse = new();
            response.documentResponse.msg = result;
            response.documentResponse.hash = request.documentRequest.hash;
            return response;
        }
        catch (EFaturaException ex)
        {
            string[] split = ex.Message.Split(':');
            EFaturaFaultType fault = new();
            fault.codeSpecified = true;
            fault.code = int.Parse(split[0]);
            fault.msg = split[1];
            throw new FaultException<EFaturaFaultType>(fault, ex.Message);
        }
        catch (EnvelopeException ex)
        {
            string[] split = ex.Message.Split(':');
            EFaturaFaultType fault = new();
            fault.codeSpecified = true;
            fault.code = int.Parse(split[0]);
            fault.msg = split[1];
            throw new FaultException<EFaturaFaultType>(fault, ex.Message);
        }
        catch (Exception)
        {
            EFaturaFaultType fault = new();
            fault.codeSpecified = true;
            fault.code = 2005;
            fault.msg = "SISTEM HATASI";
            throw new FaultException<EFaturaFaultType>(fault, "2005:SISTEM HATASI");
        }
    }

    private static void CheckName(string? name)
    {
        if (name?.Length == 40 &&
            Guid.TryParse(Path.GetFileNameWithoutExtension(name), out _) &&
            name.ToLowerInvariant().EndsWith(".zip"))
            return;
        else throw new EFaturaException("2006:GECERSIZ ZARF ADI");
    }

    private static void CheckHash(byte[]? bytes, string? hash)
    {
        string md5 = "";
        if (bytes?.Length > 0)
            md5 = Convert.ToHexString(MD5.HashData(bytes));
        if (md5 == hash?.ToUpper()) return;
        else throw new EFaturaException("2000:OZET DEGERLER ESIT DEGIL");
    }

    private static void CheckSize(byte[]? bytes)
    {
        if (bytes?.Length <= (5 * 1024 * 1024)) return;
        else throw new EFaturaException("2002:ZARF ARSIVE EKLENEMEDI");
    }
}

public class EFaturaException(string message) : Exception(message);