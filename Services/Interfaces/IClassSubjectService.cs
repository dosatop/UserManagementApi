namespace UserManagementApi.Services.Interfaces;

public interface IClassSubjectService
{
    Task<(bool Success, object? Data, string? Error)>
        AssignSubjectToClassAsync(
            Guid schoolId,
            Guid classId,
            Guid subjectId);

    Task<IEnumerable<object>>
        GetClassSubjectsAsync(
            Guid schoolId,
            Guid classId);

    Task<(bool Success, string? Error)>
        RemoveSubjectFromClassAsync(
            Guid schoolId,
            Guid classId,
            Guid subjectId);
}
