public class UpdateStudentRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public Guid ClassRoomId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? TradeId { get; set; }

    public Guid? TradeSubjectId { get; set; }
}
