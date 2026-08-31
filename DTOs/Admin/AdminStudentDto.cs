using UserManagementApi.Models.SchoolModels;

public class AdminStudentDto
{
    public Guid StudentId { get; set; }
    public string? StudentNumber { get; set; }

    public Guid SchoolId { get; set; }
    public string? SchoolName { get; set; }

    public string? StudentName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public Guid ClassId { get; set; }
    public string? ClassName { get; set; }

    // ============================================================
    // SCHOOL LEVEL
    // ============================================================

    public SchoolLevel SchoolLevel { get; set; }
    public int SchoolLevelId => (int)SchoolLevel;

    // ============================================================
    // DEPARTMENT
    // ============================================================

    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    // ============================================================
    // TRADE
    // ============================================================

    public Guid? TradeId { get; set; }
    public string? TradeName { get; set; }

    public Guid? TradeSubjectId { get; set; }
    public string? TradeSubjectName { get; set; }
    public string? TradeSubjectCode { get; set; }

    public List<AdminParentDto> Parents { get; set; } = [];

    public List<AdminStudentSubjectDto> Subjects { get; set; } = [];

    public List<AdminStudentResultDto> Results { get; set; } = [];

    public List<AdminStudentAttendanceDto> Attendance { get; set; } = [];

    public List<AdminStudentAssignmentDto> Assignments { get; set; } = [];
}
