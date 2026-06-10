using System.Collections.ObjectModel;

namespace Infrastructure.Constants
{
    public static class AppAction
    {
        public const string Read = nameof(Read);
        public const string Create = nameof(Create);
        public const string Update = nameof(Update);
        public const string Delete = nameof(Delete);
        public const string UpgradeSubscription = nameof(UpgradeSubscription);
        public const string RefreshToken = nameof(RefreshToken);
    }

    public static class AppFeature
    {
        // System and Tenancy
        public const string Tenants = nameof(Tenants);

        // Identity and Access Management
        public const string Users = nameof(Users);
        public const string Roles = nameof(Roles);
        public const string UserRoles = nameof(UserRoles);
        public const string Tokens = nameof(Tokens);

        // App Core Business Features
        public const string Enrollment = nameof(Enrollment);
        public const string Groups = nameof(Groups);
        public const string Sessions = nameof(Sessions);
        public const string Attendance = nameof(Attendance);
        public const string Grades = nameof(Grades);
        public const string Payments = nameof(Payments);
        public const string Students = nameof(Students);
        public const string Parents = nameof(Parents);
        public const string StudentLinks = nameof(StudentLinks);
    }

    public record AppPermission(string Action, string Feature, string Description, string Group, bool IsBasic = false, bool IsRoot = false)
    {
        public string Name => NameFor(Action, Feature);
        public static string NameFor(string action, string feature) => $"Permission.{feature}.{action}";
    }

    public static class AppPermissions
    {
        private static readonly AppPermission[] _allPermissions =
        [
            // ── Root / Tenancy ──────────────────────────────────────
            new(AppAction.Create,              AppFeature.Tenants, "Create Tenant",          "Tenancy", IsRoot: true),
            new(AppAction.Read,                AppFeature.Tenants, "View Tenants",           "Tenancy", IsRoot: true),
            new(AppAction.Update,              AppFeature.Tenants, "Update Tenant",          "Tenancy", IsRoot: true),
            new(AppAction.UpgradeSubscription, AppFeature.Tenants, "Upgrade Subscription",   "Tenancy", IsRoot: true),

            // ── Security — إدارة المستخدمين والصلاحيات ─────────────
            new(AppAction.Create, AppFeature.Users,     "Create Teacher",        "Security"),
            new(AppAction.Read,   AppFeature.Users,     "View Users",            "Security"),
            new(AppAction.Update, AppFeature.Users,     "Update User",           "Security"),
            new(AppAction.Delete, AppFeature.Users,     "Delete User",           "Security"),
            new(AppAction.Read,   AppFeature.Roles,     "View Roles",            "Security"),
            new(AppAction.Create, AppFeature.Roles,     "Create Roles",          "Security"),
            new(AppAction.Update, AppFeature.Roles,     "Update Roles",          "Security"),
            new(AppAction.Delete, AppFeature.Roles,     "Delete Roles",          "Security"),
            new(AppAction.Read,   AppFeature.UserRoles, "View User Roles",       "Security"),
            new(AppAction.Update, AppFeature.UserRoles, "Manage User Roles",     "Security"),

            // ── Students — المدرس يقدر يشوف ويتحكم في طلابه فقط ───
            // مفيش Create — الطالب بيعمل Sign Up بنفسه
            new(AppAction.Read,   AppFeature.Students, "View Students",          "Students", IsBasic: true),
            new(AppAction.Update, AppFeature.Students, "Edit Student Profile",   "Students"),
            new(AppAction.Delete, AppFeature.Students, "Remove Student",         "Students"),

            // ── Parents ─────────────────────────────────────────────
            new(AppAction.Read,   AppFeature.Parents,      "View Parents",       "Parents"),
            new(AppAction.Create, AppFeature.StudentLinks, "Link Parent-Student","Parents"),
            new(AppAction.Delete, AppFeature.StudentLinks, "Unlink Parent",      "Parents"),

            // ── Enrollment — الطالب بيدخل كود المجموعة ─────────────
            // ده Public مش محتاج permission — بس بعد Login
            new(AppAction.Create, AppFeature.Enrollment, "Join Group by Code",   "Enrollment", IsBasic: true),
            new(AppAction.Read,   AppFeature.Enrollment, "View My Groups",       "Enrollment", IsBasic: true),
            new(AppAction.Delete, AppFeature.Enrollment, "Leave Group",          "Enrollment", IsBasic: true),

            // ── Groups ──────────────────────────────────────────────
            new(AppAction.Create, AppFeature.Groups, "Create Groups",            "Academics"),
            new(AppAction.Read,   AppFeature.Groups, "View Groups",              "Academics", IsBasic: true),
            new(AppAction.Update, AppFeature.Groups, "Update Groups",            "Academics"),
            new(AppAction.Delete, AppFeature.Groups, "Delete Groups",            "Academics"),

            // ── Sessions ────────────────────────────────────────────
            new(AppAction.Create, AppFeature.Sessions, "Create Sessions",        "Academics"),
            new(AppAction.Read,   AppFeature.Sessions, "View Sessions",          "Academics", IsBasic: true),
            new(AppAction.Update, AppFeature.Sessions, "Update Sessions",        "Academics"),
            new(AppAction.Delete, AppFeature.Sessions, "Delete Sessions",        "Academics"),

            // ── Attendance ──────────────────────────────────────────
            new(AppAction.Create, AppFeature.Attendance, "Take Attendance",      "Attendance"),
            new(AppAction.Read,   AppFeature.Attendance, "View Attendance",      "Attendance", IsBasic: true),
            new(AppAction.Update, AppFeature.Attendance, "Edit Attendance",      "Attendance"),

            // ── Grades ──────────────────────────────────────────────
            new(AppAction.Create, AppFeature.Grades, "Add Grades",               "Grades"),
            new(AppAction.Read,   AppFeature.Grades, "View Grades",              "Grades", IsBasic: true),
            new(AppAction.Update, AppFeature.Grades, "Edit Grades",              "Grades"),

            // ── Payments ────────────────────────────────────────────
            new(AppAction.Read,   AppFeature.Payments, "View Payments",               "Payments", IsBasic: true),
            new(AppAction.Update, AppFeature.Payments, "Record / Waive / Discount",   "Payments"),
            new(AppAction.Delete, AppFeature.Payments, "Delete Payment Transactions", "Payments"),

            // Basic لكل اليوزرز
            new(AppAction.RefreshToken, AppFeature.Tokens,"Generate Refresh Token", "System Access", IsBasic: true),
        ];

