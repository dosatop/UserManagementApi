using UserManagementApi.DTOs.Admin;
using UserManagementApi.DTOs.Teachers;

namespace UserManagementApi.Services;

public interface IAdminService
{
    Task<AdminDashboardDto?> GetDashboardAsync(Guid schoolId);

    Task<List<AdminTeacherDto>> GetTeachersAsync(Guid schoolId);

    Task<List<AdminStudentDto>> GetStudentsAsync(Guid schoolId);

    Task<List<AdminClassDto>> GetClassesAsync(Guid schoolId);

    Task<List<AdminSubjectDto>> GetSubjectsAsync(Guid schoolId);

    Task<AdminStudentDto?> GetStudentAsync(
    Guid schoolId,
    Guid studentId);
}
