using NB12.Boilerplate.Modules.Auth.Application.Dtos;

namespace NB12.Boilerplate.Modules.Auth.Application.Abstractions
{
    public interface IPermissionRepository
    {
        Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct);
    }
}
