using UserManagementApi.Models.Assignments;
using UserManagementApi.Models.Attendance;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public enum SubjectType
{
    General = 1,
    Department = 2,
    Trade = 3
}

public class Subject
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    // ============================================================
    // SUBJECT TYPE
    // ============================================================

    public SubjectType Type { get; set; }

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

     public ICollection<ClassSubject> ClassSubjects { get; set; } = [];

    // ============================================================
    // TEACHERS
    // ============================================================

    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = [];

    // ============================================================
    // ASSIGNMENTS
    // ============================================================

    public ICollection<Assignment> Assignments { get; set; } = [];

    // ============================================================
    // ATTENDANCE
    // ============================================================

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}