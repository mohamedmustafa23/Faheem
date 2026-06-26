using Application.Interfaces;
using Domain.Contracts;
using Domain.Entities;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Contexts
{
    public class ApplicationDbContext : BaseDbContext
    {
        private readonly ICurrentUserService _currentUserService;
        public ApplicationDbContext(IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor, DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
            : base(tenantContextAccessor, options)
        {
            _currentUserService = currentUserService;
        }

        // Identity
        public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
        public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();
        public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

        // Academics
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<SessionOccurrence> SessionOccurrences => Set<SessionOccurrence>();
        public DbSet<GroupStudent> GroupStudents => Set<GroupStudent>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

        // Grades
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<StudentGrade> StudentGrades => Set<StudentGrade>();

        // Notifications & Content
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<UserDevice> UserDevices => Set<UserDevice>();
        public DbSet<GroupAnnouncement> GroupAnnouncements => Set<GroupAnnouncement>();
        public DbSet<Material> Materials => Set<Material>();

        // Payments
        public DbSet<PaymentCycle> PaymentCycles => Set<PaymentCycle>();
        public DbSet<StudentPaymentRecord> StudentPaymentRecords => Set<StudentPaymentRecord>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            ApplySmartTenantFilter(builder);
        }

        private void ApplySmartTenantFilter(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (!typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
                    continue;

                // Group carries an extra ownership rule on top of the tenant scope.
                if (entityType.ClrType == typeof(Group))
                {
                    entityType.SetQueryFilter(BuildGroupFilter());
                    continue;
                }

                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(BuildTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                var filter = method?.Invoke(this, new object[] { });

                if (filter != null)
                    entityType.SetQueryFilter((LambdaExpression)filter);
            }
        }

        private LambdaExpression BuildTenantFilter<TEntity>() where TEntity : class, IMustHaveTenant
        {
            Expression<Func<TEntity, bool>> filter = x =>
                _currentUserService.IsGlobalUser || x.TenantId == _currentUserService.TenantId;

            return filter;
        }

        // Same tenant scope, plus: a center member teacher only sees groups they own.
        // Owners and assistants (and individual-workspace teachers, who are Owners) see
        // every group in the workspace.
        private LambdaExpression BuildGroupFilter()
        {
            Expression<Func<Group, bool>> filter = g =>
                _currentUserService.IsGlobalUser
                || (g.TenantId == _currentUserService.TenantId
                    && (!_currentUserService.IsWorkspaceMemberTeacher
                        || g.OwnerUserId == _currentUserService.UserId));

            return filter;
        }
    }
}
