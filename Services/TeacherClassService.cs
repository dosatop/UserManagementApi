using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class TeacherClassService : ITeacherClassService
{
    private readonly ApplicationDbContext _context;

    public TeacherClassService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        AssignClassAsync(
            Guid schoolId,
            Guid teacherId,
            AssignTeacherClassRequest request)
    {
        // Check teacher
        var teacher = await _context.Teachers
     .Include(x => x.User)
     .FirstOrDefaultAsync(x =>
         x.Id == teacherId &&
         x.SchoolId == schoolId);

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher not found in this school."
            );
        }

        // Check class
        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class not found in this school."
            );
        }

        // Check existing assignment
        var alreadyAssigned =
            await _context.TeacherClasses.AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.ClassId == request.ClassId);

        if (alreadyAssigned)
        {
            return (
                false,
                null,
                "This teacher is already assigned to this class."
            );
        }

        // Create assignment
        var teacherClass = new TeacherClass
        {
            TeacherId = teacherId,
            ClassId = request.ClassId
        };

        _context.TeacherClasses.Add(teacherClass);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                TeacherId = teacher.Id,
                TeacherName = teacher.User.FullName,
                ClassId = classroom.Id,
                ClassName = classroom.Name,
                SchoolId = schoolId
            },
            null
        );
    }

    public async Task<IEnumerable<object>> GetTeacherClassesAsync(
        Guid schoolId,
        Guid teacherId)
    {
        return await _context.TeacherClasses
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacherId &&
                x.Teacher.SchoolId == schoolId)
            .Select(x => new
            {
                x.ClassId,
                ClassName = x.Class.Name
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)>
        RemoveClassAsync(
            Guid schoolId,
            Guid teacherId,
            Guid classId)
    {
        var teacherClass =
            await _context.TeacherClasses
                .FirstOrDefaultAsync(x =>
                    x.TeacherId == teacherId &&
                    x.ClassId == classId &&
                    x.Teacher.SchoolId == schoolId);

        if (teacherClass == null)
        {
            return (
                false,
                "Teacher is not assigned to this class."
            );
        }

        _context.TeacherClasses.Remove(teacherClass);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}