namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Models
{
    public sealed class PermissionRecord
    {
        public string Key { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Module { get; set; } = null!;
        public DateTime UpdatedAtUtc { get; set; }
    }
}
