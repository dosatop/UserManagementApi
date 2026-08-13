public class SchoolManagementDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public int AdminCount { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public int ClassCount { get; set; }
    public int SubjectCount { get; set; }
}