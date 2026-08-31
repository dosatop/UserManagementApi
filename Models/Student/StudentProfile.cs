using UserManagementApi.Models.Assignments;
using UserManagementApi.Models.Attendance;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class StudentProfile
{
    public Guid Id { get; set; }

    // ============================================================
    // CLASS
    // ============================================================

    public Guid ClassId { get; set; }

    public Class Class { get; set; } = null!;

    // ============================================================
    // LOGIN ACCOUNT
    // ============================================================

    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    // ============================================================
    // SCHOOL LEVEL
    // ============================================================

    public SchoolLevel SchoolLevel { get; set; }

    // ============================================================
    // DEPARTMENT
    // ============================================================

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }


    // ============================================================
    // TRADE
    // ============================================================

    public Guid? TradeId { get; set; }

    public Trade? Trade { get; set; }

    public Guid? TradeSubjectId { get; set; }

    public Subject? TradeSubject { get; set; }


    // ============================================================
    // SCHOOL
    // ============================================================

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    // ============================================================
    // STUDENT INFORMATION
    // ============================================================

    public string StudentNumber { get; set; } = string.Empty;

    // ============================================================
    // PARENTS
    // ============================================================

    public ICollection<ParentStudent> Parents { get; set; } = [];

    // ============================================================
    // ATTENDANCE
    // ============================================================

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];

    // ============================================================
    // ASSIGNMENT SUBMISSIONS
    // ============================================================

    public ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = [];
}