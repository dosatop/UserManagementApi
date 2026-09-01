using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Subjects;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class SubjectService : ISubjectService
{
    private readonly ApplicationDbContext _context;

    public SubjectService(ApplicationDbContext context)
    {
        _context = context;
    }

   public async Task<(bool Success, object? Data, string? Error)>
    CreateSubjectAsync(
        Guid schoolId,
        CreateSubjectRequest request)
{
    var school = await _context.Schools
        .FirstOrDefaultAsync(x => x.Id == schoolId);

    if (school == null)
    {
        return (
            false,
            null,
            "School not found."
        );
    }

    var name = request.Name.Trim();
    var code = request.Code?.Trim();

    if (string.IsNullOrWhiteSpace(name))
    {
        return (
            false,
            null,
            "Subject name is required."
        );
    }

    // ================================================================
    // TYPE VALIDATION
    // ================================================================

    if (request.Type == SubjectType.Department &&
        request.DepartmentId == null)
    {
        return (
            false,
            null,
            "Department is required for a department subject."
        );
    }

    if (request.Type == SubjectType.Trade &&
        request.TradeId == null)
    {
        return (
            false,
            null,
            "Trade is required for a trade subject."
        );
    }

    if (request.Type == SubjectType.General &&
        (request.DepartmentId != null || request.TradeId != null))
    {
        return (
            false,
            null,
            "General subjects cannot belong to a department or trade."
        );
    }

    // ================================================================
    // DEPARTMENT
    // ================================================================

    Department? department = null;

    if (request.DepartmentId.HasValue)
    {
        department = await _context.Departments
            .FirstOrDefaultAsync(x =>
                x.Id == request.DepartmentId.Value &&
                x.SchoolId == schoolId);

        if (department == null)
        {
            return (
                false,
                null,
                "Department not found in this school."
            );
        }
    }

    // ================================================================
    // TRADE
    // ================================================================

    Trade? trade = null;

    if (request.TradeId.HasValue)
    {
        trade = await _context.Trades
            .FirstOrDefaultAsync(x =>
                x.Id == request.TradeId.Value &&
                x.SchoolId == schoolId);

        if (trade == null)
        {
            return (
                false,
                null,
                "Trade not found in this school."
            );
        }
    }

    // ================================================================
    // DUPLICATE SUBJECT NAME
    // ================================================================

    var exists = await _context.Subjects
        .AnyAsync(x =>
            x.SchoolId == schoolId &&
            x.Name.ToLower() == name.ToLower());

    if (exists)
    {
        return (
            false,
            null,
            "A subject with this name already exists."
        );
    }

    // ================================================================
    // DUPLICATE SUBJECT CODE
    // ================================================================

    if (!string.IsNullOrWhiteSpace(code))
    {
        var codeExists = await _context.Subjects
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Code != null &&
                x.Code.ToLower() == code.ToLower());

        if (codeExists)
        {
            return (
                false,
                null,
                "A subject with this code already exists."
            );
        }
    }

    // ================================================================
    // CREATE SUBJECT
    // ================================================================

    var subject = new Subject
    {
        Id = Guid.NewGuid(),
        SchoolId = schoolId,
        Name = name,
        Code = code,

        Type = request.Type,

        DepartmentId = request.DepartmentId,
        TradeId = request.TradeId
    };

    _context.Subjects.Add(subject);

    await _context.SaveChangesAsync();

    // ================================================================
    // RESPONSE
    // ================================================================

    return (
        true,
        new
        {
            subject.Id,
            subject.Name,
            subject.Code,
            subject.SchoolId,

            SchoolName = school.Name,

            Type = subject.Type,

            DepartmentId = subject.DepartmentId,
            DepartmentName = department?.Name,

            TradeId = subject.TradeId,
            TradeName = trade?.Name
        },
        null
    );
}


    public async Task<IEnumerable<object>>
        GetSubjectsAsync(Guid schoolId)
    {
        return await _context.Subjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.SchoolId,
                type = x.Type,
                departmentId = x.DepartmentId,
                departmentName = x.Department != null
        ? x.Department.Name
        : null,

                tradeId = x.TradeId,
                tradeName = x.Trade != null
        ? x.Trade.Name
        : null
            })
            .ToListAsync();
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetSubjectAsync(
            Guid schoolId,
            Guid subjectId)
    {
        var subject = await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == subjectId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.SchoolId,
                type = x.Type,
                departmentId = x.DepartmentId,
                departmentName = x.Department != null
        ? x.Department.Name
        : null,

                tradeId = x.TradeId,
                tradeName = x.Trade != null
        ? x.Trade.Name
        : null
            })
            .FirstOrDefaultAsync();

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject not found."
            );
        }

        return (
            true,
            subject,
            null
        );
    }

 public async Task<(bool Success, object? Data, string? Error)>
    UpdateSubjectAsync(
        Guid schoolId,
        Guid subjectId,
        CreateSubjectRequest request)
{
    var subject = await _context.Subjects
        .FirstOrDefaultAsync(x =>
            x.Id == subjectId &&
            x.SchoolId == schoolId);

    if (subject == null)
    {
        return (
            false,
            null,
            "Subject not found."
        );
    }

    var name = request.Name.Trim();
    var code = request.Code?.Trim();

    // ============================================================
    // BASIC VALIDATION
    // ============================================================

    if (string.IsNullOrWhiteSpace(name))
    {
        return (
            false,
            null,
            "Subject name is required."
        );
    }

    // ============================================================
    // TYPE VALIDATION
    // ============================================================

    if (request.Type == SubjectType.Department &&
        request.DepartmentId == null)
    {
        return (
            false,
            null,
            "Department is required for a department subject."
        );
    }

    if (request.Type == SubjectType.Trade &&
        request.TradeId == null)
    {
        return (
            false,
            null,
            "Trade is required for a trade subject."
        );
    }

    if (request.Type == SubjectType.General &&
        (request.DepartmentId != null ||
         request.TradeId != null))
    {
        return (
            false,
            null,
            "General subjects cannot belong to a department or trade."
        );
    }

    // ============================================================
    // DEPARTMENT VALIDATION
    // ============================================================

    Department? department = null;

    if (request.DepartmentId.HasValue)
    {
        department = await _context.Departments
            .FirstOrDefaultAsync(x =>
                x.Id == request.DepartmentId.Value &&
                x.SchoolId == schoolId);

        if (department == null)
        {
            return (
                false,
                null,
                "Department not found in this school."
            );
        }
    }

    // ============================================================
    // TRADE VALIDATION
    // ============================================================

    Trade? trade = null;

    if (request.TradeId.HasValue)
    {
        trade = await _context.Trades
            .FirstOrDefaultAsync(x =>
                x.Id == request.TradeId.Value &&
                x.SchoolId == schoolId);

        if (trade == null)
        {
            return (
                false,
                null,
                "Trade not found in this school."
            );
        }
    }

    // ============================================================
    // DUPLICATE NAME
    // ============================================================

    var nameExists = await _context.Subjects
        .AnyAsync(x =>
            x.SchoolId == schoolId &&
            x.Id != subjectId &&
            x.Name.ToLower() == name.ToLower());

    if (nameExists)
    {
        return (
            false,
            null,
            "A subject with this name already exists."
        );
    }

    // ============================================================
    // DUPLICATE CODE
    // ============================================================

    if (!string.IsNullOrWhiteSpace(code))
    {
        var codeExists = await _context.Subjects
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Id != subjectId &&
                x.Code != null &&
                x.Code.ToLower() == code.ToLower());

        if (codeExists)
        {
            return (
                false,
                null,
                "A subject with this code already exists."
            );
        }
    }

    // ============================================================
    // UPDATE
    // ============================================================

    subject.Name = name;
    subject.Code = code;

    subject.Type = request.Type;

    subject.DepartmentId = request.DepartmentId;
    subject.TradeId = request.TradeId;

    await _context.SaveChangesAsync();

    // ============================================================
    // RESPONSE
    // ============================================================

    var updatedSubject = await _context.Subjects
        .AsNoTracking()
        .Where(x =>
            x.Id == subjectId &&
            x.SchoolId == schoolId)
        .Select(x => new
        {
            x.Id,
            x.Name,
            x.Code,
            x.SchoolId,

            type = x.Type,

            departmentId = x.DepartmentId,

            departmentName = x.Department != null
                ? x.Department.Name
                : null,

            tradeId = x.TradeId,

            tradeName = x.Trade != null
                ? x.Trade.Name
                : null
        })
        .FirstAsync();

    return (
        true,
        updatedSubject,
        null
    );
}


    public async Task<(bool Success, string? Error)>
        DeleteSubjectAsync(
            Guid schoolId,
            Guid subjectId)
    {
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == subjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                "Subject not found."
            );
        }

        var hasTeacherAssignments =
            await _context.TeacherSubjects
                .AnyAsync(x => x.SubjectId == subjectId);

        if (hasTeacherAssignments)
        {
            return (
                false,
                "This subject cannot be deleted because teachers are assigned to it."
            );
        }

        _context.Subjects.Remove(subject);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}