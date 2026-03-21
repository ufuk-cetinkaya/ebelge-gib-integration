using Application.DTOs;

namespace Application.Contracts;

public interface IGibUserClient
{
    Task<GibUserDto?> GetAsync(GetGibUserRequest request);
}
