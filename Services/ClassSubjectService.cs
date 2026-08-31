using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Models.Assignments;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class ClassSubjectService : IClassSubjectService
{
    private readonly ApplicationDbContext _context;

    public ClassSubjectService(ApplicationDbContext context)
    {
        _context = context;
    }

// ============================================================
// ASSIGN SUBJECT TO CLASS
// ============================================================

public async Task<(bool Success, object? Data, string? Error)>
    AssignSubjectToClassAsync(
        Guid schoolId,
        Guid classId,
        Guid subjectId)
{
    // --------------------------------------------------------
    // CLASS
    // --------------------------------------------------------

    var classroom = await _context.Classes
        .FirstOrDefaultAsync(x =>
            x.Id == classId &&
            x.SchoolId == schoolId);

    if (classroom == null)
    {
        return (
            false,
            null,
            "Class not found in this school."
        );
    }

    // --------------------------------------------------------
    // SUBJECT
    // --------------------------------------------------------

    var subject = await _context.Subjects
        .FirstOrDefaultAsync(x =>
            x.Id == subjectId &&
            x.SchoolId == schoolId);

    if (subject == null)
    {
        return (
            false,
            null,
            "Subject not found in this school."
        );
    }

    // --------------------------------------------------------
    // TRADE SUBJECTS CANNOT BE ASSIGNED TO A CLASS
    // --------------------------------------------------------

    if (subject.TradeId.HasValue)
    {
        return (
            false,
            null,
            "Trade subjects cannot be assigned directly to a class. " +
            "Students must first choose a trade."
        );
    }

    // --------------------------------------------------------
    // CHECK DUPLICATE
    // --------------------------------------------------------

    var exists = await _context.ClassSubjects
        .AnyAsync(x =>
            x.ClassId == classId &&
            x.SubjectId == subjectId);

    if (exists)
    {
        return (
            false,
            null,
            "This subject is already assigned to this class."
        );
    }

    // --------------------------------------------------------
    // CREATE
    // --------------------------------------------------------

    var classSubject = new ClassSubject
    {
        Id = Guid.NewGuid(),
        SchoolId = schoolId,
        ClassId = classId,
        SubjectId = subjectId
    };

    _context.ClassSubjects.Add(classSubject);

    await _context.SaveChangesAsync();

    // --------------------------------------------------------
    // RESPONSE
    // --------------------------------------------------------

    return (
        true,
        new
        {
            classSubjectId = classSubject.Id,

            schoolId,

            classId = classroom.Id,
            className = classroom.Name,

            subjectId = subject.Id,
            subjectName = subject.Name,
            subjectCode = subject.Code,

            message =
                "Subject assigned to class successfully."
        },
        null
    );
}

    // ============================================================
    // GET SUBJECTS FOR CLASS
    // ============================================================

    public async Task<IEnumerable<object>>
        GetClassSubjectsAsync(
            Guid schoolId,
            Guid classId)
    {
        return await _context.ClassSubjects
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == classId)
            .OrderBy(x => x.Subject.Name)
            .Select(x => new
            {
                classSubjectId = x.Id,

                subjectId = x.SubjectId,
                subjectName = x.Subject.Name,
                subjectCode = x.Subject.Code,

                classId = x.ClassId,
                className = x.Class.Name
            })
            .ToListAsync();
    }


    // ============================================================
    // REMOVE SUBJECT FROM CLASS
    // ============================================================

    public async Task<(bool Success, string? Error)>
        RemoveSubjectFromClassAsync(
            Guid schoolId,
            Guid classId,
            Guid subjectId)
    {
        var classSubject = await _context.ClassSubjects
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
                x.ClassId == classId &&
                x.SubjectId == subjectId);

        if (classSubject == null)
        {
            return (
                false,
                "This subject is not assigned to this class."
            );
        }

        _context.ClassSubjects.Remove(classSubject);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}
