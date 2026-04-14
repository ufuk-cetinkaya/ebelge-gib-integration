using Application.Contracts;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace DocumentApi;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/document")
            .WithTags("Document");

        group.MapGet("/", GetDocumentsAsync)
            .WithName("GetDocuments")
            .AllowAnonymous()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        group.MapGet("/preview/{uuid:guid}/{type}", PreviewAsync)
            .WithName("Preview")
            .AllowAnonymous()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        group.MapPost("/upload", UploadAsync)
            .WithName("Upload")
            .AllowAnonymous()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        group.MapDelete("/cancel/{uuid:guid}", CancelAsync)
            .WithName("Cancel")
            .AllowAnonymous()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
    }

    private static async Task<IResult> GetDocumentsAsync(
        [AsParameters] DocumentFilter filter,
        IDocumentService documentService)
    {
        var documents = await documentService.GetDocuments(filter);
        return TypedResults.Ok(documents);
    }

    private static async Task<IResult> PreviewAsync(
        Guid uuid,
        string type,
        IDocumentService documentService)
    {
        var (content, contentType) = type.ToLower() switch
        {
            "xml" => (await documentService.GetXmlContent(uuid), "application/xml"),
            "html" => (await documentService.GetHtmlContent(uuid), "text/html"),
            "pdf" => (await documentService.GetPdfContent(uuid), "application/pdf"),
            _ => throw new BadHttpRequestException("Döküman tipi xml, html veya pdf olmalıdır.")
        };
        return TypedResults.File(content, contentType);
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        IDocumentService documentService)
    {
        if (request.ContentLength is null or 0)
            throw new BadHttpRequestException("Dosya içeriği bulunamadı.");
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        await documentService.LoadDocument(ms.ToArray());
        return TypedResults.Ok();
    }

    private static async Task<IResult> CancelAsync(
        Guid uuid,
        IDocumentService documentService)
    {
        await documentService.CancelDocument(uuid);
        return TypedResults.Ok();
    }
}
