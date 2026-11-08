using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.DTOs.SuperAdmin;
using UserManagementApi.Models;             // ← User, Teacher, StudentProfile, Class
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

// ====================================================================
// SUPER ADMIN SERVICE — EDIT / DELETE
// ====================================================================
// Second half of SuperAdminService (same class, split for size).
// In SuperAdminService.cs change the class line to:
//     public partial class SuperAdminService(
//
// Every record is checked against the school in the URL, so a super
// admin can't edit a teacher of school A through school B's route.

public partial class SuperAdminService
{
    // ================================================================
    // TEACHER — UPDATE
    // ================================================================

    public async Task<SuperAdminResult> UpdateSchoolTeacherAsync(
        Guid schoolId,
        Guid teacherId,
        SuperAdminUpdateTeacherRequest request)
    {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.Id == teacherId && t.SchoolId == schoolId);

        if (teacher == null)
            return SuperAdminResult.Missing("Teacher not found in this school.");

        var user = await _userManager.FindByIdAsync(teacher.UserId);

        if (user == null)
            return SuperAdminResult.Missing("This teacher's login account is missing.");

        var error = await UpdateUserAsync(
            user,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.NewPassword);

        if (error != null) return SuperAdminResult.Fail(error);

        return SuperAdminResult.Ok(new
        {
            message = "Teacher updated successfully.",
            teacher = new
            {
                teacherId = teacher.Id,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber
            }
        });
    }

    // ================================================================
    // TEACHER — DELETE
    // ================================================================
    // Removes their subject + class-teacher assignments, the teacher
    // profile and the login account.

    public async Task<SuperAdminResult> DeleteSchoolTeacherAsync(
        Guid schoolId,
        Guid teacherId)
    {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.Id == teacherId && t.SchoolId == schoolId);

        if (teacher == null)
            return SuperAdminResult.Missing("Teacher not found in this school.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.TeacherSubjects
                .Where(x => x.TeacherId == teacherId)
                .ExecuteDeleteAsync();

            await _context.TeacherClasses
                .Where(x => x.TeacherId == teacherId)
                .ExecuteDeleteAsync();

            await _context.ClassTeachers
                .Where(x => x.TeacherId == teacherId)
                .ExecuteDeleteAsync();

            var userId = teacher.UserId;

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            var deleteError = await DeleteUserAsync(userId);
            if (deleteError != null)
            {
                await transaction.RollbackAsync();
                return SuperAdminResult.Fail(deleteError);
            }

            await transaction.CommitAsync();

            return SuperAdminResult.Ok(new { message = "Teacher deleted successfully." });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return SuperAdminResult.Fail(
                "This teacher still has records linked to them (for example " +
                "assignments or results they created), so they can't be " +
                "deleted. Reassign or remove those first.");
        }
    }

    // ================================================================
    // STUDENT — UPDATE
    // ================================================================

    public async Task<SuperAdminResult> UpdateSchoolStudentAsync(
        Guid schoolId,
        Guid studentId,
        SuperAdminUpdateStudentRequest request)
    {
        var student = await _context.StudentProfiles
            .FirstOrDefaultAsync(s => s.Id == studentId && s.SchoolId == schoolId);

        if (student == null)
            return SuperAdminResult.Missing("Student not found in this school.");

        var user = await _userManager.FindByIdAsync(student.UserId);

        if (user == null)
            return SuperAdminResult.Missing("This student's login account is missing.");

        // ---- placement (class, department, trade) ---------------------
        var placement = await ValidatePlacementAsync(
            schoolId,
            request.ClassId,
            request.DepartmentId,
            request.TradeId,
            request.TradeSubjectId);

        if (placement.Error != null) return SuperAdminResult.Fail(placement.Error);

        // ---- student number ------------------------------------------
        var studentNumber = request.StudentNumber.Trim();

        var numberTaken = await _context.StudentProfiles.AnyAsync(s =>
            s.SchoolId == schoolId &&
            s.Id != studentId &&
            s.StudentNumber == studentNumber);

        if (numberTaken)
            return SuperAdminResult.Fail("This student number is already in use.");

        // ---- save ------------------------------------------------------
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var error = await UpdateUserAsync(
                user,
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.NewPassword);

            if (error != null)
            {
                await transaction.RollbackAsync();
                return SuperAdminResult.Fail(error);
            }

            student.StudentNumber = studentNumber;
            student.ClassId = placement.ClassId;
            student.DepartmentId = request.DepartmentId;
            student.TradeId = placement.TradeId;
            student.TradeSubjectId = request.TradeSubjectId;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return SuperAdminResult.Ok(new
            {
                message = "Student updated successfully.",
                student = new
                {
                    studentId = student.Id,
                    fullName = user.FullName,
                    studentNumber,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    classId = placement.ClassId,
                    className = placement.ClassName,
                    departmentId = request.DepartmentId,
                    departmentName = placement.DepartmentName,
                    tradeId = placement.TradeId,
                    tradeName = placement.TradeName
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return SuperAdminResult.Fail($"Could not update the student: {ex.Message}");
        }
    }

    // ================================================================
    // STUDENT — DELETE
    // ================================================================
    // Removes the student's results, attendance and assignment
    // submissions, the profile and the login account.

    public async Task<SuperAdminResult> DeleteSchoolStudentAsync(
        Guid schoolId,
        Guid studentId)
    {
        var student = await _context.StudentProfiles
            .FirstOrDefaultAsync(s => s.Id == studentId && s.SchoolId == schoolId);

        if (student == null)
            return SuperAdminResult.Missing("Student not found in this school.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ← If one of these doesn't compile, rename it to your DbSet /
            //   column, or delete the line if that table doesn't exist.
            await _context.StudentResults
                .Where(x => x.StudentId == studentId)
                .ExecuteDeleteAsync();

            await _context.AttendanceRecords
                .Where(x => x.StudentId == studentId)
                .ExecuteDeleteAsync();

            await _context.AssignmentSubmissions
                .Where(x => x.StudentId == studentId)
                .ExecuteDeleteAsync();

            var userId = student.UserId;

            _context.StudentProfiles.Remove(student);
            await _context.SaveChangesAsync();

            var deleteError = await DeleteUserAsync(userId);
            if (deleteError != null)
            {
                await transaction.RollbackAsync();
                return SuperAdminResult.Fail(deleteError);
            }

            await transaction.CommitAsync();

            return SuperAdminResult.Ok(new { message = "Student deleted successfully." });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return SuperAdminResult.Fail(
                "This student still has records linked to them (for example " +
                "a parent link), so they can't be deleted yet. Remove those " +
                "first.");
        }
    }

    // ================================================================
    // CLASS — UPDATE (rename)
    // ================================================================

    public async Task<SuperAdminResult> UpdateSchoolClassAsync(
        Guid schoolId,
        Guid classId,
        SuperAdminUpdateClassRequest request)
    {
        var schoolClass = await _context.Classes
            .FirstOrDefaultAsync(c => c.Id == classId && c.SchoolId == schoolId);

        if (schoolClass == null)
            return SuperAdminResult.Missing("Class not found in this school.");

        var name = request.Name.Trim();

        if (string.IsNullOrEmpty(name))
            return SuperAdminResult.Fail("Class name is required.");

        var duplicate = await _context.Classes.AnyAsync(c =>
            c.SchoolId == schoolId &&
            c.Id != classId &&
            c.Name.ToLower() == name.ToLower());

        if (duplicate)
            return SuperAdminResult.Fail($"A class named \"{name}\" already exists in this school.");

        schoolClass.Name = name;
        await _context.SaveChangesAsync();

        return SuperAdminResult.Ok(new
        {
            message = "Class updated successfully.",
            @class = new { classId = schoolClass.Id, className = schoolClass.Name }
        });
    }

    // ================================================================
    // CLASS — DELETE
    // ================================================================
    // Refused while students are still in the class (move them first).
    // Removes the class's subjects and teacher assignments with it.

    public async Task<SuperAdminResult> DeleteSchoolClassAsync(
        Guid schoolId,
        Guid classId)
    {
        var schoolClass = await _context.Classes
            .FirstOrDefaultAsync(c => c.Id == classId && c.SchoolId == schoolId);

        if (schoolClass == null)
            return SuperAdminResult.Missing("Class not found in this school.");

        var studentCount = await _context.StudentProfiles
            .CountAsync(s => s.ClassId == classId);

        if (studentCount > 0)
        {
            return SuperAdminResult.Fail(
                $"{schoolClass.Name} still has {studentCount} " +
                $"{(studentCount == 1 ? "student" : "students")}. " +
                "Move them to another class before deleting it.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.ClassSubjects
                .Where(x => x.ClassId == classId)
                .ExecuteDeleteAsync();

            await _context.TeacherSubjects
                .Where(x => x.ClassId == classId)
                .ExecuteDeleteAsync();

            await _context.TeacherClasses
                .Where(x => x.ClassId == classId)
                .ExecuteDeleteAsync();

            await _context.ClassTeachers
                .Where(x => x.ClassId == classId)
                .ExecuteDeleteAsync();

            _context.Classes.Remove(schoolClass);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return SuperAdminResult.Ok(new { message = "Class deleted successfully." });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return SuperAdminResult.Fail(
                "This class still has records linked to it (for example " +
                "assignments or results), so it can't be deleted.");
        }
    }

    // ================================================================
    // CLASS — REMOVE CLASS TEACHER
    // ================================================================

    public async Task<SuperAdminResult> RemoveClassTeacherAsync(
        Guid schoolId,
        Guid classId)
    {
        var exists = await _context.Classes
            .AnyAsync(c => c.Id == classId && c.SchoolId == schoolId);

        if (!exists)
            return SuperAdminResult.Missing("Class not found in this school.");

        var removed =
            await _context.TeacherClasses
                .Where(x => x.ClassId == classId)
                .ExecuteDeleteAsync()
            +
            await _context.ClassTeachers
                .Where(x => x.ClassId == classId)
                .ExecuteDeleteAsync();

        if (removed == 0)
            return SuperAdminResult.Fail("This class has no class teacher.");

        return SuperAdminResult.Ok(new { message = "Class teacher removed." });
    }

    // ================================================================
    // HELPERS
    // ================================================================

    // Updates name / email / phone and (optionally) the password.
    // Returns an error message, or null on success.
    private async Task<string?> UpdateUserAsync(
        User user,
        string fullName,
        string email,
        string? phoneNumber,
        string? newPassword)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var other = await _userManager.FindByEmailAsync(normalizedEmail);
            if (other != null && other.Id != user.Id)
                return "Another user already uses this email.";

            // Email is also the username they sign in with.
            user.Email = normalizedEmail;
            user.UserName = normalizedEmail;
        }

        user.FullName = fullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber.Trim();

        var updated = await _userManager.UpdateAsync(user);
        if (!updated.Succeeded) return IdentityError(updated);

        if (!string.IsNullOrEmpty(newPassword))
        {
            // Works without email-token providers.
            if (await _userManager.HasPasswordAsync(user))
            {
                var removed = await _userManager.RemovePasswordAsync(user);
                if (!removed.Succeeded) return IdentityError(removed);
            }

            var added = await _userManager.AddPasswordAsync(user, newPassword);
            if (!added.Succeeded) return IdentityError(added);
        }

        return null;
    }

    private async Task<string?> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null; // already gone

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded ? null : IdentityError(result);
    }

    private record Placement(
        Guid ClassId,
        string? ClassName,
        string? DepartmentName,
        Guid? TradeId,
        string? TradeName,
        string? Error);

    // Same rules as Create student: class, department and trade must
    // belong to the school; the trade subject must belong to the trade
    // (a trade subject without a trade picks up its trade).
    private async Task<Placement> ValidatePlacementAsync(
        Guid schoolId,
        Guid classId,
        Guid? departmentId,
        Guid? tradeId,
        Guid? tradeSubjectId)
    {
        Placement Fail(string error) => new(classId, null, null, null, null, error);

        var className = await _context.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId && c.SchoolId == schoolId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

        if (className == null)
            return Fail("The selected class does not belong to this school.");

        string? departmentName = null;

        if (departmentId.HasValue)
        {
            departmentName = await _context.Departments
                .AsNoTracking()
                .Where(d => d.Id == departmentId.Value && d.SchoolId == schoolId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();

            if (departmentName == null)
                return Fail("The selected department does not belong to this school.");
        }

        if (tradeSubjectId.HasValue)
        {
            var subjectTradeId = await _context.Subjects
                .AsNoTracking()
                .Where(s => s.Id == tradeSubjectId.Value && s.SchoolId == schoolId)
                .Select(s => s.TradeId)
                .FirstOrDefaultAsync();

            if (subjectTradeId == null)
                return Fail("The selected trade subject is not a trade subject of this school.");

            if (tradeId.HasValue && tradeId != subjectTradeId)
                return Fail("The trade subject does not belong to the selected trade.");

            tradeId = subjectTradeId;
        }

        string? tradeName = null;

        if (tradeId.HasValue)
        {
            tradeName = await _context.Trades
                .AsNoTracking()
                .Where(t => t.Id == tradeId.Value && t.SchoolId == schoolId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();

            if (tradeName == null)
                return Fail("The selected trade does not belong to this school.");
        }

        return new Placement(classId, className, departmentName, tradeId, tradeName, null);
    }
}
