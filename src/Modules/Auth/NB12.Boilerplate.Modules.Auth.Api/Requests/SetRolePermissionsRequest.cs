namespace NB12.Boilerplate.Modules.Auth.Api.Requests
{
    public sealed record SetRolePermissionsRequest(IReadOnlyList<string> Permissions);
}
