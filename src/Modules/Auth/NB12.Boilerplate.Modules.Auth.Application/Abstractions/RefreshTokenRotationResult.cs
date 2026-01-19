using NB12.Boilerplate.Modules.Auth.Application.Enums;

namespace NB12.Boilerplate.Modules.Auth.Application.Abstractions
{
    public sealed record RefreshTokenRotationResult(
        RefreshTokenRotationStatus Status,
        string? UserId,
        Guid? FamilyId);
}
