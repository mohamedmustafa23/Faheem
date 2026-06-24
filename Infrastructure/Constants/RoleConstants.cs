using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Constants
{
    public static class RoleConstants
    {
        public const string Admin = nameof(Admin);
        public const string Teacher = nameof(Teacher);
        public const string Student = nameof(Student);
        public const string Parent = nameof(Parent);
        public const string Assistant = nameof(Assistant);
        public const string CenterOwner = nameof(CenterOwner);
        public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>([Admin, Teacher, Assistant, Student, Parent, CenterOwner]);

        public static bool IsDefaultRole(string roleName) =>
            DefaultRoles.Contains(roleName);
    }
}
