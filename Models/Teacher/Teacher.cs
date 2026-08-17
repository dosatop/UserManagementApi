using UserManagementApi.Models.Assignments;
using UserManagementApi.Models.Attendance;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class Teacher
{
    public Guid Id { get; set; }

    // ============================================================
    // LOGIN ACCOUNT
    // ============================================================

    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    // ============================================================
    // SCHOOL
    // ============================================================

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;

    public string EmployeeNumber { get; set; } = string.Empty;

    // ============================================================
    // CLASSES ASSIGNED TO TEACHER
    // ============================================================

    public ICollection<TeacherClass> TeacherClasses { get; set; } = [];

    // ============================================================
    // SUBJECTS TAUGHT
    // ============================================================

    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = [];

    // ============================================================
    // ASSIGNMENTS CREATED
    // ============================================================

    public ICollection<Assignment> Assignments { get; set; } = [];

    // ============================================================
    // ATTENDANCE COLLECTED
    // ============================================================

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}