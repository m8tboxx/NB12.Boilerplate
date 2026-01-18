using System;
using System.Collections.Generic;
using System.Text;

namespace NB12.Boilerplate.Modules.Auth.Application.Contracts
{
    public sealed record UserTokenData(
        string UserId,
        string Email,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions,
        string SecurityStamp);
}
