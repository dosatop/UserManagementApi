using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class Teacher
{
    public Guid Id { get; set; }

    // Login account
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    // School
    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    public string EmployeeNumber { get; set; } = string.Empty;

    // Classes assigned to teacher
    public ICollection<TeacherClass> TeacherClasses { get; set; } = [];

    // Subjects taught
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = [];
}