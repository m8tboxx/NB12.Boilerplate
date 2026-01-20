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
            Console.WriteLine($">>> UOW SaveChanges on: {_authDbContext.GetType().FullName}");
            return _authDbContext.SaveChangesAsync(ct);
        }
    }
}
