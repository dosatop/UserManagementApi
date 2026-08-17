using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Classes;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class ClassService : IClassService
{
    private readonly ApplicationDbContext _context;

    public ClassService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        CreateClassAsync(
            Guid schoolId,
            CreateClassRequest request)
    {
        var school = await _context.Schools
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return (false, null, "School not found.");
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, null, "Class name is required.");
        }

        var exists = await _context.Classes
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Name.ToLower() == name.ToLower());

        if (exists)
        {
            return (
                false,
                null,
                "A class with this name already exists."
            );
        }

        var classroom = new Class
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name
        };

        _context.Classes.Add(classroom);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                classroom.Id,
                classroom.Name,
                classroom.SchoolId,
                SchoolName = school.Name
            },
            null
        );
    }

    public async Task<IEnumerable<object>>
        GetClassesAsync(Guid schoolId)
    {
        return await _context.Classes
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SchoolId
            })
            .ToListAsync();
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetClassAsync(
            Guid schoolId,
            Guid classId)
    {
        var classroom = await _context.Classes
            .AsNoTracking()
            .Where(x =>
                x.Id == classId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SchoolId
            })
            .FirstOrDefaultAsync();

        if (classroom == null)
        {
            return (false, null, "Class not found.");
        }

        return (true, classroom, null);
    }

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateClassAsync(
            Guid schoolId,
            Guid classId,
            CreateClassRequest request)
    {
        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == classId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (false, null, "Class not found.");
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, null, "Class name is required.");
        }

        var exists = await _context.Classes
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Id != classId &&
                x.Name.ToLower() == name.ToLower());

        if (exists)
        {
            return (
                false,
                null,
                "A class with this name already exists."
            );
        }

        classroom.Name = name;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                classroom.Id,
                classroom.Name,
                classroom.SchoolId
            },
            null
        );
    }

    public async Task<(bool Success, string? Error)>
        DeleteClassAsync(
            Guid schoolId,
            Guid classId)
    {
        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == classId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (false, "Class not found.");
        }

        var hasStudents = await _context.StudentProfiles
            .AnyAsync(x => x.ClassId == classId);

        if (hasStudents)
        {
            return (
                false,
                "This class cannot be deleted because it has students."
            );
        }

        _context.Classes.Remove(classroom);

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, object? Data, string? Error)>
    GetClassAssignmentsAsync(
        Guid schoolId,
        Guid classId,
        string session,
        string term)
{
    var classroom = await _context.Classes
        .AsNoTracking()
        .Where(x =>
            x.Id == classId &&
            x.SchoolId == schoolId)
        .Select(x => new
        {
            x.Id,
            x.Name,
            x.SchoolId
        })
        .FirstOrDefaultAsync();

    if (classroom == null)
    {
        return (
            false,
            null,
            "Class not found."
        );
    }

    var assignments = await _context.Assignments
        .AsNoTracking()
        .Where(x =>
            x.ClassId == classId &&
            x.SchoolId == schoolId &&
            x.Session == session &&
            x.Term == term)
        .OrderByDescending(x => x.AssignedAt)
        .Select(x => new
        {
            x.Id,
            x.Title,
            x.Description,
            x.AttachmentUrl,
            x.AssignedAt,
            x.DueDate,
            x.Session,
            x.Term,
            x.IsPublished,

            SubjectId = x.SubjectId,
            SubjectName = x.Subject.Name,

            TeacherId = x.TeacherId,
            TeacherName = x.Teacher.User.FullName
        })
        .ToListAsync();

    return (
        true,
        new
        {
            ClassId = classroom.Id,
            ClassName = classroom.Name,
            SchoolId = classroom.SchoolId,

            Session = session,
            Term = term,

            TotalAssignments = assignments.Count,

            Assignments = assignments
        },
        null
    );
}

public async Task<(bool Success, object? Data, string? Error)>
    GetAssignmentCountAsync(
        Guid schoolId,
        Guid classId,
        string session,
        string term)
{
    var classroom = await _context.Classes
        .AsNoTracking()
        .FirstOrDefaultAsync(x =>
            x.Id == classId &&
            x.SchoolId == schoolId);

    if (classroom == null)
    {
        return (
            false,
            null,
            "Class not found."
        );
    }

    var count = await _context.Assignments
        .AsNoTracking()
        .CountAsync(x =>
            x.ClassId == classId &&
            x.SchoolId == schoolId &&
            x.Session == session &&
            x.Term == term);

    return (
        true,
        new
        {
            ClassId = classroom.Id,
            ClassName = classroom.Name,
            Session = session,
            Term = term,
            TotalAssignments = count
        },
        null
    );
}

public async Task<(bool Success, object? Data, string? Error)>
    GetSchoolAssignmentCountAsync(
        Guid schoolId,
        string session,
        string term)
{
    var schoolExists = await _context.Schools
        .AnyAsync(x => x.Id == schoolId);

    if (!schoolExists)
    {
        return (
            false,
            null,
            "School not found."
        );
    }

    var assignments = await _context.Assignments
        .AsNoTracking()
        .Where(x =>
            x.SchoolId == schoolId &&
            x.Session == session &&
            x.Term == term)
        .OrderByDescending(x => x.AssignedAt)
        .Select(x => new
        {
            AssignmentId = x.Id,

            AssignmentTitle = x.Title,

            ClassId = x.ClassId,
            ClassName = x.Class.Name,

            SubjectId = x.SubjectId,
            SubjectName = x.Subject.Name,

            TeacherId = x.TeacherId,
            TeacherName = x.Teacher.User.FullName,

            x.AssignedAt,
            x.DueDate,

            x.IsPublished,

            x.Session,
            x.Term
        })
        .ToListAsync();

    return (
        true,
        new
        {
            SchoolId = schoolId,

            Session = session,
            Term = term,

            TotalAssignments = assignments.Count,

            Assignments = assignments
        },
        null
    );
}

public async Task<(bool Success, object? Data, string? Error)>
    GetClassStudentsAsync(
        Guid schoolId,
        Guid classId,
        Guid? subjectId)
{
    // Check class belongs to this school
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

    // If a subject was supplied, make sure it belongs
    // to the school and is assigned to this class.
    if (subjectId.HasValue)
    {
        var subjectExists = await _context.Subjects
            .AnyAsync(x =>
                x.Id == subjectId.Value &&
                x.SchoolId == schoolId);

        if (!subjectExists)
        {
            return (
                false,
                null,
                "Subject not found in this school."
            );
        }

        var subjectAssignedToClass =
            await _context.TeacherSubjects
                .AnyAsync(x =>
                    x.ClassId == classId &&
                    x.SubjectId == subjectId.Value);

        if (!subjectAssignedToClass)
        {
            return (
                false,
                null,
                "This subject is not assigned to this class."
            );
        }
    }

    // Get students in the class
    var students = await _context.StudentProfiles
        .Where(x =>
            x.SchoolId == schoolId &&
            x.ClassId == classId)
        .Select(x => new
        {
            StudentId = x.Id,
            x.StudentNumber,

            // Adjust these according to your StudentProfile model
            // FirstName = x.User.FirstName,
            // LastName = x.User.LastName,

            FullName = x.User.FullName,

            ClassId = classroom.Id,
            ClassName = classroom.Name,

            SubjectId = subjectId
        })
        .ToListAsync();

    return (
        true,
        new
        {
            ClassId = classroom.Id,
            ClassName = classroom.Name,
            SubjectId = subjectId,
            StudentCount = students.Count,
            Students = students
        },
        null
    );
}

}