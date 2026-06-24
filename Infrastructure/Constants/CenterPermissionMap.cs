using Domain.Enums;

namespace Infrastructure.Constants
{
    /// <summary>
    /// Translates a member's <see cref="CenterPermissions"/> bitmask into the permission
    /// claim names the authorization layer checks (<c>Permission.{Feature}.{Action}</c>).
    /// This is the single place that maps a capability flag to concrete actions — adding a
    /// new flag means adding one branch here.
    /// </summary>
    public static class CenterPermissionMap
    {
        public static IEnumerable<string> ToPermissionNames(CenterPermissions permissions)
        {
            var names = new HashSet<string>();

            void Add(string action, string feature) => names.Add(AppPermission.NameFor(action, feature));

            if (permissions.HasFlag(CenterPermissions.ManageGroups))
            {
                Add(AppAction.Create, AppFeature.Groups);
                Add(AppAction.Read, AppFeature.Groups);
                Add(AppAction.Update, AppFeature.Groups);
                Add(AppAction.Create, AppFeature.Sessions);
                Add(AppAction.Read, AppFeature.Sessions);
                Add(AppAction.Update, AppFeature.Sessions);
                Add(AppAction.Delete, AppFeature.Sessions);
            }

            if (permissions.HasFlag(CenterPermissions.DeleteGroups))
                Add(AppAction.Delete, AppFeature.Groups);

            if (permissions.HasFlag(CenterPermissions.ManageAttendance))
            {
                Add(AppAction.Create, AppFeature.Attendance);
                Add(AppAction.Read, AppFeature.Attendance);
                Add(AppAction.Update, AppFeature.Attendance);
                Add(AppAction.Read, AppFeature.Sessions);
            }

            if (permissions.HasFlag(CenterPermissions.ManageStudents))
            {
                Add(AppAction.Read, AppFeature.Students);
                Add(AppAction.Update, AppFeature.Students);
                Add(AppAction.Delete, AppFeature.Students);
                Add(AppAction.Read, AppFeature.Enrollment);
                Add(AppAction.Delete, AppFeature.Enrollment);
            }

            if (permissions.HasFlag(CenterPermissions.ManagePayments))
            {
                Add(AppAction.Read, AppFeature.Payments);
                Add(AppAction.Update, AppFeature.Payments);
                Add(AppAction.Delete, AppFeature.Payments);
            }

            if (permissions.HasFlag(CenterPermissions.ViewFinancials))
                Add(AppAction.Read, AppFeature.Payments);

            return names;
        }
    }
}
