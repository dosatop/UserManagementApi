using UserManagementApi.DTOs.Teachers;

namespace UserManagementApi.Services.Interfaces;

public interface ITeacherService
{
    // ================================================================
    // TEACHER CRUD
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        CreateTeacherAsync(
            Guid schoolId,
            CreateTeacherRequest request);

    Task<IEnumerable<object>>
        GetTeachersAsync(
            Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        GetTeacherByIdAsync(
            Guid schoolId,
            Guid teacherId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateTeacherAsync(
            Guid schoolId,
            Guid teacherId,
            UpdateTeacherRequest request);

    Task<(bool Success, object? Data, string? Error)>
        DeleteTeacherAsync(
            Guid schoolId,
            Guid teacherId);


    // ================================================================
    // TEACHER SUBJECT
    // ================================================================
    //
    // A teacher must be assigned to:
    //      Teacher + Subject + Class
    //
    // ClassId is compulsory.
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        AssignTeachingSubjectAsync(
            Guid schoolId,
            Guid teacherId,
            AssignTeachingSubjectRequest request);

    Task<(bool Success, object? Data, string? Error)>
        RemoveTeachingSubjectAsync(
            Guid schoolId,
            Guid assignmentId);


    // ================================================================
    // CLASS TEACHER
    // ================================================================
    //
    // A ClassTeacher assignment means:
    //      Teacher + Class
    //
    // This is separate from TeacherSubject.
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        AssignClassTeacherAsync(
            Guid schoolId,
            Guid teacherId,
            Guid classId);

    Task<(bool Success, object? Data, string? Error)>
        RemoveClassTeacherAsync(
            Guid schoolId,
            Guid assignmentId);
}