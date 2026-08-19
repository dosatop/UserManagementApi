namespace UserManagementApi.DTOs.Admin;

public class AdminDashboardDto
{
    public Guid SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;

    public int UserCount { get; set; }
    public int AdminCount { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public int ClassCount { get; set; }
    public int SubjectCount { get; set; }
    public int ParentCount { get; set; }
}
public class AdminTeacherDto
{
    public Guid TeacherId { get; set; }
    public string UserId { get; set; }
    public Guid SchoolId { get; set; }

    public string TeacherName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public bool IsClassTeacher { get; set; }

    public AdminClassTeacherDto? ClassTeacher { get; set; }

    public List<AdminTeacherSubjectDto> Subjects { get; set; } = [];
}

public class AdminTeacherSubjectDto
{
    public Guid SubjectId { get; set; }

    public string? SubjectName { get; set; }

    public string? Code { get; set; }

    // Class this teacher teaches the subject to
    public Guid? ClassId { get; set; }

    public string? ClassName { get; set; }
}

public class AdminStudentDto
{
    public Guid StudentId { get; set; }
    public string? StudentNumber { get; set; }

    public Guid SchoolId { get; set; }
    public string? SchoolName { get; set; }

    public string? StudentName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public Guid? ClassId { get; set; }
    public string? ClassName { get; set; }

    public List<AdminParentDto> Parents { get; set; } = [];
}

public class AdminParentDto
{
    public Guid ParentId { get; set; }

    public string? ParentName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class AdminClassDto
{
    public Guid ClassId { get; set; }
    public string? ClassName { get; set; }
    public Guid SchoolId { get; set; }

    public Guid? ClassTeacherId { get; set; }

public string? ClassTeacherName { get; set; }

    public List<AdminClassStudentDto> Students { get; set; } = [];
    public List<AdminClassTeacherDto> Teachers { get; set; } = [];
}

public class AdminClassStudentDto
{
    public Guid StudentId { get; set; }
    public string? StudentNumber { get; set; }
    public string? StudentName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class AdminClassTeacherDto
{
    public Guid TeacherId { get; set; }

    public string? TeacherName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public Guid SubjectId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; }

    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
}

public class AdminTeacherClassTeacherDto
{
    public Guid TeacherId { get; set; }

    public string TeacherName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public Guid ClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;
}

public class AdminSubjectDto
{
    public Guid SubjectId { get; set; }
    public Guid SchoolId { get; set; }

    public string? SubjectName { get; set; }
    public string? Code { get; set; }

    public List<AdminSubjectTeacherDto> TeacherSubjects { get; set; } = [];
}

public class AdminSubjectTeacherDto
{
    public Guid TeacherId { get; set; }

    public string? TeacherName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}