namespace UserManagementApi.Services.Interfaces;

public interface ITeacherAssignmentService
{
    Task<(bool Success, object? Data, string? Error)>
        AssignClassAsync(
            Guid schoolId,
            Guid teacherId,
            Guid classId);

    Task<(bool Success, object? Data, string? Error)>
        AssignSubjectAsync(
            Guid schoolId,
            Guid teacherId,
            Guid subjectId);

    Task<(bool Success, string? Error)>
        RemoveClassAsync(
            Guid schoolId,
            Guid teacherId,
            Guid classId);

    Task<(bool Success, string? Error)>
        RemoveSubjectAsync(
            Guid schoolId,
            Guid teacherId,
            Guid subjectId);
}