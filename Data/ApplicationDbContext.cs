using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Models;
using UserManagementApi.Models.Assignments;
using UserManagementApi.Models.AuthModels;
using UserManagementApi.Models.Results;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User>(options)
{
    // ================================================================
    // AUTH
    // ================================================================

    public DbSet<RefreshToken> RefreshTokens { get; set; }


    // ================================================================
    // SCHOOL
    // ================================================================

    public DbSet<School> Schools { get; set; }

    public DbSet<Class> Classes { get; set; }

    public DbSet<Subject> Subjects { get; set; }


    // ================================================================
    // STUDENTS
    // ================================================================

    public DbSet<StudentProfile> StudentProfiles { get; set; } = null!;


    // ================================================================
    // TEACHERS
    // ================================================================

    public DbSet<Teacher> Teachers { get; set; }

    public DbSet<TeacherClass> TeacherClasses { get; set; }

    public DbSet<TeacherSubject> TeacherSubjects { get; set; }
    public DbSet<ClassTeacher> ClassTeachers { get; set; }


    // ================================================================
    // PARENTS
    // ================================================================

    public DbSet<Parent> Parents { get; set; }

    public DbSet<ParentStudent> ParentStudents { get; set; }


    // ================================================================
    // RESULTS
    // ================================================================

    public DbSet<StudentResult> StudentResults { get; set; } = null!;


    // ================================================================
    // ACADEMIC SESSIONS
    // ================================================================

    public DbSet<AcademicSession> AcademicSessions { get; set; } = null!;


    // ================================================================
    // ASSIGNMENTS
    // ================================================================

    public DbSet<Assignment> Assignments { get; set; } = null!;

    public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; } = null!;


    // ================================================================
    // ATTENDANCE
    // ================================================================

    public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        // ============================================================
        // USER
        // ============================================================

        builder.Entity<User>()
            .HasIndex(u => u.NormalizedEmail)
            .IsUnique();

        builder.Entity<User>()
            .HasOne(u => u.School)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);


        // ============================================================
        // STUDENT
        // ============================================================

        builder.Entity<StudentProfile>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<StudentProfile>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasOne(s => s.School)
            .WithMany()
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudentProfile>()
            .HasOne(s => s.Class)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudentProfile>()
            .HasIndex(s => new
            {
                s.SchoolId,
                s.StudentNumber
            })
            .IsUnique();


        // ============================================================
        // STUDENT RESULTS
        // ============================================================

        builder.Entity<StudentResult>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentResult>()
            .HasOne(x => x.School)
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudentResult>()
            .HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudentResult>()
            .HasOne(x => x.Class)
            .WithMany()
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudentResult>()
            .HasIndex(x => new
            {
                x.StudentId,
                x.SubjectId,
                x.Session,
                x.Term
            })
            .IsUnique();


        // ============================================================
        // TEACHER CLASS
        // ============================================================

        builder.Entity<TeacherClass>()
            .HasKey(tc => tc.Id);


        // Teacher → TeacherClasses
        builder.Entity<TeacherClass>()
            .HasOne(tc => tc.Teacher)
            .WithMany(t => t.TeacherClasses)
            .HasForeignKey(tc => tc.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);


        // Class → TeacherClasses
        builder.Entity<TeacherClass>()
            .HasOne(tc => tc.Class)
            .WithMany(c => c.TeacherClasses)
            .HasForeignKey(tc => tc.ClassId)
            .OnDelete(DeleteBehavior.Cascade);


        // One class can have only ONE class teacher
        builder.Entity<TeacherClass>()
     .HasIndex(tc => new
     {
         tc.TeacherId,
         tc.ClassId
     })
     .IsUnique();
        // ============================================================
        // TEACHER SUBJECT
        // ============================================================

        builder.Entity<TeacherSubject>()
            .HasKey(ts => ts.Id);


        // Teacher → TeacherSubjects
        builder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Teacher)
            .WithMany(t => t.TeacherSubjects)
            .HasForeignKey(ts => ts.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);


        // Subject → TeacherSubjects
        builder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Subject)
            .WithMany(s => s.TeacherSubjects)
            .HasForeignKey(ts => ts.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);


        // Class → TeacherSubjects
        // Class is OPTIONAL
        builder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Class)
            .WithMany(c => c.TeacherSubjects)
            .HasForeignKey(ts => ts.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
        // ============================================================
        // CLASS TEACHER
        // ============================================================

        builder.Entity<ClassTeacher>()
            .HasKey(ct => ct.Id);

        // School → ClassTeachers
        builder.Entity<ClassTeacher>()
            .HasOne<School>()
            .WithMany()
            .HasForeignKey(ct => ct.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        // Teacher → ClassTeacher
        builder.Entity<ClassTeacher>()
            .HasOne(ct => ct.Teacher)
            .WithMany()
            .HasForeignKey(ct => ct.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        // Class → ClassTeacher
        builder.Entity<ClassTeacher>()
            .HasOne(ct => ct.Class)
            .WithMany()
            .HasForeignKey(ct => ct.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // One class can have only ONE class teacher
        builder.Entity<ClassTeacher>()
            .HasIndex(ct => ct.ClassId)
            .IsUnique();

        // Prevent duplicate assignments
        builder.Entity<TeacherSubject>()
            .HasIndex(ts => new
            {
                ts.TeacherId,
                ts.SubjectId,
                ts.ClassId
            })
            .IsUnique();

        // ============================================================
        // PARENT
        // ============================================================

        builder.Entity<Parent>()
            .HasOne(p => p.User)
            .WithOne(u => u.Parent)
            .HasForeignKey<Parent>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Parent>()
            .HasOne(p => p.School)
            .WithMany()
            .HasForeignKey(p => p.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        // CLASS
        // ============================================================

        builder.Entity<Class>()
            .HasOne(c => c.School)
            .WithMany(s => s.Classes)
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);


        // ============================================================
        // SUBJECT
        // ============================================================

        builder.Entity<Subject>()
            .HasOne(s => s.School)
            .WithMany(s => s.Subjects)
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);


        // ============================================================
        // PARENT STUDENT
        // ============================================================

        builder.Entity<ParentStudent>()
            .HasKey(ps => new
            {
                ps.ParentId,
                ps.StudentId
            });

        builder.Entity<ParentStudent>()
            .HasOne(ps => ps.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(ps => ps.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ParentStudent>()
            .HasOne(ps => ps.Student)
            .WithMany(s => s.Parents)
            .HasForeignKey(ps => ps.StudentId)
            .OnDelete(DeleteBehavior.Cascade);


        // ============================================================
        // ACADEMIC SESSION
        // ============================================================

        builder.Entity<AcademicSession>()
            .HasKey(x => x.Id);

        builder.Entity<AcademicSession>()
            .HasOne(x => x.School)
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AcademicSession>()
            .Property(x => x.Session)
            .IsRequired();

        builder.Entity<AcademicSession>()
            .Property(x => x.Term)
            .IsRequired();

        // Index for finding the current session for a school
        builder.Entity<AcademicSession>()
            .HasIndex(x => new
            {
                x.SchoolId,
                x.IsCurrent
            });


        // ============================================================
        // ASSIGNMENTS
        // ============================================================

        builder.Entity<Assignment>(entity =>
        {
            entity.HasKey(a => a.Id);

            // --------------------------------------------------------
            // SCHOOL
            // --------------------------------------------------------

            entity.HasOne(a => a.School)
                .WithMany()
                .HasForeignKey(a => a.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);


            // --------------------------------------------------------
            // TEACHER
            // --------------------------------------------------------

            entity.HasOne(a => a.Teacher)
          .WithMany(t => t.Assignments)
          .HasForeignKey(a => a.TeacherId)
          .OnDelete(DeleteBehavior.Restrict);

            // --------------------------------------------------------
            // CLASS
            // --------------------------------------------------------

            entity.HasOne(a => a.Class)
                .WithMany()
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Restrict);


            // --------------------------------------------------------
            // SUBJECT
            // --------------------------------------------------------

            entity.HasOne(a => a.Subject)
      .WithMany(s => s.Assignments)
      .HasForeignKey(a => a.SubjectId)
      .OnDelete(DeleteBehavior.Restrict);
            // --------------------------------------------------------
            // REQUIRED FIELDS
            // --------------------------------------------------------

            entity.Property(a => a.Title)
                .IsRequired();

            entity.Property(a => a.Session)
                .IsRequired();

            entity.Property(a => a.Term)
                .IsRequired();


            // --------------------------------------------------------
            // DATES
            // --------------------------------------------------------

            entity.Property(a => a.AssignedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(a => a.CreatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(a => a.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(a => a.DueDate)
                .HasColumnType("timestamp with time zone");
        });


        // ============================================================
        // ASSIGNMENT SUBMISSIONS
        // ============================================================

        builder.Entity<AssignmentSubmission>()
            .HasOne(x => x.Assignment)
            .WithMany(x => x.Submissions)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AssignmentSubmission>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AssignmentSubmission>()
            .HasIndex(x => new
            {
                x.AssignmentId,
                x.StudentId
            })
            .IsUnique();


        // ============================================================
        // ATTENDANCE
        // ============================================================

        builder.Entity<AttendanceRecord>()
            .HasOne(x => x.School)
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AttendanceRecord>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AttendanceRecord>()
            .HasOne(x => x.Class)
            .WithMany()
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AttendanceRecord>()
            .HasOne(x => x.Teacher)
            .WithMany(t => t.AttendanceRecords)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AttendanceRecord>()
     .HasOne(x => x.Subject)
     .WithMany(s => s.AttendanceRecords)
     .HasForeignKey(x => x.SubjectId)
     .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AttendanceRecord>()
            .HasIndex(x => new
            {
                x.StudentId,
                x.ClassId,
                x.SubjectId,
                x.AttendanceDate,
                x.Session,
                x.Term
            })
            .IsUnique();
    }
}