using Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocumentApi;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is DocumentException ex)
        {
            _logger.LogWarning(exception, "Document Exception");

            problem = new ProblemDetails
            {
                Title = "Belge Hatası",
                Detail = ex.Message
            };
        }
        else
        {
            _logger.LogError(exception, "Beklenmedik bir hata oluştu.");

            problem = new ProblemDetails
            {
                Title = "Server Error",
                Detail = "Beklenmedik bir hata oluştu.",
                Status = StatusCodes.Status500InternalServerError
            };

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}