        public static IReadOnlyList<AppPermission> Root { get; }
            = new ReadOnlyCollection<AppPermission>(_allPermissions.Where(p => p.IsRoot).ToArray());
        public static IReadOnlyList<AppPermission> Admin { get; }
            = new ReadOnlyCollection<AppPermission>(_allPermissions.Where(p => !p.IsRoot).ToArray());

        public static IReadOnlyList<AppPermission> Teacher { get; }
            = new ReadOnlyCollection<AppPermission>(_allPermissions.Where(p =>
                    p.Group is "Academics" or "Attendance" or "Grades" or "Payments" or "Students"
                    && !p.IsRoot).ToArray());

        public static IReadOnlyList<AppPermission> Student { get; }
            = new ReadOnlyCollection<AppPermission>(_allPermissions.Where(p =>
                    p.Group is "Enrollment"
                    || (p.Group is "Academics" or "Attendance" or "Grades" && p.IsBasic))
                    .ToArray());

        public static IReadOnlyList<AppPermission> Parent { get; }
            = new ReadOnlyCollection<AppPermission>(_allPermissions.Where(p =>
                    p.IsBasic &&
                    p.Group is "Payments" or "Attendance" or "Grades")
                    .ToArray());

        public static IReadOnlyList<AppPermission> Assistant { get; }
            = new ReadOnlyCollection<AppPermission>(_allPermissions.Where(p =>
                    (p.Group is "Attendance" or "Grades" or "Payments" or "Students") && p.Action != AppAction.Delete
                    && !p.IsRoot).ToArray());
    }
}