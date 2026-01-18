using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Dtos;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Repositories
{
    internal sealed class PermissionRepository : IPermissionRepository
    {
        private readonly AuthDbContext _db;

        public PermissionRepository(AuthDbContext db) => _db = db;

        public async Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct)
        {
            var list = await _db.Permissions
                .OrderBy(p => p.Module).ThenBy(p => p.Key)
                .Select(p => new PermissionDto(p.Key, p.DisplayName, p.Description, p.Module))
                .ToListAsync(ct);

            return list.AsReadOnly();
        }
    }
}
