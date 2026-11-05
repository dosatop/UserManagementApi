using Microsoft.EntityFrameworkCore;

namespace UserManagementApi.Data;

public static class StudentSubjectQueries
{
    // ================================================================
    // SUBJECT IDS A STUDENT ACTUALLY TAKES
    // ================================================================
    //   • general subjects of their class
    //   • their department's subjects
    //   • their trade subject (or every subject of their trade if no
    //     specific one is picked)
    // ================================================================

    public static async Task<List<Guid>> GetStudentSubjectIdsAsync(
        this ApplicationDbContext db,
        Guid studentProfileId)
    {
        var s = await db.StudentProfiles
            .AsNoTracking()
            .Where(x => x.Id == studentProfileId)
            .Select(x => new
            {
                x.ClassId,
                x.DepartmentId,
                x.TradeId,
                x.TradeSubjectId
            })
            .FirstOrDefaultAsync();

        if (s == null || s.ClassId == Guid.Empty)
            return [];

        Guid? departmentId = s.DepartmentId;
        Guid? tradeId = s.TradeId;
        Guid? tradeSubjectId = s.TradeSubjectId;

        return await db.ClassSubjects
            .AsNoTracking()
            .Where(cs => cs.ClassId == s.ClassId)
            .Select(cs => cs.Subject)
            .Where(sub =>
                (sub.DepartmentId == null && sub.TradeId == null) ||
                (departmentId != null && sub.DepartmentId == departmentId) ||
                (tradeSubjectId != null && sub.Id == tradeSubjectId) ||
                (tradeSubjectId == null && tradeId != null && sub.TradeId == tradeId))
            .Select(sub => sub.Id)
            .Distinct()
            .ToListAsync();
    }
}