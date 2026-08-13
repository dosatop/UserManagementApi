public interface IStudentService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateStudentAsync(
            Guid schoolId,
            CreateStudentRequest request);

    Task<IEnumerable<object>> GetStudentsAsync(
        Guid schoolId);
}