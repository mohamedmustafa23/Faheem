using Finbuckle.MultiTenant;
using Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Contexts
{
    internal class DbConfiguration
    {
        internal class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
        {
            public void Configure(EntityTypeBuilder<ApplicationUser> builder)
            {
                builder.ToTable("Users", "Identity");

                builder.Property(u => u.StudentCode).HasMaxLength(12);

                // Unique only among accounts that actually have a code (ghost students).
                builder.HasIndex(u => u.StudentCode)
                    .IsUnique()
                    .HasFilter("[StudentCode] IS NOT NULL");
            }
        }

        internal class ApplicationRoleConfig : IEntityTypeConfiguration<ApplicationRole>
        {
            public void Configure(EntityTypeBuilder<ApplicationRole> builder)
            {
                builder.ToTable("Roles", "Identity");

                builder.HasIndex(r => r.NormalizedName)
                    .HasDatabaseName("RoleNameIndex")
                    .IsUnique(false);
            }
        }

        internal class ApplicationRoleClaimConfig : IEntityTypeConfiguration<ApplicationRoleClaim>
        {
            public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder)
            {
                builder.ToTable("RoleClaims", "Identity");
            }
        }

        internal class IdentityUserRoleConfig : IEntityTypeConfiguration<IdentityUserRole<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
            {
                builder.ToTable("UserRoles", "Identity");
            }
        }

        internal class IdentityUserClaimConfig : IEntityTypeConfiguration<IdentityUserClaim<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> builder)
            {
                builder.ToTable("UserClaims", "Identity");
            }
        }

        internal class IdentityUserLoginConfig : IEntityTypeConfiguration<IdentityUserLogin<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder)
            {
                builder.ToTable("UserLogins", "Identity");
            }
        }

        internal class IdentityUserTokenConfig : IEntityTypeConfiguration<IdentityUserToken<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityUserToken<string>> builder)
            {
                builder.ToTable("UserTokens", "Identity");
            }
        }

        internal class StudentProfileConfig : IEntityTypeConfiguration<StudentProfile>
        {
            public void Configure(EntityTypeBuilder<StudentProfile> builder)
            {
                builder.ToTable("StudentProfiles", "Identity");

                builder
                    .HasOne(s => s.User)
                    .WithOne(u => u.StudentProfile)
                    .HasForeignKey<StudentProfile>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.Property(s => s.EducationalStage)
                    .HasMaxLength(50)
                    .IsRequired();

                builder.Property(s => s.GradeYear)
                    .HasMaxLength(50)
                    .IsRequired();
            }
        }

        internal class ParentStudentLinkConfig : IEntityTypeConfiguration<ParentStudentLink>
        {
            public void Configure(EntityTypeBuilder<ParentStudentLink> builder)
            {
                builder.ToTable("ParentStudentLinks", "Identity");

                builder
                    .HasOne(l => l.Parent)
                    .WithMany(u => u.MyChildren)
                    .HasForeignKey(l => l.ParentUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder
                    .HasOne(l => l.Student)
                    .WithMany(u => u.MyParentRequests)
                    .HasForeignKey(l => l.StudentUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder
                    .HasIndex(l => new { l.ParentUserId, l.StudentUserId })
                    .IsUnique();

                builder.Property(l => l.Status)
                    .IsRequired();

                builder.Property(l => l.RequestedAt)
                    .IsRequired();
            }
        }

        internal class EmailVerificationConfig : IEntityTypeConfiguration<EmailVerification>
        {
            public void Configure(EntityTypeBuilder<EmailVerification> builder)
            {
                builder.ToTable("EmailVerifications", "Identity");

                builder
                    .HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.Property(e => e.OtpCode)
                    .HasMaxLength(100)
                    .IsRequired();

                builder.Property(e => e.Purpose)
                    .HasMaxLength(50)
                    .IsRequired();

                builder.Property(e => e.ExpiresAt)
                    .IsRequired();
            }
        }

        internal class UserRefreshTokenConfig : IEntityTypeConfiguration<UserRefreshToken>
        {
            public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
            {
                builder.ToTable("UserRefreshTokens", "Identity");

                builder.Property(t => t.TokenHash)
                    .HasMaxLength(256)
                    .IsRequired();

                builder.Property(t => t.JwtId)
                    .HasMaxLength(128)
                    .IsRequired();

                builder.HasIndex(t => new { t.UserId, t.TokenHash });
            }
        }

        internal class WorkspaceMemberConfig : IEntityTypeConfiguration<WorkspaceMember>
        {
            public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
            {
                builder.ToTable("WorkspaceMembers", "Identity");

                builder.Property(m => m.UserId).IsRequired();
                builder.Property(m => m.TenantId).HasMaxLength(64).IsRequired();

                builder.Property(m => m.Role)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                builder.Property(m => m.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                builder.Property(m => m.CreatedAt).IsRequired();

                // Membership is meaningless without its user — cascade on user delete.
                // WorkspaceMembers is reachable only via this FK, so there's no
                // multiple-cascade-path conflict.
                builder.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // A user can belong to a workspace once.
                builder.HasIndex(m => new { m.UserId, m.TenantId }).IsUnique();

                // Fast lookup of all members of a workspace (center member lists).
                builder.HasIndex(m => m.TenantId);
            }
        }

        internal class GroupConfig : IEntityTypeConfiguration<Domain.Entities.Group>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.Group> builder)
            {
                builder.ToTable("Groups", "Academics")
                    .IsMultiTenant();

                builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
                builder.Property(g => g.Subject).HasMaxLength(100).IsRequired();
                builder.Property(g => g.EducationalStage).HasMaxLength(100).IsRequired();
                builder.Property(g => g.GradeYear).HasMaxLength(50).IsRequired();
                builder.Property(g => g.TenantId).HasMaxLength(64).IsRequired();

                builder.Property(g => g.EnrollmentCode).HasMaxLength(6).IsRequired();
                builder.HasIndex(g => g.EnrollmentCode).IsUnique();

                // Owning teacher (matters when a center shares one tenant across teachers).
                builder.Property(g => g.OwnerUserId).HasMaxLength(450);
                builder.HasIndex(g => g.OwnerUserId).HasFilter("[OwnerUserId] IS NOT NULL");

                builder.Property(g => g.Status)
                       .HasConversion<string>()
                       .HasMaxLength(20)
                       .IsRequired();

                // Use explicit precision to avoid silent truncation
                builder.Property(g => g.MonthlyFee)
                       .HasColumnType("decimal(10,2)");
            }
        }
    

        internal class SessionConfig : IEntityTypeConfiguration<Domain.Entities.Session>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.Session> builder)
            {
                builder.ToTable("Sessions", "Academics")
                    .IsMultiTenant();

                builder.Property(s => s.TenantId).HasMaxLength(64).IsRequired();

                builder.HasOne(s => s.Group)
                    .WithMany(g => g.Sessions)
                    .HasForeignKey(s => s.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        }

        internal class GroupStudentConfig : IEntityTypeConfiguration<Domain.Entities.GroupStudent>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.GroupStudent> builder)
            {
                builder.ToTable("GroupStudents", "Academics")
                    .IsMultiTenant();

                builder.Property(gs => gs.TenantId).HasMaxLength(64).IsRequired();

                builder.HasOne(gs => gs.Group)
                    .WithMany(g => g.Students)
                    .HasForeignKey(gs => gs.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(gs => gs.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasIndex(gs => new { gs.GroupId, gs.StudentId }).IsUnique();
            }
        }

        internal class SessionOccurrenceConfig : IEntityTypeConfiguration<Domain.Entities.SessionOccurrence>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.SessionOccurrence> builder)
            {
                builder.ToTable("SessionOccurrences", "Academics")
                    .IsMultiTenant();

                builder.Property(o => o.TenantId).HasMaxLength(64).IsRequired();

                builder.Property(o => o.PaymentMode)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                builder.Property(o => o.SessionPrice)
                    .HasColumnType("decimal(10,2)");

                // Nullable FK — standalone occurrences have no parent schedule
                builder.HasOne(o => o.Session)
                    .WithMany(s => s.Occurrences)
                    .HasForeignKey(o => o.SessionId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(o => o.Group)
                    .WithMany()
                    .HasForeignKey(o => o.GroupId)
                    .OnDelete(DeleteBehavior.NoAction);

                // For AddToCycle occurrences: link back to the source cycle.
                // SetNull on cycle delete preserves the occurrence (cycle deletion is rare; group delete cascades).
                builder.HasOne(o => o.PaymentCycle)
                    .WithMany()
                    .HasForeignKey(o => o.PaymentCycleId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);

                // Unique per schedule per date (only when SessionId is not null)
                builder.HasIndex(o => new { o.SessionId, o.OccurrenceDate })
                    .IsUnique()
                    .HasFilter("[SessionId] IS NOT NULL");

                builder.HasIndex(o => o.PaymentCycleId)
                    .HasFilter("[PaymentCycleId] IS NOT NULL");
            }
        }

        internal class AttendanceRecordConfig : IEntityTypeConfiguration<Domain.Entities.AttendanceRecord>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.AttendanceRecord> builder)
            {
                builder.ToTable("AttendanceRecords", "Academics")
                    .IsMultiTenant();

                builder.Property(a => a.TenantId).HasMaxLength(64).IsRequired();
                builder.Property(a => a.StudentId).IsRequired();

                builder.HasOne(a => a.Occurrence)
                    .WithMany(o => o.AttendanceRecords)
                    .HasForeignKey(a => a.OccurrenceId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasIndex(a => new { a.OccurrenceId, a.StudentId }).IsUnique();
            }
        }

        internal class ExamConfig : IEntityTypeConfiguration<Domain.Entities.Exam>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.Exam> builder)
            {
                builder.ToTable("Exams", "Academics").IsMultiTenant();
                builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
                builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
                builder.Property(e => e.MaxScore).HasColumnType("decimal(18,2)");

                builder.HasOne(e => e.Group)
                    .WithMany()
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        }

        internal class StudentGradeConfig : IEntityTypeConfiguration<Domain.Entities.StudentGrade>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.StudentGrade> builder)
            {
                builder.ToTable("StudentGrades", "Academics").IsMultiTenant();
                builder.Property(sg => sg.TenantId).HasMaxLength(64).IsRequired();
                builder.Property(sg => sg.Score).HasColumnType("decimal(18,2)");

                builder.HasOne(sg => sg.Exam)
                    .WithMany(e => e.StudentGrades)
                    .HasForeignKey(sg => sg.ExamId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasIndex(sg => new { sg.ExamId, sg.StudentId }).IsUnique();
            }
        }

        internal class NotificationConfig : IEntityTypeConfiguration<Domain.Entities.Notification>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.Notification> builder)
            {
                builder.ToTable("Notifications", "Communication")
                       .IsMultiTenant(); 

                builder.Property(n => n.TenantId).HasMaxLength(64).IsRequired();
                builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
                builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
                builder.Property(n => n.UserId).IsRequired();
                // Deep-link route (e.g. /parent/children/{id}) — capped so a
                // malformed value can never blow up the row.
                builder.Property(n => n.Route).HasMaxLength(300);
            }
        }

        internal class UserDeviceConfig : IEntityTypeConfiguration<Domain.Entities.UserDevice>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.UserDevice> builder)
            {
                builder.ToTable("UserDevices", "Communication");

                builder.Property(d => d.UserId).IsRequired();
                builder.Property(d => d.FcmToken).IsRequired();

                builder.HasIndex(d => new { d.UserId, d.FcmToken }).IsUnique();
            }
        }

        internal class MaterialConfig : IEntityTypeConfiguration<Domain.Entities.Material>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.Material> builder)
            {
                builder.ToTable("Materials", "Academics").IsMultiTenant();

                builder.Property(m => m.TenantId).HasMaxLength(64).IsRequired();
                builder.Property(m => m.Title).HasMaxLength(150).IsRequired();
                builder.Property(m => m.FileUrl).IsRequired();

                builder.HasOne(m => m.Group)
                    .WithMany()
                    .HasForeignKey(m => m.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        }
        internal class GroupAnnouncementConfig : IEntityTypeConfiguration<Domain.Entities.GroupAnnouncement>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.GroupAnnouncement> builder)
            {
                builder.ToTable("GroupAnnouncements", "Communication").IsMultiTenant();

                builder.Property(a => a.TenantId).HasMaxLength(64).IsRequired();
                builder.Property(a => a.Message).HasMaxLength(1000).IsRequired();

                builder.HasOne(a => a.Group)
                    .WithMany()
                    .HasForeignKey(a => a.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        }

        internal class PaymentCycleConfig : IEntityTypeConfiguration<Domain.Entities.PaymentCycle>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.PaymentCycle> builder)
            {
                builder.ToTable("PaymentCycles", "Academics", t =>
                {
                    t.HasCheckConstraint("CK_PaymentCycle_SessionsCompleted_NonNegative",
                        "[SessionsCompleted] >= 0");
                }).IsMultiTenant();

                builder.Property(c => c.TenantId).HasMaxLength(64).IsRequired();

                builder.Property(c => c.BaseFee)
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0m);

                builder.Property(c => c.ExtraFee)
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0m);

                builder.Property(c => c.RowVersion)
                    .IsRowVersion();

                builder.HasOne(c => c.Group)
                    .WithMany(g => g.PaymentCycles)
                    .HasForeignKey(c => c.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasIndex(c => new { c.GroupId, c.CycleNumber }).IsUnique();

                builder.HasIndex(c => new { c.GroupId, c.IsCompleted });
            }
        }

        internal class StudentPaymentRecordConfig : IEntityTypeConfiguration<Domain.Entities.StudentPaymentRecord>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.StudentPaymentRecord> builder)
            {
                builder.ToTable("StudentPaymentRecords", "Academics", t =>
                {
                    // Exactly one of PaymentCycleId / OccurrenceId must be set.
                    t.HasCheckConstraint("CK_StudentPaymentRecord_CycleXorOccurrence",
                        "([PaymentCycleId] IS NOT NULL AND [OccurrenceId] IS NULL) OR " +
                        "([PaymentCycleId] IS NULL AND [OccurrenceId] IS NOT NULL)");
                    // Discount must be non-negative and never exceed the expected amount.
                    t.HasCheckConstraint("CK_StudentPaymentRecord_DiscountValid",
                        "[DiscountAmount] >= 0 AND [DiscountAmount] <= [ExpectedAmount]");
                }).IsMultiTenant();

                builder.Property(r => r.TenantId).HasMaxLength(64).IsRequired();
                builder.Property(r => r.StudentId).IsRequired();

                builder.Property(r => r.ExpectedAmount)
                    .HasColumnType("decimal(10,2)");

                builder.Property(r => r.DiscountAmount)
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0m);

                builder.Property(r => r.DiscountReason)
                    .HasMaxLength(200);

                builder.Property(r => r.RowVersion)
                    .IsRowVersion();

                // Cycle-linked record
                builder.HasOne(r => r.PaymentCycle)
                    .WithMany(c => c.StudentRecords)
                    .HasForeignKey(r => r.PaymentCycleId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                // One record per student per cycle
                builder.HasIndex(r => new { r.PaymentCycleId, r.StudentId })
                    .IsUnique()
                    .HasFilter("[PaymentCycleId] IS NOT NULL");

                // One record per student per standalone occurrence
                builder.HasIndex(r => new { r.OccurrenceId, r.StudentId })
                    .IsUnique()
                    .HasFilter("[OccurrenceId] IS NOT NULL");

                // Common filter when teacher dashboard lists pending records.
                builder.HasIndex(r => new { r.GroupId, r.Status });
            }
        }

        internal class PaymentTransactionConfig : IEntityTypeConfiguration<Domain.Entities.PaymentTransaction>
        {
            public void Configure(EntityTypeBuilder<Domain.Entities.PaymentTransaction> builder)
            {
                builder.ToTable("PaymentTransactions", "Academics").IsMultiTenant();

                builder.Property(t => t.TenantId).HasMaxLength(64).IsRequired();

                builder.Property(t => t.Amount)
                    .HasColumnType("decimal(10,2)");

                builder.Property(t => t.PaidBy)
                    .HasMaxLength(450);

                builder.Property(t => t.Notes)
                    .HasMaxLength(500);

                builder.HasOne(t => t.PaymentRecord)
                    .WithMany(r => r.Transactions)
                    .HasForeignKey(t => t.StudentPaymentRecordId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        }
    }
}