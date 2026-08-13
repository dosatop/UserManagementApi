using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Models;
using UserManagementApi.Models.AuthModels;
using UserManagementApi.Models.Results;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<School> Schools { get; set; }

    public DbSet<StudentProfile> StudentProfiles { get; set; } = null!;
    public DbSet<StudentResult> StudentResults { get; set; } = null!;

    public DbSet<Teacher> Teachers { get; set; }

    public DbSet<Parent> Parents { get; set; }

    public DbSet<Class> Classes { get; set; }

    public DbSet<Subject> Subjects { get; set; }

    public DbSet<TeacherClass> TeacherClasses { get; set; }

    public DbSet<TeacherSubject> TeacherSubjects { get; set; }

    public DbSet<ParentStudent> ParentStudents { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // -------------------------
        // USER
        // -------------------------

        builder.Entity<User>()
            .HasIndex(u => u.NormalizedEmail)
            .IsUnique();

        builder.Entity<User>()
            .HasOne(u => u.School)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);


        // -------------------------
        // STUDENT
        // -------------------------

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
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<StudentProfile>()
            .HasIndex(s => new
            {
                s.SchoolId,
                s.StudentNumber
            })
            .IsUnique();

        // -------------------------
        // STUDENT RESULTS
        // -------------------------

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

        // -------------------------
        // TEACHER
        // -------------------------

        builder.Entity<Teacher>()
            .HasOne(t => t.User)
            .WithOne(u => u.Teacher)
            .HasForeignKey<Teacher>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Teacher>()
            .HasOne(t => t.School)
            .WithMany()
            .HasForeignKey(t => t.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Teacher>()
            .HasIndex(t => new
            {
                t.SchoolId,
                t.EmployeeNumber
            })
            .IsUnique();


        // -------------------------
        // PARENT
        // -------------------------

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


        // -------------------------
        // CLASS
        // -------------------------

        builder.Entity<Class>()
            .HasOne(c => c.School)
            .WithMany(s => s.Classes)
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);


        // -------------------------
        // SUBJECT
        // -------------------------

        builder.Entity<Subject>()
            .HasOne(s => s.School)
            .WithMany(s => s.Subjects)
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);


        // -------------------------
        // TEACHER CLASS
        // -------------------------

        builder.Entity<TeacherClass>()
            .HasKey(tc => new
            {
                tc.TeacherId,
                tc.ClassId
            });

        builder.Entity<TeacherClass>()
            .HasOne(tc => tc.Teacher)
            .WithMany(t => t.TeacherClasses)
            .HasForeignKey(tc => tc.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeacherClass>()
            .HasOne(tc => tc.Class)
            .WithMany(c => c.TeacherClasses)
            .HasForeignKey(tc => tc.ClassId)
            .OnDelete(DeleteBehavior.Cascade);


        // -------------------------
        // TEACHER SUBJECT
        // -------------------------

        builder.Entity<TeacherSubject>()
            .HasKey(ts => new
            {
                ts.TeacherId,
                ts.SubjectId
            });

        builder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Teacher)
            .WithMany(t => t.TeacherSubjects)
            .HasForeignKey(ts => ts.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Subject)
            .WithMany(s => s.TeacherSubjects)
            .HasForeignKey(ts => ts.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);


        // -------------------------
        // PARENT STUDENT
        // -------------------------

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
    }
}