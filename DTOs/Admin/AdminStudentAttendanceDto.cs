public class AdminStudentAttendanceDto
{
    public Guid Id { get; set; }

    public DateTime AttendanceDate { get; set; }

    public string? Status { get; set; }

    public string? Remarks { get; set; }

    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }

    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }

    public Guid ClassId { get; set; }
    public Guid SchoolId { get; set; }

    public string? Session { get; set; }
    public string? Term { get; set; }
}