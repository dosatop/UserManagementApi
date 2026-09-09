using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.AcademicTerms;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class AcademicTermService(
    ApplicationDbContext context) : IAcademicTermService
{
    private readonly ApplicationDbContext _context = context;

    // ================================================================
    // CREATE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateAsync(
            Guid schoolId,
            CreateAcademicTermRequest request)
    {
        if (request.AcademicSessionId == Guid.Empty)
        {
            return (
                false,
                null,
                "Academic session is required."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Term))
        {
            return (
                false,
                null,
                "Term is required."
            );
        }

        if (request.EndDate < request.StartDate)
        {
            return (
                false,
                null,
                "End date cannot be before start date."
            );
        }

        // ============================================================
        // CHECK SESSION
        // ============================================================

        var academicSession =
            await _context.AcademicSessions
                .FirstOrDefaultAsync(x =>
                    x.Id == request.AcademicSessionId &&
                    x.SchoolId == schoolId);

        if (academicSession == null)
        {
            return (
                false,
                null,
                "Academic session not found."
            );
        }

        // ============================================================
        // VALIDATE DATES
        // ============================================================

        if (request.StartDate < academicSession.StartDate ||
            request.EndDate > academicSession.EndDate)
        {
            return (
                false,
                null,
                "Term dates must be within the academic session dates."
            );
        }

        var termName = request.Term.Trim();

        // ============================================================
        // CHECK DUPLICATE
        // ============================================================

        var exists =
            await _context.AcademicTerms
                .AnyAsync(x =>
                    x.AcademicSessionId ==
                        request.AcademicSessionId &&
                    x.Term == termName);

        if (exists)
        {
            return (
                false,
                null,
                "This term already exists for the academic session."
            );
        }

        // ============================================================
        // CURRENT TERM
        // ============================================================

        if (request.IsCurrent)
        {
            var currentTerms =
                await _context.AcademicTerms
                    .Where(x =>
                        x.AcademicSessionId ==
                            request.AcademicSessionId &&
                        x.IsCurrent)
                    .ToListAsync();

            foreach (var currentTerm in currentTerms)
            {
                currentTerm.IsCurrent = false;
            }
        }

        // ============================================================
        // CREATE
        // ============================================================

        var academicTerm = new AcademicTerm
        {
            Id = Guid.NewGuid(),

            AcademicSessionId =
                request.AcademicSessionId,

            Term = termName,

            StartDate = request.StartDate,

            EndDate = request.EndDate,

            IsCurrent = request.IsCurrent,

            CreatedAt = DateTime.UtcNow
        };

        _context.AcademicTerms.Add(academicTerm);

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                id = academicTerm.Id,

                academicSessionId =
                    academicTerm.AcademicSessionId,

                term = academicTerm.Term,

                startDate = academicTerm.StartDate,

                endDate = academicTerm.EndDate,

                isCurrent = academicTerm.IsCurrent,

                createdAt = academicTerm.CreatedAt
            },
            null
        );
    }


    // ================================================================
    // GET ALL
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAllAsync(
            Guid schoolId,
            Guid academicSessionId)
    {
        var sessionExists =
            await _context.AcademicSessions
                .AnyAsync(x =>
                    x.Id == academicSessionId &&
                    x.SchoolId == schoolId);

        if (!sessionExists)
        {
            return (
                false,
                null,
                "Academic session not found."
            );
        }

        var terms =
            await _context.AcademicTerms
                .AsNoTracking()
                .Where(x =>
                    x.AcademicSessionId ==
                        academicSessionId)
                .OrderByDescending(x => x.IsCurrent)
                .ThenBy(x => x.StartDate)
                .Select(x => new
                {
                    id = x.Id,

                    academicSessionId =
                        x.AcademicSessionId,

                    term = x.Term,

                    startDate = x.StartDate,

                    endDate = x.EndDate,

                    isCurrent = x.IsCurrent,

                    createdAt = x.CreatedAt
                })
                .ToListAsync();

        return (
            true,
            terms,
            null
        );
    }


    // ================================================================
    // GET BY ID
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetByIdAsync(
            Guid schoolId,
            Guid termId)
    {
        var term =
            await _context.AcademicTerms
                .AsNoTracking()
                .Where(x =>
                    x.Id == termId &&
                    x.AcademicSession.SchoolId == schoolId)
                .Select(x => new
                {
                    id = x.Id,

                    academicSessionId =
                        x.AcademicSessionId,

                    term = x.Term,

                    startDate = x.StartDate,

                    endDate = x.EndDate,

                    isCurrent = x.IsCurrent,

                    createdAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();

        if (term == null)
        {
            return (
                false,
                null,
                "Academic term not found."
            );
        }

        return (
            true,
            term,
            null
        );
    }


    // ================================================================
    // UPDATE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateAsync(
            Guid schoolId,
            Guid termId,
            UpdateAcademicTermRequest request)
    {
        var academicTerm =
            await _context.AcademicTerms
                .Include(x => x.AcademicSession)
                .FirstOrDefaultAsync(x =>
                    x.Id == termId &&
                    x.AcademicSession.SchoolId == schoolId);

        if (academicTerm == null)
        {
            return (
                false,
                null,
                "Academic term not found."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Term))
        {
            return (
                false,
                null,
                "Term is required."
            );
        }

        if (request.EndDate < request.StartDate)
        {
            return (
                false,
                null,
                "End date cannot be before start date."
            );
        }

        // ============================================================
        // SESSION
        // ============================================================

        var academicSession =
            academicTerm.AcademicSession;

        // ============================================================
        // VALIDATE DATES
        // ============================================================

        if (request.StartDate < academicSession.StartDate ||
            request.EndDate > academicSession.EndDate)
        {
            return (
                false,
                null,
                "Term dates must be within the academic session dates."
            );
        }

        var termName = request.Term.Trim();

        // ============================================================
        // CHECK DUPLICATE
        // ============================================================

        var duplicate =
            await _context.AcademicTerms
                .AnyAsync(x =>
                    x.Id != termId &&
                    x.AcademicSessionId ==
                        academicTerm.AcademicSessionId &&
                    x.Term == termName);

        if (duplicate)
        {
            return (
                false,
                null,
                "This term already exists for the academic session."
            );
        }

        // ============================================================
        // CURRENT TERM
        // ============================================================

        if (request.IsCurrent)
        {
            var currentTerms =
                await _context.AcademicTerms
                    .Where(x =>
                        x.AcademicSessionId ==
                            academicTerm.AcademicSessionId &&
                        x.Id != termId &&
                        x.IsCurrent)
                    .ToListAsync();

            foreach (var currentTerm in currentTerms)
            {
                currentTerm.IsCurrent = false;
            }
        }

        // ============================================================
        // UPDATE
        // ============================================================

        academicTerm.Term = termName;

        academicTerm.StartDate =
            request.StartDate;

        academicTerm.EndDate =
            request.EndDate;

        academicTerm.IsCurrent =
            request.IsCurrent;

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                id = academicTerm.Id,

                academicSessionId =
                    academicTerm.AcademicSessionId,

                term = academicTerm.Term,

                startDate = academicTerm.StartDate,

                endDate = academicTerm.EndDate,

                isCurrent = academicTerm.IsCurrent,

                createdAt = academicTerm.CreatedAt
            },
            null
        );
    }


    // ================================================================
    // DELETE
    // ================================================================

    public async Task<(bool Success, string? Error)>
        DeleteAsync(
            Guid schoolId,
            Guid termId)
    {
        var academicTerm =
            await _context.AcademicTerms
                .Include(x => x.AcademicSession)
                .FirstOrDefaultAsync(x =>
                    x.Id == termId &&
                    x.AcademicSession.SchoolId == schoolId);

        if (academicTerm == null)
        {
            return (
                false,
                "Academic term not found."
            );
        }

        _context.AcademicTerms.Remove(academicTerm);

        await _context.SaveChangesAsync();

        return (
            true,
            null
        );
    }
}