using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    internal sealed class AuditRetentionStatusProvider(AuditRetentionStatusState state)
        : IAuditRetentionStatusProvider
    {
        public Task<AuditRetentionStatusDto> GetAsync(CancellationToken ct)
            => Task.FromResult(state.Snapshot());

        public AuditRetentionStatusDto GetStatus()
        {
            return state.Snapshot();
        }
    }
}
