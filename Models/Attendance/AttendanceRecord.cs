using UserManagementApi.Models.Attendance;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class AttendanceRecord
{
    public Guid Id { get; set; }

    // ============================================================
    // SCHOOL
    // ============================================================

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    // ============================================================
    // STUDENT
    // ============================================================

    public Guid StudentId { get; set; }

    public StudentProfile Student { get; set; } = null!;

    // ============================================================
    // CLASS
    // ============================================================

    public Guid ClassId { get; set; }

    public Class Class { get; set; } = null!;

    // ============================================================
    // TEACHER
    // ============================================================

    public Guid TeacherId { get; set; }

    public Teacher Teacher { get; set; } = null!;

    // ============================================================
    // SUBJECT
    // ============================================================

    // Nullable because attendance can be:
    // - General/class attendance
    // - Subject-specific attendance
    public Guid? SubjectId { get; set; }

    public Subject? Subject { get; set; }

    // ============================================================
    // ATTENDANCE DATE
    // ============================================================

    public DateTime AttendanceDate { get; set; }

    // ============================================================
    // STATUS
    // ============================================================

    public AttendanceStatus Status { get; set; }

    // ============================================================
    // OPTIONAL INFORMATION
    // ============================================================

    public string? Remarks { get; set; }

    // ============================================================
    // ACADEMIC PERIOD
    // ============================================================

    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    // ============================================================
    // AUDIT
    // ============================================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}