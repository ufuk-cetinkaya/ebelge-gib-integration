using Application.Contracts;
using Application.DTOs;
using System.Net.Http.Json;

namespace Application.Services;

public class GibUserClient : IGibUserClient
{
    private readonly HttpClient _http;

    public GibUserClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<GibUserDto?> GetAsync(GetGibUserRequest request)
    {
        var response = await _http.GetAsync(
            $"/gib-users?identifier={request.Identifier}&documenttype={request.DocumentType}&unit={request.Unit}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GibUserDto>();
    }
}
