using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Attendance;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Results;
using UserManagementApi.DTOs.TeacherPortal;
using UserManagementApi.Services;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/teacher")]
[Authorize(Roles = Roles.Teacher)]
public class TeacherController(
    ITeacherPortalService teacherService,
    ICurrentUserService currentUser,
    IResultService resultService,
    ApplicationDbContext context) : ControllerBase
{
    private readonly ITeacherPortalService _teacherService = teacherService;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IResultService _resultService = resultService;
    private readonly ApplicationDbContext _context = context;


    // ================================================================
    // CURRENT USER
    // ================================================================

    private string? GetUserId()
    {
        return _currentUser.GetUserId()
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }


    // ================================================================
    // PROFILE
    // ================================================================

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetProfileAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // CLASSES
    // ================================================================

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetClassesAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // SUBJECTS
    // ================================================================

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetSubjectsAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(
 [FromQuery] Guid? classId = null)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetStudentsAsync(
            userId,
            classId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);

    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudentDetails(
    Guid studentId)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User not authenticated."
            });
        }

        var result =
            await _teacherService.GetStudentDetailsAsync(
                userId,
                studentId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // GET RESULTS
    // ============================================================

    [HttpGet("results")]
    public async Task<IActionResult> GetResults(
        [FromQuery] GetTeacherResultsRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.GetResultsAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // CREATE COMPLETE RESULT
    // TEST + EXAM
    // ============================================================

    [HttpPost("results")]
    public async Task<IActionResult> CreateResult(
        [FromBody] UploadResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.UploadResultAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // CREATE TEST RESULT
    // ============================================================

    [HttpPost("results/test")]
    public async Task<IActionResult> CreateTestResult(
        [FromBody] UploadTestResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.UploadTestResultAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // CREATE EXAM RESULT
    // ============================================================

    [HttpPost("results/exam")]
    public async Task<IActionResult> CreateExamResult(
        [FromBody] UploadExamResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.UploadExamResultAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // BULK COMPLETE RESULTS
    // ============================================================

    [HttpPost("results/bulk")]
    public async Task<IActionResult> BulkResults(
        [FromBody] BulkResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.BulkResultAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // BULK TEST RESULTS
    // ============================================================

    [HttpPost("results/bulk/test")]
    public async Task<IActionResult> BulkTestResults(
        [FromBody] BulkTestResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.BulkTestResultAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // BULK EXAM RESULTS
    // ============================================================

    [HttpPost("results/bulk/exam")]
    public async Task<IActionResult> BulkExamResults(
        [FromBody] BulkExamResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.BulkExamResultAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // UPDATE COMPLETE RESULT
    // ============================================================

    [HttpPut("results/{resultId:guid}")]
    public async Task<IActionResult> UpdateResult(
        Guid resultId,
        [FromBody] UpdateResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.UpdateResultAsync(
            userId,
            resultId,
            request);

        if (!result.Success)
        {
            if (result.Error == "Result not found.")
            {
                return NotFound(new
                {
                    message = result.Error
                });
            }

            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // UPDATE TEST ONLY
    // ============================================================

    [HttpPut("results/{resultId:guid}/test")]
    public async Task<IActionResult> UpdateTestResult(
        Guid resultId,
        [FromBody] UpdateTestResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.UpdateTestResultAsync(
            userId,
            resultId,
            request);

        if (!result.Success)
        {
            if (result.Error == "Result not found.")
            {
                return NotFound(new
                {
                    message = result.Error
                });
            }

            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // UPDATE EXAM ONLY
    // ============================================================

    [HttpPut("results/{resultId:guid}/exam")]
    public async Task<IActionResult> UpdateExamResult(
        Guid resultId,
        [FromBody] UpdateExamResultRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.UpdateExamResultAsync(
            userId,
            resultId,
            request);

        if (!result.Success)
        {
            if (result.Error == "Result not found.")
            {
                return NotFound(new
                {
                    message = result.Error
                });
            }

            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // DELETE RESULT
    // ============================================================

    [HttpDelete("results/{resultId:guid}")]
    public async Task<IActionResult> DeleteResult(
        Guid resultId)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "User authentication is required."
            });
        }

        var result = await _resultService.DeleteResultAsync(
            userId,
            resultId);

        if (!result.Success)
        {
            if (result.Error == "Result not found.")
            {
                return NotFound(new
                {
                    message = result.Error
                });
            }

            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Result deleted successfully.",
            data = result.Data
        });
    }

    // ================================================================
    // ASSIGNMENTS
    // ================================================================

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] GetTeacherAssignmentsRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetAssignmentsAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // SINGLE ASSIGNMENT
    // ================================================================

    [HttpGet("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> GetAssignment(
        Guid assignmentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetAssignmentAsync(
            userId,
            assignmentId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // CREATE ASSIGNMENT
    // ================================================================

    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreateAssignmentRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.CreateAssignmentAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        // The service returns an anonymous object containing
        // assignmentId, so return the created data directly.
        return StatusCode(
            StatusCodes.Status201Created,
            result.Data);
    }


    // ================================================================
    // UPDATE ASSIGNMENT
    // ================================================================

    [HttpPut("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> UpdateAssignment(
        Guid assignmentId,
        [FromBody] UpdateAssignmentRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.UpdateAssignmentAsync(
            userId,
            assignmentId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // DELETE ASSIGNMENT
    // ================================================================

    [HttpDelete("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> DeleteAssignment(
        Guid assignmentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.DeleteAssignmentAsync(
            userId,
            assignmentId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Assignment deleted successfully."
        });
    }


    // ============================================================
    // BULK CLASS ATTENDANCE
    // ============================================================

    [HttpPost("bulk-attendance")]
public async Task<IActionResult> CreateBulkAttendance(
    [FromBody] CreateBulkAttendanceRequest request)
{
    try
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.CreateBulkAttendanceAsync(
            userId,
            request
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Error
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Data
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine("BULK ATTENDANCE ERROR:");
        Console.WriteLine(ex.ToString());

        return StatusCode(500, new
        {
            success = false,
            message = ex.Message,
            detail = ex.InnerException?.Message
        });
    }
}




    // ================================================================
    // ATTENDANCE
    // ================================================================

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] GetTeacherAttendanceRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetAttendanceAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // SINGLE ATTENDANCE RECORD
    // ================================================================

    [HttpGet("attendance/{attendanceId:guid}")]
    public async Task<IActionResult> GetAttendanceRecord(
        Guid attendanceId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetAttendanceRecordAsync(
            userId,
            attendanceId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // CREATE ATTENDANCE
    // ================================================================

    [HttpPost("attendance")]
    public async Task<IActionResult> CreateAttendance(
        [FromBody] CreateAttendanceRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.CreateAttendanceAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // UPDATE ATTENDANCE
    // ================================================================

    [HttpPut("attendance/{attendanceId:guid}")]
    public async Task<IActionResult> UpdateAttendance(
        Guid attendanceId,
        [FromBody] UpdateAttendanceRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.UpdateAttendanceAsync(
            userId,
            attendanceId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // DELETE ATTENDANCE
    // ================================================================

    [HttpDelete("attendance/{attendanceId:guid}")]
    public async Task<IActionResult> DeleteAttendance(
        Guid attendanceId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.DeleteAttendanceAsync(
            userId,
            attendanceId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Attendance record deleted successfully."
        });
    }
}