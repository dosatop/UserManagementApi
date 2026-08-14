using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Results;
using UserManagementApi.Models.Results;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class ResultImportService(
    ApplicationDbContext context, IResultGradingService gradingService) : IResultImportService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IResultGradingService _gradingService = gradingService;

    private static string CalculateGrade(decimal score)
    {
        return score switch
        {
            >= 70 => "A",
            >= 60 => "B",
            >= 50 => "C",
            >= 45 => "D",
            >= 40 => "E",
            _ => "F"
        };
    }

    public async Task<(bool Success, object? Data, string? Error)>
    CreateAsync(
        Guid schoolId,
        CreateResultRequest request)
    {
        // Validate student
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Id == request.StudentId &&
                x.SchoolId == schoolId);

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found in this school."
            );
        }

        // Validate subject
        var subject = await _context.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.SubjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject not found in this school."
            );
        }

        // Validate class
        var classroom = await _context.Classes
            .AsNoTracking()
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

        // Check duplicate result
        var exists = await _context.StudentResults
            .AnyAsync(x =>
                x.StudentId == request.StudentId &&
                x.SubjectId == request.SubjectId &&
                x.ClassId == request.ClassId &&
                x.Session == request.Session &&
                x.Term == request.Term &&
                x.SchoolId == schoolId);

        if (exists)
        {
            return (
                false,
                null,
                "A result already exists for this student, subject, session and term."
            );
        }

        var result = new StudentResult
        {
            Id = Guid.NewGuid(),

            StudentId = request.StudentId,
            SubjectId = request.SubjectId,
            ClassId = request.ClassId,
            SchoolId = schoolId,

            Session = request.Session,
            Term = request.Term,

            Score = request.Score,
            ExamScore = request.ExamScore,
            TestScore = request.TestScore,

            Grade = CalculateGrade(request.Score),

            Remark = request.Remark,

            CreatedAt = DateTime.UtcNow
        };

        _context.StudentResults.Add(result);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                resultId = result.Id,

                studentId = student.Id,
                studentName = student.User.FullName,

                subjectId = subject.Id,
                subjectName = subject.Name,

                classId = classroom.Id,
                className = classroom.Name,

                schoolId,

                result.Session,
                result.Term,
                result.Score,
                result.ExamScore,
                result.TestScore,
                result.Grade,
                result.Remark,
                result.CreatedAt
            },
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
  GetAsync(
      Guid schoolId,
      GetResultsRequest request)
    {
        var query = _context.StudentResults
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId);

        // ============================================================
        // FILTER BY STUDENT
        // ============================================================

        if (request.StudentId.HasValue)
        {
            query = query.Where(x =>
                x.StudentId == request.StudentId.Value);
        }

        // ============================================================
        // FILTER BY SUBJECT
        // ============================================================

        if (request.SubjectId.HasValue)
        {
            query = query.Where(x =>
                x.SubjectId == request.SubjectId.Value);
        }

        // ============================================================
        // FILTER BY CLASS
        // ============================================================

        if (request.ClassId.HasValue)
        {
            query = query.Where(x =>
                x.ClassId == request.ClassId.Value);
        }

        // ============================================================
        // FILTER BY SESSION
        // ============================================================

        if (!string.IsNullOrWhiteSpace(request.Session))
        {
            query = query.Where(x =>
                x.Session == request.Session);
        }

        // ============================================================
        // FILTER BY TERM
        // ============================================================

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            query = query.Where(x =>
                x.Term == request.Term);
        }

        // ============================================================
        // RESULT
        // ============================================================

        var results = await query
            .OrderBy(x => x.Student.User.FullName)
            .ThenBy(x => x.Subject.Name)
            .Select(x => new
            {
                resultId = x.Id,

                // ====================================================
                // STUDENT
                // ====================================================

                studentId = x.StudentId,

                studentName = x.Student.User.FullName,

                studentNumber = x.Student.StudentNumber,

                // ====================================================
                // SUBJECT
                // ====================================================

                subjectId = x.SubjectId,

                subjectName = x.Subject.Name,

                subjectCode = x.Subject.Code,

                // ====================================================
                // CLASS
                // ====================================================

                classId = x.ClassId,

                className = x.Class.Name,

                // ====================================================
                // SCHOOL
                // ====================================================

                schoolId = x.SchoolId,

                // ====================================================
                // ACADEMIC
                // ====================================================

                session = x.Session,

                term = x.Term,

                // ====================================================
                // SCORES
                // ====================================================

                score = x.Score,

                examScore = x.ExamScore,

                testScore = x.TestScore,

                // ====================================================
                // RESULT
                // ====================================================

                grade = x.Grade,

                remark = x.Remark,

                createdAt = x.CreatedAt
            })
            .ToListAsync();

        return (
            true,
            results,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
    UpdateAsync(
        Guid schoolId,
        Guid resultId,
        UpdateResultRequest request)
    {
        var result = await _context.StudentResults
            .Include(x => x.Student)
                .ThenInclude(x => x.User)
            .Include(x => x.Subject)
            .Include(x => x.Class)
            .FirstOrDefaultAsync(x =>
                x.Id == resultId &&
                x.SchoolId == schoolId);

        if (result == null)
        {
            return (
                false,
                null,
                "Result not found in this school."
            );
        }

        result.Score = request.Score;
        result.ExamScore = request.ExamScore;
        result.TestScore = request.TestScore;
        result.Remark = request.Remark;

        result.Grade = CalculateGrade(request.Score);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                resultId = result.Id,

                studentId = result.StudentId,
                studentName = result.Student.User.FullName,

                subjectId = result.SubjectId,
                subjectName = result.Subject.Name,

                classId = result.ClassId,
                className = result.Class.Name,

                schoolId = result.SchoolId,

                result.Session,
                result.Term,
                result.Score,
                result.ExamScore,
                result.TestScore,
                result.Grade,
                result.Remark,
                result.CreatedAt
            },
            null
        );
    }

    public async Task<(bool Success, string? Error)>
    DeleteAsync(
        Guid schoolId,
        Guid resultId)
    {
        var result = await _context.StudentResults
            .FirstOrDefaultAsync(x =>
                x.Id == resultId &&
                x.SchoolId == schoolId);

        if (result == null)
        {
            return (
                false,
                "Result not found in this school."
            );
        }

        _context.StudentResults.Remove(result);

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(
        bool Success,
        ResultImportPreviewResponse? Data,
        string? Error
    )> PreviewAsync(
        Guid schoolId,
        ImportResultsRequest request)
    {
        // -------------------------
        // 1. Validate school
        // -------------------------

        var school = await _context.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return (
                false,
                null,
                "School not found."
            );
        }

        // -------------------------
        // 2. Validate class
        // -------------------------

        var classroom = await _context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class does not belong to this school."
            );
        }

        // -------------------------
        // 3. Validate subject
        // -------------------------

        var subject = await _context.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.SubjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject does not belong to this school."
            );
        }

        // -------------------------
        // 4. Validate file
        // -------------------------

        if (request.File == null ||
            request.File.Length == 0)
        {
            return (
                false,
                null,
                "Please upload a result file."
            );
        }

        var extension =
            Path.GetExtension(request.File.FileName)
                .ToLowerInvariant();

        if (extension != ".xlsx")
        {
            return (
                false,
                null,
                "Only .xlsx files are currently supported."
            );
        }

        // -------------------------
        // 5. Read Excel
        // -------------------------

        using var stream = request.File.OpenReadStream();

        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            return (
                false,
                null,
                "The Excel file contains no worksheet."
            );
        }

        var preview = new ResultImportPreviewResponse();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        // Assuming row 1 contains headers
        for (var rowNumber = 2;
             rowNumber <= lastRow;
             rowNumber++)
        {
            var row = worksheet.Row(rowNumber);

            var studentNumber =
                row.Cell(1).GetString().Trim();

            var testText =
                row.Cell(2).GetString().Trim();

            var examText =
                row.Cell(3).GetString().Trim();

            var resultRow = new ResultImportRow
            {
                RowNumber = rowNumber,
                StudentNumber = studentNumber
            };

            // -------------------------
            // Student number
            // -------------------------

            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                resultRow.Errors.Add(
                    "Student number is required.");
            }
            else
            {
                var student =
                    await _context.StudentProfiles
                        .AsNoTracking()
                        .Include(x => x.User)
                        .FirstOrDefaultAsync(x =>
                            x.SchoolId == schoolId &&
                            x.StudentNumber == studentNumber);

                if (student == null)
                {
                    resultRow.Errors.Add(
                        "Student was not found in this school.");
                }
                else
                {
                    resultRow.StudentName =
                        student.User.FullName;

                    // Make sure student belongs to selected class
                    if (student.ClassId != request.ClassId)
                    {
                        resultRow.Errors.Add(
                            "Student does not belong to the selected class.");
                    }
                }
            }

            // -------------------------
            // Test score
            // -------------------------

            if (!decimal.TryParse(
                    testText,
                    out var testScore))
            {
                resultRow.Errors.Add(
                    "Test score must be a valid number.");
            }
            else if (testScore < 0 || testScore > 40)
            {
                resultRow.Errors.Add(
                    "Test score must be between 0 and 40.");
            }
            else
            {
                resultRow.TestScore = testScore;
            }

            // -------------------------
            // Exam score
            // -------------------------

            if (!decimal.TryParse(
                    examText,
                    out var examScore))
            {
                resultRow.Errors.Add(
                    "Exam score must be a valid number.");
            }
            else if (examScore < 0 || examScore > 60)
            {
                resultRow.Errors.Add(
                    "Exam score must be between 0 and 60.");
            }
            else
            {
                resultRow.ExamScore = examScore;
            }

            // -------------------------
            // Total
            // -------------------------

            if (resultRow.TestScore.HasValue &&
                resultRow.ExamScore.HasValue)
            {
                resultRow.Score =
                    resultRow.TestScore.Value +
                    resultRow.ExamScore.Value;
            }

            resultRow.IsValid =
                resultRow.Errors.Count == 0;

            preview.Rows.Add(resultRow);
        }

        preview.TotalRows = preview.Rows.Count;

        preview.ValidRows =
            preview.Rows.Count(x => x.IsValid);

        preview.InvalidRows =
            preview.Rows.Count(x => !x.IsValid);

        preview.CanImport =
            preview.TotalRows > 0 &&
            preview.InvalidRows == 0;

        return (
            true,
            preview,
            null
        );
    }

    public async Task<(
    bool Success,
    object? Data,
    string? Error
)> ConfirmImportAsync(
    Guid schoolId,
    ConfirmResultImportRequest request)
    {
        // -------------------------
        // 1. Validate school
        // -------------------------

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

        // -------------------------
        // 2. Validate class
        // -------------------------

        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class does not belong to this school."
            );
        }

        // -------------------------
        // 3. Validate subject
        // -------------------------

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == request.SubjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject does not belong to this school."
            );
        }

        if (request.Rows == null ||
            request.Rows.Count == 0)
        {
            return (
                false,
                null,
                "There are no results to import."
            );
        }

        // -------------------------
        // 4. Validate all rows
        // BEFORE saving anything
        // -------------------------

        var results = new List<StudentResult>();

        foreach (var row in request.Rows)
        {
            var student =
                await _context.StudentProfiles
                    .FirstOrDefaultAsync(x =>
                        x.SchoolId == schoolId &&
                        x.ClassId == request.ClassId &&
                        x.StudentNumber == row.StudentNumber);

            if (student == null)
            {
                return (
                    false,
                    null,
                    $"Student '{row.StudentNumber}' was not found in this class."
                );
            }

            // -------------------------
            // Score validation
            // -------------------------

            if (row.TestScore < 0 ||
                row.TestScore > 40)
            {
                return (
                    false,
                    null,
                    $"Invalid test score for student '{row.StudentNumber}'."
                );
            }

            if (row.ExamScore < 0 ||
                row.ExamScore > 60)
            {
                return (
                    false,
                    null,
                    $"Invalid exam score for student '{row.StudentNumber}'."
                );
            }

            // -------------------------
            // Prevent duplicates
            // -------------------------

            var existingResult =
                await _context.StudentResults
                    .AnyAsync(x =>
                        x.StudentId == student.Id &&
                        x.SubjectId == request.SubjectId &&
                        x.Session == request.Session &&
                        x.Term == request.Term);

            if (existingResult)
            {
                return (
                    false,
                    null,
                    $"A result already exists for '{row.StudentNumber}'."
                );
            }

            // -------------------------
            // Create result
            // -------------------------

            var total =
                row.TestScore +
                row.ExamScore;

            var grade = _gradingService.Calculate(total);

            results.Add(new StudentResult
            {
                Id = Guid.NewGuid(),

                StudentId = student.Id,

                SchoolId = schoolId,

                SubjectId = request.SubjectId,

                ClassId = request.ClassId,

                Session = request.Session,

                Term = request.Term,

                TestScore = row.TestScore,

                ExamScore = row.ExamScore,

                Score = total,

                Grade = grade.Grade,

                Remark = row.Remark
            });
        }

        // -------------------------
        // 5. Save everything
        // -------------------------

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.StudentResults.AddRangeAsync(results);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            return (
                false,
                null,
                "An error occurred while saving the results."
            );
        }

        // -------------------------
        // 6. Return summary
        // -------------------------

        return (
            true,
            new
            {
                Imported = results.Count,
                School = school.Name,
                Class = classroom.Name,
                Subject = subject.Name,
                request.Session,
                request.Term
            },
            null
        );
    }
}