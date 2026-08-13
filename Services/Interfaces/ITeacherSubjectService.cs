using UserManagementApi.DTOs.Teachers;

namespace UserManagementApi.Services.Interfaces;

public interface ITeacherSubjectService
{
    Task<(bool Success, object? Data, string? Error)>
        AssignSubjectAsync(
            Guid schoolId,
            Guid teacherId,
            AssignTeacherSubjectRequest request);

    Task<IEnumerable<object>> GetTeacherSubjectsAsync(
        Guid schoolId,
        Guid teacherId);

    Task<(bool Success, string? Error)>
        RemoveSubjectAsync(
            Guid schoolId,
            Guid teacherId,
            Guid subjectId);
}