using UserManagementApi.Models.Attendance;

namespace UserManagementApi.DTOs.Attendance;

public class BulkAttendanceItem
{
    public Guid StudentId { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Remarks { get; set; }
}

public class CreateBulkAttendanceRequest
{
    public Guid ClassId { get; set; }

    public DateTime AttendanceDate { get; set; }

    public List<BulkAttendanceItem> Students { get; set; } = new();
}
