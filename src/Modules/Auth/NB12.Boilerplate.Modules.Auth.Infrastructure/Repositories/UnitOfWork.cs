using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Repositories
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly AuthDbContext _authDbContext;

        public UnitOfWork(AuthDbContext authDbContext)
        {
            _authDbContext = authDbContext;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            return _authDbContext.SaveChangesAsync(ct);
        }
    }
}
