using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models
{
    public class Class
    {
        public Guid Id { get; set; }

        public Guid SchoolId { get; set; }

        public School School { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public string? Section { get; set; }

        public int? AcademicYear { get; set; }

        // Students in this class
        public ICollection<StudentProfile> Students { get; set; } = [];

        // Teachers assigned to this class
        public ICollection<TeacherClass> TeacherClasses { get; set; } = [];
    }
}