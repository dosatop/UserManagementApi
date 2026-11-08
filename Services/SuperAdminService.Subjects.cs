using Microsoft.EntityFrameworkCore;
using UserManagementApi.DTOs.SuperAdmin;
using UserManagementApi.DTOs.Subjects;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

// ====================================================================
// SUPER ADMIN SERVICE — SUBJECTS
// ====================================================================
// Create / update / assign reuse SubjectService (same validation the
// school admin gets). The list is richer: each subject comes with the
// classes that take it and who teaches it in each class.

public partial class SuperAdminService
{
    // ================================================================
    // LIST
    // ================================================================

    public async Task<SuperAdminResult> GetSchoolSubjectsAsync(Guid schoolId)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var subjects = await _context.Subjects
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                subjectId = s.Id,
                subjectName = s.Name,
                code = s.Code,
                type = (int)s.Type,                 // 1 General, 2 Department, 3 Trade
                typeName = s.Type.ToString(),
                departmentId = s.DepartmentId,
                departmentName = s.Department != null ? s.Department.Name : null,
                tradeId = s.TradeId,
                tradeName = s.Trade != null ? s.Trade.Name : null
            })
            .ToListAsync();

        var classNames = await ClassNames(schoolId);

        var classLinks = await _context.ClassSubjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new { x.SubjectId, x.ClassId })
            .ToListAsync();

        var teacherNames = await _context.Teachers
            .AsNoTracking()
            .Where(t => t.SchoolId == schoolId)
            .Select(t => new { t.Id, name = t.User.FullName })
            .ToDictionaryAsync(t => t.Id, t => t.name);

        var teacherLinks = await _context.TeacherSubjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new { x.SubjectId, x.ClassId, x.TeacherId })
            .ToListAsync();

        var result = subjects.Select(s =>
        {
            var classes = classLinks
                .Where(l => l.SubjectId == s.subjectId)
                .Select(l => l.ClassId)
                .Distinct()
                .Select(id => new
                {
                    classId = id,
                    className = classNames.GetValueOrDefault(id, "Class")
                })
                .OrderBy(c => c.className)
                .ToList();

            var teachers = teacherLinks
                .Where(l => l.SubjectId == s.subjectId)
                .Select(l => new
                {
                    teacherId = l.TeacherId,
                    teacherName = teacherNames.GetValueOrDefault(l.TeacherId, "Teacher"),
                    classId = l.ClassId,
                    className = classNames.GetValueOrDefault(l.ClassId, "Class")
                })
                .Distinct()
                .OrderBy(t => t.className)
                .ToList();

            return new
            {
                s.subjectId,
                s.subjectName,
                s.code,
                s.type,
                s.typeName,
                s.departmentId,
                s.departmentName,
                s.tradeId,
                s.tradeName,
                classCount = classes.Count,
                teacherCount = teachers.Select(t => t.teacherId).Distinct().Count(),
                classes,
                teachers
            };
        }).ToList();

        return SuperAdminResult.Ok(new { subjectCount = result.Count, subjects = result });
    }

    // ================================================================
    // CREATE / UPDATE (SubjectService does the validation)
    // ================================================================

    public async Task<SuperAdminResult> CreateSchoolSubjectAsync(
        Guid schoolId,
        CreateSubjectRequest request)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var (success, data, error) =
            await _subjectService.CreateSubjectAsync(schoolId, request);

        return success
            ? SuperAdminResult.Ok(new { message = "Subject created successfully.", subject = data })
            : SuperAdminResult.Fail(error ?? "Could not create the subject.");
    }

    public async Task<SuperAdminResult> UpdateSchoolSubjectAsync(
        Guid schoolId,
        Guid subjectId,
        CreateSubjectRequest request)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var (success, data, error) =
            await _subjectService.UpdateSubjectAsync(schoolId, subjectId, request);

        if (success)
            return SuperAdminResult.Ok(new { message = "Subject updated successfully.", subject = data });

        return error == "Subject not found."
            ? SuperAdminResult.Missing(error)
            : SuperAdminResult.Fail(error ?? "Could not update the subject.");
    }

    // ================================================================
    // DELETE
    // ================================================================
    // Refused while the subject has teachers, results, assignments or
    // attendance — so no one's work or scores are lost. Its class
    // links are removed automatically.

    public async Task<SuperAdminResult> DeleteSchoolSubjectAsync(
        Guid schoolId,
        Guid subjectId)
    {
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == subjectId && s.SchoolId == schoolId);

        if (subject == null)
            return SuperAdminResult.Missing("Subject not found in this school.");

        if (await _context.TeacherSubjects.AnyAsync(x => x.SubjectId == subjectId))
            return SuperAdminResult.Fail(
                $"Teachers are still assigned to {subject.Name}. Remove it from " +
                "its classes (or unassign the teachers) first.");

        // ← Rename StudentResults / SubjectId if yours differ.
        if (await _context.StudentResults.AnyAsync(x => x.SubjectId == subjectId))
            return SuperAdminResult.Fail(
                $"{subject.Name} already has student results, so it can't be deleted.");

        if (await _context.Assignments.AnyAsync(x => x.SubjectId == subjectId))
            return SuperAdminResult.Fail(
                $"{subject.Name} has assignments, so it can't be deleted.");

        if (await _context.AttendanceRecords.AnyAsync(x => x.SubjectId == subjectId))
            return SuperAdminResult.Fail(
                $"{subject.Name} has attendance records, so it can't be deleted.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.ClassSubjects
                .Where(x => x.SubjectId == subjectId)
                .ExecuteDeleteAsync();

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return SuperAdminResult.Ok(new { message = "Subject deleted successfully." });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return SuperAdminResult.Fail(
                $"{subject.Name} still has records linked to it, so it can't be deleted.");
        }
    }

    // ================================================================
    // ASSIGN TO CLASSES (+ optional teacher)
    // ================================================================

    public async Task<SuperAdminResult> AssignSchoolSubjectAsync(
        Guid schoolId,
        Guid subjectId,
        SuperAdminAssignSubjectRequest request)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var (success, data, error) =
            await _subjectService.AssignSubjectToMultipleClassesAsync(
                schoolId,
                subjectId,
                request.ClassIds,
                request.TeacherId);

        return success
            ? SuperAdminResult.Ok(data!)
            : SuperAdminResult.Fail(error ?? "Could not assign the subject.");
    }

    // ================================================================
    // REMOVE FROM ONE CLASS
    // ================================================================
    // Removes the class link and that class's teacher assignments for
    // this subject. Results already recorded are kept.

    public async Task<SuperAdminResult> RemoveSubjectFromClassAsync(
        Guid schoolId,
        Guid subjectId,
        Guid classId)
    {
        var subjectExists = await _context.Subjects
            .AnyAsync(s => s.Id == subjectId && s.SchoolId == schoolId);

        if (!subjectExists)
            return SuperAdminResult.Missing("Subject not found in this school.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var teachersRemoved = await _context.TeacherSubjects
            .Where(x => x.SubjectId == subjectId && x.ClassId == classId)
            .ExecuteDeleteAsync();

        var linksRemoved = await _context.ClassSubjects
            .Where(x => x.SubjectId == subjectId && x.ClassId == classId)
            .ExecuteDeleteAsync();

        await transaction.CommitAsync();

        if (linksRemoved == 0 && teachersRemoved == 0)
            return SuperAdminResult.Fail("This class doesn't take this subject.");

        return SuperAdminResult.Ok(new
        {
            message = "Subject removed from the class.",
            teachersRemoved
        });
    }
}
