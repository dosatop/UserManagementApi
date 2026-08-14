using UserManagementApi.Models.Assignments;
using UserManagementApi.Models.Attendance;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class Subject
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

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