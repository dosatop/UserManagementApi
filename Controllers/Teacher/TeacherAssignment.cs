using UserManagementApi.Models.Attendance;

namespace UserManagementApi.DTOs.TeacherPortal;

// ================================================================
// ASSIGNMENTS
// ================================================================

public class CreateAssignmentRequest
{
    public Guid ClassId { get; set; }

    public Guid SubjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? AttachmentUrl { get; set; }

    public string? DueDate { get; set; }
}

public class UpdateAssignmentRequest
{
    public Guid ClassId { get; set; }

    public Guid SubjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? AttachmentUrl { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsPublished { get; set; }
}

public class GetTeacherAssignmentsRequest
{
    public Guid? ClassId { get; set; }

    public Guid? SubjectId { get; set; }

    public string? Session { get; set; }

    public string? Term { get; set; }
}

// ================================================================
// ATTENDANCE
// ================================================================

public class CreateAttendanceRequest
{
    public Guid StudentId { get; set; }

    public Guid ClassId { get; set; }

    public Guid? SubjectId { get; set; }

    public DateTime AttendanceDate { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Remarks { get; set; }
}

public class UpdateAttendanceRequest
{
    public AttendanceStatus Status { get; set; }

    public string? Remarks { get; set; }
}

public class GetTeacherAttendanceRequest
{
    public Guid? StudentId { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? SubjectId { get; set; }

    public DateTime? Date { get; set; }

    public string? Session { get; set; }

    public string? Term { get; set; }
}