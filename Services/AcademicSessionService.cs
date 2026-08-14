using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.AcademicSessions;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class AcademicSessionService(
    ApplicationDbContext context) : IAcademicSessionService
{
    private readonly ApplicationDbContext _context = context;

    // ================================================================
    // CREATE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateAsync(
            Guid schoolId,
            CreateAcademicSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Session))
        {
            return (
                false,
                null,
                "Session is required."
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

        var session = request.Session.Trim();
        var term = request.Term.Trim();

        // ============================================================
        // CHECK DUPLICATE
        // ============================================================

        var exists = await _context.AcademicSessions
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Session == session &&
                x.Term == term);

        if (exists)
        {
            return (
                false,
                null,
                "This academic session and term already exists."
            );
        }

        // ============================================================
        // CREATE
        // ============================================================

        var academicSession = new AcademicSession
        {
            Id = Guid.NewGuid(),

            SchoolId = schoolId,

            Session = session,
            Term = term,

            IsCurrent = false,

            CreatedAt = DateTime.UtcNow
        };

        _context.AcademicSessions.Add(academicSession);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                id = academicSession.Id,

                schoolId = academicSession.SchoolId,

                session = academicSession.Session,
                term = academicSession.Term,

                isCurrent = academicSession.IsCurrent,

                createdAt = academicSession.CreatedAt
            },
            null
        );
    }

    // ================================================================
    // GET ALL
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAllAsync(
            Guid schoolId)
    {
        var sessions = await _context.AcademicSessions
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id,

                schoolId = x.SchoolId,

                session = x.Session,
                term = x.Term,

                isCurrent = x.IsCurrent,

                createdAt = x.CreatedAt
            })
            .ToListAsync();

        return (
            true,
            sessions,
            null
        );
    }

    // ================================================================
    // GET CURRENT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetCurrentAsync(
            Guid schoolId)
    {
        var current = await _context.AcademicSessions
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent)
            .Select(x => new
            {
                id = x.Id,

                schoolId = x.SchoolId,

                session = x.Session,
                term = x.Term,

                isCurrent = x.IsCurrent,

                createdAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (current == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        return (
            true,
            current,
            null
        );
    }

    // ================================================================
    // GET BY ID
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetByIdAsync(
            Guid schoolId,
            Guid sessionId)
    {
        var academicSession =
            await _context.AcademicSessions
                .AsNoTracking()
                .Where(x =>
                    x.Id == sessionId &&
                    x.SchoolId == schoolId)
                .Select(x => new
                {
                    id = x.Id,

                    schoolId = x.SchoolId,

                    session = x.Session,
                    term = x.Term,

                    isCurrent = x.IsCurrent,

                    createdAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();

        if (academicSession == null)
        {
            return (
                false,
                null,
                "Academic session not found."
            );
        }

        return (
            true,
            academicSession,
            null
        );
    }

    // ================================================================
    // UPDATE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateAsync(
            Guid schoolId,
            Guid sessionId,
            UpdateAcademicSessionRequest request)
    {
        var academicSession =
            await _context.AcademicSessions
                .FirstOrDefaultAsync(x =>
                    x.Id == sessionId &&
                    x.SchoolId == schoolId);

        if (academicSession == null)
        {
            return (
                false,
                null,
                "Academic session not found."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Session))
        {
            return (
                false,
                null,
                "Session is required."
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

        var session = request.Session.Trim();
        var term = request.Term.Trim();

        // ============================================================
        // DUPLICATE
        // ============================================================

        var duplicate = await _context.AcademicSessions
            .AnyAsync(x =>
                x.Id != sessionId &&
                x.SchoolId == schoolId &&
                x.Session == session &&
                x.Term == term);

        if (duplicate)
        {
            return (
                false,
                null,
                "This academic session and term already exists."
            );
        }

        academicSession.Session = session;
        academicSession.Term = term;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                id = academicSession.Id,

                schoolId = academicSession.SchoolId,

                session = academicSession.Session,
                term = academicSession.Term,

                isCurrent = academicSession.IsCurrent,

                createdAt = academicSession.CreatedAt
            },
            null
        );
    }

    // ================================================================
    // ACTIVATE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        ActivateAsync(
            Guid schoolId,
            Guid sessionId)
    {
        var sessions = await _context.AcademicSessions
            .Where(x => x.SchoolId == schoolId)
            .ToListAsync();

        var selectedSession = sessions
            .FirstOrDefault(x => x.Id == sessionId);

        if (selectedSession == null)
        {
            return (
                false,
                null,
                "Academic session not found."
            );
        }

        // ============================================================
        // DEACTIVATE EVERYTHING ELSE
        // ============================================================

        foreach (var session in sessions)
        {
            session.IsCurrent = false;
        }

        // ============================================================
        // ACTIVATE SELECTED SESSION
        // ============================================================

        selectedSession.IsCurrent = true;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                id = selectedSession.Id,

                schoolId = selectedSession.SchoolId,

                session = selectedSession.Session,
                term = selectedSession.Term,

                isCurrent = selectedSession.IsCurrent,

                createdAt = selectedSession.CreatedAt
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
            Guid sessionId)
    {
        var academicSession =
            await _context.AcademicSessions
                .FirstOrDefaultAsync(x =>
                    x.Id == sessionId &&
                    x.SchoolId == schoolId);

        if (academicSession == null)
        {
            return (
                false,
                "Academic session not found."
            );
        }

        // Don't allow deleting the active period
        if (academicSession.IsCurrent)
        {
            return (
                false,
                "The current academic session cannot be deleted. Activate another session first."
            );
        }

        _context.AcademicSessions.Remove(academicSession);

        await _context.SaveChangesAsync();

        return (
            true,
            null
        );
    }
}