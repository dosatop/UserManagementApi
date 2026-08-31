using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Classes;
using UserManagementApi.Models;
using UserManagementApi.Models.SchoolModels;
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
            Name = request.Name,
            Level = request.Level
        };

        _context.Classes.Add(classroom);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                classroom.Id,
                classroom.Name,
                classroom.Level,
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
                x.SchoolId,

                Level = x.Level.ToString(),
                LevelId = (int)x.Level
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
                // ========================================================
                // CLASS
                // ========================================================

                ClassId = x.Id,
                ClassName = x.Name,
                SchoolId = x.SchoolId,

                Level = x.Level,
                LevelName = x.Level == SchoolLevel.Junior
                ? "Junior"
                : "Senior",
                
                // ========================================================
                // CLASS TEACHER
                // ========================================================

                ClassTeacherId = x.TeacherClasses
                    .Select(tc => (Guid?)tc.TeacherId)
                    .FirstOrDefault(),

                ClassTeacherName = x.TeacherClasses
                    .Select(tc => tc.Teacher.User.FullName)
                    .FirstOrDefault(),

                // ========================================================
                // SUBJECTS
                // ========================================================

                Subjects = x.ClassSubjects
                    .Select(cs => new
                    {
                        SubjectId = cs.SubjectId,
                        SubjectName = cs.Subject.Name,
                        SubjectCode = cs.Subject.Code
                    })
                    .ToList(),


                // ========================================================
                // TEACHERS
                // ========================================================

                Teachers = x.TeacherSubjects
                    .Select(ts => new
                    {
                        TeacherId = ts.TeacherId,

                        TeacherName = ts.Teacher.User.FullName,
                        Email = ts.Teacher.User.Email,
                        PhoneNumber = ts.Teacher.User.PhoneNumber,

                        SubjectId = ts.SubjectId,
                        SubjectName = ts.Subject.Name,
                        SubjectCode = ts.Subject.Code,

                        ClassId = ts.ClassId,
                        ClassName = ts.Class.Name
                    })
                    .ToList(),

                // ========================================================
                // STUDENTS
                // ========================================================

                Students = x.Students
                    .Select(s => new
                    {
                        StudentId = s.Id,
                        StudentNumber = s.StudentNumber,
                        StudentName = s.User.FullName,
                        Email = s.User.Email,
                        PhoneNumber = s.User.PhoneNumber,

                        // =================================================
                        // PARENTS
                        // =================================================

                        ParentCount = s.Parents.Count(),

                        // =================================================
                        // RESULTS
                        // =================================================

                        Results = _context.StudentResults
                            .Where(r =>
                                r.StudentId == s.Id &&
                                r.ClassId == x.Id &&
                                r.SchoolId == schoolId)
                            .Select(r => new
                            {
                                ResultId = r.Id,

                                SubjectId = r.SubjectId,
                                SubjectName = r.Subject.Name,
                                SubjectCode = r.Subject.Code,

                                Session = r.Session,
                                Term = r.Term,

                                TestScore = r.TestScore,
                                ExamScore = r.ExamScore,
                                Score = r.Score,

                                Grade = r.Grade,
                                Remark = r.Remark,

                                CreatedAt = r.CreatedAt
                            })
                            .ToList(),

                        // =================================================
                        // ATTENDANCE
                        // =================================================

                        Attendance = _context.AttendanceRecords
                            .Where(a =>
                                a.StudentId == s.Id &&
                                a.ClassId == x.Id &&
                                a.SchoolId == schoolId)
                            .Select(a => new
                            {
                                AttendanceId = a.Id,

                                SubjectId = a.SubjectId,
                                SubjectName = a.Subject.Name,
                                SubjectCode = a.Subject.Code,

                                TeacherId = a.TeacherId,
                                TeacherName = a.Teacher.User.FullName,

                                AttendanceDate = a.AttendanceDate,

                                Status = a.Status,

                                Session = a.Session,
                                Term = a.Term
                            })
                            .ToList()
                    })
                    .ToList()
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

        return (
            true,
            classroom,
            null
        );
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
            return (
                false,
                null,
                "Class name is required."
            );
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

        // ============================================================
        // UPDATE
        // ============================================================

        classroom.Name = name;
        classroom.Level = request.Level;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                ClassId = classroom.Id,
                ClassName = classroom.Name,
                Level = classroom.Level,
                SchoolId = classroom.SchoolId
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
        // ============================================================
        // CHECK CLASS
        // ============================================================

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
                "Class not found in this school."
            );
        }


        // ============================================================
        // CHECK SUBJECT
        // ============================================================

        if (subjectId.HasValue)
        {
            // Make sure subject belongs to this school
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


            // ========================================================
            // IMPORTANT:
            // Check ClassSubject, NOT TeacherSubject
            // ========================================================

            var classSubjectExists = await _context.ClassSubjects
                .AnyAsync(x =>
                    x.ClassId == classId &&
                    x.SubjectId == subjectId.Value &&
                    x.SchoolId == schoolId);

            if (!classSubjectExists)
            {
                return (
                    false,
                    null,
                    "This subject has not been assigned to this class."
                );
            }
        }


        // ============================================================
        // GET STUDENTS
        // ============================================================

        var students = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == classId)
            .Select(x => new
            {
                StudentId = x.Id,
                StudentNumber = x.StudentNumber,

                FullName = x.User.FullName,

                ClassId = x.ClassId,
                ClassName = x.Class.Name,

                SchoolLevel = x.Class.Level.ToString(),
                SchoolLevelId = (int)x.Class.Level,

                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null
                    ? x.Department.Name
                    : null,

                TradeId = x.TradeId,
                TradeName = x.Trade != null
                    ? x.Trade.Name
                    : null,

                SubjectId = subjectId
            })
            .OrderBy(x => x.FullName)
            .ToListAsync();


        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                ClassId = classroom.Id,
                ClassName = classroom.Name,

                SchoolLevel = classroom.Level.ToString(),
                SchoolLevelId = (int)classroom.Level,

                SubjectId = subjectId,

                StudentCount = students.Count,

                Students = students
            },
            null
        );
    }

}