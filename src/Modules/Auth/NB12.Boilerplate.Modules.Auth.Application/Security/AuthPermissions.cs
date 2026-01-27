using NB12.Boilerplate.BuildingBlocks.Application.Security;

namespace NB12.Boilerplate.Modules.Auth.Application.Security
{
    public static class AuthPermissions
    {
        public static class Auth
        {
            public const string PermissionsRead = "auth.permissions.read";
            public const string RolesRead = "auth.roles.read";
            public const string RolesWrite = "auth.roles.write";
            public const string UsersRolesRead = "auth.users.roles.read";
            public const string UsersRolesWrite = "auth.users.roles.write";

            public const string UsersWrite = "auth.users.write";

            public const string MeRead = "auth.me.read";

            // Outbox Admin
            public const string OutboxRead = "auth.outbox.read";
            public const string OutboxReplay = "auth.outbox.replay";
            public const string OutboxDelete = "auth.outbox.delete";

            // Ops / Operational Dashboard
            public const string OpsRead = "ops.read";
            public const string OpsWrite = "ops.write";
        }

        public static IReadOnlyList<PermissionDefinition> All { get; } =
        [
            new(Auth.PermissionsRead, "Read permissions", "List available permissions.", "Auth"),

            new(Auth.RolesRead, "Read roles", "List roles and their permissions.", "Auth"),
            new(Auth.RolesWrite, "Manage roles", "Create/rename/delete roles and manage role permissions.", "Auth"),

            new(Auth.UsersWrite, "Manage users", "Create users.", "Auth"),

            new(Auth.UsersRolesRead, "Read user roles", "Read roles assigned to users.", "Auth"),
            new(Auth.UsersRolesWrite, "Manage user roles", "Assign/remove roles to/from users.", "Auth"),

            new(Auth.MeRead, "Read my profile", "Read current user's profile.", "Auth"),

            // Outbox Admin
            new(Auth.OutboxRead, "Read outbox", "List outbox messages (including failed).", "Auth"),
            new(Auth.OutboxReplay, "Replay outbox messages", "Reset & replay outbox messages.", "Auth"),
            new(Auth.OutboxDelete, "Delete outbox messages", "Delete outbox messages (maintenance).", "Auth"),

            // Ops
            new(Auth.OpsRead, "Read ops dashboard", "Read operational status dashboard.", "Ops"),
            new(Auth.OpsWrite, "Write ops", "Wrirte operational data", "Ops")
        ];
    }
}
