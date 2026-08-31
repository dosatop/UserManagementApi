namespace UserManagementApi.Models.Assignments;

public class ClassSubject
{
    public Guid Id { get; set; }

    // ============================================================
    // CLASS
    // ============================================================

    public Guid ClassId { get; set; }

    public Class Class { get; set; } = null!;


    // ============================================================
    // SUBJECT
    // ============================================================

    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;


    // ============================================================
    // OPTIONAL: SCHOOL
    // ============================================================

    public Guid SchoolId { get; set; }
}
