using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Classes;
using UserManagementApi.DTOs.ClassSubjects;
using UserManagementApi.DTOs.Departments;
using UserManagementApi.DTOs.Parents;
using UserManagementApi.DTOs.Results;
using UserManagementApi.DTOs.Students;
using UserManagementApi.DTOs.Subjects;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.DTOs.Trades;
using UserManagementApi.Services;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;


public abstract class SchoolAdminControllerBase : ControllerBase
{
    protected Guid? GetSchoolId()
    {
        var value = User.FindFirst("SchoolId")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var schoolId)
            ? schoolId
            : null;
    }
}



[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(IAdminService adminService, ITeacherService teacherService, IStudentService studentService, IClassService classService, ISubjectService subjectService, IParentService parentService, IResultService resultService, IDepartmentService departmentService, ITradeService tradeService, IClassSubjectService classSubjectService) : SchoolAdminControllerBase
{
    private readonly IAdminService _adminService = adminService;
    private readonly ITeacherService _teacherService = teacherService;
    private readonly IStudentService _studentService = studentService;
    private readonly IClassService _classService = classService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IParentService _parentService = parentService;

    private readonly IResultService _resultService = resultService;
    private readonly IDepartmentService _departmentService = departmentService;
    private readonly ITradeService _tradeService = tradeService;
    private readonly IClassSubjectService _classSubjectService = classSubjectService;

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }


    // ================================================================
    // DASHBOARD
    // ================================================================

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var dashboard = await _adminService.GetDashboardAsync(schoolId.Value);

        if (dashboard == null)
        {
            return NotFound(new
            {
                message = "School not found."
            });
        }

        return Ok(dashboard);
    }

    // ================================================================
    // CREATE TEACHERS
    // ================================================================
    [HttpPost("teachers")]
    public async Task<IActionResult> CreateTeacher(
        [FromBody] CreateTeacherRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _teacherService.CreateTeacherAsync(
            schoolId.Value,
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
    // GET TEACHERS
    // ================================================================

    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var teachers = await _adminService
            .GetTeachersAsync(schoolId.Value);

        return Ok(teachers);
    }


    [HttpGet("teachers/{teacherId:guid}")]
    public async Task<IActionResult> GetTeacher(
    Guid teacherId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _teacherService.GetTeacherByIdAsync(
            schoolId.Value,
            teacherId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpPut("teachers/{teacherId:guid}")]
    public async Task<IActionResult> UpdateTeacher(
        Guid teacherId,
        [FromBody] UpdateTeacherRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _teacherService.UpdateTeacherAsync(
            schoolId.Value,
            teacherId,
            request);

        if (!result.Success)
        {
            if (result.Error == "Teacher not found.")
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

    [HttpDelete("teachers/{teacherId:guid}")]
    public async Task<IActionResult> DeleteTeacher(
        Guid teacherId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _teacherService.DeleteTeacherAsync(
            schoolId.Value,
            teacherId);

        if (!result.Success)
        {
            if (result.Error == "Teacher not found.")
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


    // ================================================================
    // CREATE STUDENTS
    // ================================================================
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent(
        CreateStudentRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _studentService.CreateStudentAsync(
                schoolId.Value,
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

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(
      [FromQuery] Guid? classId,
      [FromQuery] Guid? subjectId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }


        var result = await _studentService.GetStudentsByClassOrSubjectAsync(
            schoolId.Value,
            classId,
            subjectId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("students/{studentId:guid}/records")]
    public async Task<IActionResult> GetStudent(
       Guid studentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var student = await _adminService.GetStudentAsync(
            schoolId.Value,
            studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        return Ok(student);
    }

    [HttpPut("students/{studentId:guid}")]
    public async Task<IActionResult> UpdateStudent(
    Guid studentId,
    UpdateStudentRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _studentService.UpdateStudentAsync(
                schoolId.Value,
                studentId,
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


    [HttpDelete("students/{studentId:guid}")]
    public async Task<IActionResult> DeleteStudent(
        Guid studentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _studentService.DeleteStudentAsync(
                schoolId.Value,
                studentId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Student deleted successfully."
        });
    }

    [HttpPut("{studentId:guid}/academic-path")]
    public async Task<IActionResult> UpdateStudentAcademicPath(
    Guid studentId,
    [FromBody] UpdateStudentAcademicPathRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _studentService.UpdateStudentAcademicPathAsync(
                schoolId.Value,
                studentId,
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


    [HttpGet("students/{studentId:guid}/assignments")]
    public async Task<IActionResult> GetStudentAssignments(
     Guid studentId,
     [FromQuery] string session,
     [FromQuery] string term)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School ID not found."
            });
        }

        var result = await _studentService.GetStudentAssignmentsAsync(
            schoolId.Value,
            studentId,
            session,
            term);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("students/{studentId:guid}/attendance")]
    public async Task<IActionResult> GetStudentAttendance(
        Guid studentId,
        [FromQuery] string session,
        [FromQuery] string term)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School ID not found."
            });
        }

        var result = await _studentService
            .GetStudentAttendanceAsync(
                schoolId.Value,
                studentId,
                session,
                term);

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
    // CREATE CLASSES
    // ================================================================
    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass(
        CreateClassRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _classService.CreateClassAsync(
            schoolId.Value,
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
    // GET CLASSES
    // ================================================================

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var classes = await _adminService
            .GetClassesAsync(schoolId.Value);

        return Ok(classes);
    }


    [HttpGet("classes/{classId:guid}")]
    public async Task<IActionResult> GetClass(
    Guid classId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _classService.GetClassAsync(
            schoolId.Value,
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

    [HttpPut("classes/{classId:guid}")]
    public async Task<IActionResult> UpdateClass(
        Guid classId,
        CreateClassRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _classService.UpdateClassAsync(
            schoolId.Value,
            classId,
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

    [HttpDelete("classes/{classId:guid}")]
    public async Task<IActionResult> DeleteClass(
        Guid classId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _classService.DeleteClassAsync(
            schoolId.Value,
            classId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Class deleted successfully."
        });
    }

    [HttpGet("classes/{classId:guid}/assignments")]
    public async Task<IActionResult> GetClassAssignments(
    Guid classId,
    [FromQuery] string session,
    [FromQuery] string term)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School ID not found."
            });
        }

        var result = await _classService.GetClassAssignmentsAsync(
            schoolId.Value,
            classId,
            session,
            term);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("classes/{classId:guid}/assignments/count")]
    public async Task<IActionResult> GetAssignmentCount(
        Guid classId,
        [FromQuery] string session,
        [FromQuery] string term)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School ID not found."
            });
        }

        var result = await _classService.GetAssignmentCountAsync(
            schoolId.Value,
            classId,
            session,
            term);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetSchoolAssignments(
    [FromQuery] string session,
    [FromQuery] string term)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School ID not found."
            });
        }

        var result = await _classService
            .GetSchoolAssignmentCountAsync(
                schoolId.Value,
                session,
                term);

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
    // CREATE SUBJECTS
    // ================================================================
    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(
        CreateSubjectRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _subjectService.CreateSubjectAsync(
            schoolId.Value,
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
    // GET SUBJECTS
    // ================================================================

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var subjects = await _adminService
            .GetSubjectsAsync(schoolId.Value);

        return Ok(subjects);
    }


    [HttpGet("subjects/{subjectId:guid}")]
    public async Task<IActionResult> GetSubject(
    Guid subjectId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _subjectService.GetSubjectAsync(
            schoolId.Value,
            subjectId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpPut("subjects/{subjectId:guid}")]
    public async Task<IActionResult> UpdateSubject(
        Guid subjectId,
        CreateSubjectRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _subjectService.UpdateSubjectAsync(
            schoolId.Value,
            subjectId,
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


    [HttpDelete("subjects/{subjectId:guid}")]
    public async Task<IActionResult> DeleteSubject(
        Guid subjectId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _subjectService.DeleteSubjectAsync(
            schoolId.Value,
            subjectId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Subject deleted successfully."
        });
    }

    // ================================================================
    // CREATE PARENTS
    // ================================================================
    [HttpPost("parents")]
    public async Task<IActionResult> CreateParent(
        CreateParentRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _parentService.CreateParentAsync(
            schoolId.Value,
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
    // GET ALL PARENTS
    // ================================================================

    [HttpGet("parents")]
    public async Task<IActionResult> GetParents()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var parents = await _parentService.GetParentsAsync(
            schoolId.Value);

        return Ok(parents);
    }


    [HttpGet("parents/{parentId:guid}")]
    public async Task<IActionResult> GetParent(Guid parentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _parentService.GetParentAsync(
            schoolId.Value,
            parentId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    [HttpPut("parents/{parentId:guid}")]
    public async Task<IActionResult> UpdateParent(
        Guid parentId,
        UpdateParentRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _parentService.UpdateParentAsync(
            schoolId.Value,
            parentId,
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


    [HttpDelete("parents/{parentId:guid}")]
    public async Task<IActionResult> DeleteParent(
        Guid parentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _parentService.DeleteParentAsync(
            schoolId.Value,
            parentId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Parent deleted successfully."
        });
    }


    [HttpGet("classes/{classId:guid}/students")]
    public async Task<IActionResult> GetClassStudents(
        Guid classId,
        [FromQuery] Guid? subjectId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _classService.GetClassStudentsAsync(
            schoolId.Value,
            classId,
            subjectId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpPost("teachers/{teacherId:guid}/teaching-assignments")]
    public async Task<IActionResult> AssignTeachingSubject(
     Guid teacherId,
     [FromBody] AssignTeachingSubjectRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _teacherService.AssignTeachingSubjectAsync(
                schoolId.Value,
                teacherId,
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


    [HttpPost("classes/{classId:guid}/class-teacher/{teacherId:guid}")]
    public async Task<IActionResult> AssignClassTeacher(
        Guid classId,
        Guid teacherId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _teacherService.AssignClassTeacherAsync(
                schoolId.Value,
                teacherId,
                classId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    [HttpDelete("teaching-subjects/{assignmentId:guid}")]
    public async Task<IActionResult> RemoveTeachingSubject(
        Guid assignmentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _teacherService.RemoveTeachingSubjectAsync(
                schoolId.Value,
                assignmentId);

        if (!result.Success)
        {
            if (result.Error == "Teaching subject assignment not found.")
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


    [HttpDelete("class-teachers/{assignmentId:guid}")]
    public async Task<IActionResult> RemoveClassTeacher(
        Guid assignmentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _teacherService.RemoveClassTeacherAsync(
                schoolId.Value,
                assignmentId);

        if (!result.Success)
        {
            if (result.Error == "Class teacher assignment not found.")
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

    // ================================================================
    // RESULTS
    // ================================================================

    // ================================================================
    // CREATE COMPLETE RESULT
    // TEST = 40
    // EXAM = 60
    // ================================================================

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


    // ================================================================
    // BULK COMPLETE RESULTS
    // TEST = 40
    // EXAM = 60
    // ================================================================

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


    // ================================================================
    // BULK TEST RESULTS
    // ================================================================

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


    // ================================================================
    // BULK EXAM RESULTS
    // ================================================================

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


    // ================================================================
    // CREATE TEST RESULT
    // ================================================================

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


    // ================================================================
    // CREATE EXAM RESULT
    // ================================================================

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


    // ================================================================
    // UPDATE COMPLETE RESULT
    // TEST + EXAM
    // ================================================================

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


    // ================================================================
    // UPDATE TEST ONLY
    // ================================================================

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


    // ================================================================
    // UPDATE EXAM ONLY
    // ================================================================

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


    // ================================================================
    // DELETE RESULT
    // ================================================================

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

        // NOTE:
        // You need a DeleteResultAsync method in ITeacherResultService
        // before this endpoint can compile.

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
            message = "Result deleted successfully."
        });
    }

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
    [HttpPost("department")]
    public async Task<IActionResult> CreateDepartmentAsync(
        [FromBody] CreateDepartmentRequest req)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _departmentService.CreateDepartmentAsync(
            schoolId.Value,
            req
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Department created successfully.",
            data = result.Data
        });
    }


    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartmentsAsync()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var departments = await _departmentService
            .GetDepartmentsAsync(schoolId.Value);

        return Ok(new
        {
            data = departments
        });
    }

    [HttpGet("department/{departmentId}")]
    public async Task<IActionResult> GetDepartmentAsync(
        Guid departmentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _departmentService.GetDepartmentAsync(
            schoolId.Value,
            departmentId
        );

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            data = result.Data
        });
    }

    [HttpPut("department/{departmentId}")]
    public async Task<IActionResult> UpdateDepartmentAsync(
    Guid departmentId,
    [FromBody] CreateDepartmentRequest req)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _departmentService.UpdateDepartmentAsync(
            schoolId.Value,
            departmentId,
            req
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Department updated successfully.",
            data = result.Data
        });
    }

    [HttpDelete("department/{departmentId}")]
    public async Task<IActionResult> DeleteDepartmentAsync(
    Guid departmentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _departmentService.DeleteDepartmentAsync(
            schoolId.Value,
            departmentId
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Department deleted successfully."
        });
    }

    [HttpPost("trade")]
    public async Task<IActionResult> CreateTradeAsync(
        [FromBody] CreateTradeRequest req)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _tradeService.CreateTradeAsync(
            schoolId.Value,
            req);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Trade Created Successfully.",
            data = result.Data
        });
    }

    [HttpGet("trades")]
    public async Task<IActionResult> GetTradesAsync()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var trades = await _tradeService.GetTradesAsync(
            schoolId.Value
        );

        return Ok(trades);
    }

    [HttpGet("trade/{tradeId:guid}")]
    public async Task<IActionResult> GetTradeAsync(
        Guid tradeId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _tradeService.GetTradeAsync(
            schoolId.Value,
            tradeId
        );

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpPut("trade/{tradeId:guid}")]
    public async Task<IActionResult> UpdateTradeAsync(
        Guid tradeId,
        [FromBody] CreateTradeRequest req)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _tradeService.UpdateTradeAsync(
            schoolId.Value,
            tradeId,
            req
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Trade updated successfully.",
            data = result.Data
        });
    }

    [HttpDelete("trade/{tradeId:guid}")]
    public async Task<IActionResult> DeleteTradeAsync(
        Guid tradeId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _tradeService.DeleteTradeAsync(
            schoolId.Value,
            tradeId
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Trade deleted successfully."
        });
    }

    // ============================================================
    // ASSIGN SUBJECT TO CLASS
    // ============================================================

    [HttpPost("classes/subjects")]
    public async Task<IActionResult> AssignSubjectToClass(
        [FromBody] AssignClassSubjectRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _classSubjectService.AssignSubjectToClassAsync(
                schoolId.Value,
                request.ClassId,
                request.SubjectId);

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
    // GET SUBJECTS FOR CLASS
    // ============================================================

    [HttpGet("class/{classId:guid}")]
    public async Task<IActionResult> GetClassSubjects(
        Guid classId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var subjects =
            await _classSubjectService.GetClassSubjectsAsync(
                schoolId.Value,
                classId);

        return Ok(subjects);
    }


    // ============================================================
    // REMOVE SUBJECT FROM CLASS
    // ============================================================

    [HttpDelete("class/{classId:guid}/subject/{subjectId:guid}")]
    public async Task<IActionResult> RemoveSubjectFromClass(
        Guid classId,
        Guid subjectId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result =
            await _classSubjectService.RemoveSubjectFromClassAsync(
                schoolId.Value,
                classId,
                subjectId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Subject removed from class successfully."
        });
    }

    [HttpPut("students/{studentId}/trade")]
    public async Task<IActionResult> AssignTrade(
     Guid studentId,
     [FromBody] AssignStudentTradeRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _studentService.AssignTradeAsync(
            schoolId.Value,
            studentId,
            request.TradeId,
            request.TradeSubjectId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    [HttpDelete("students/{studentId}/trade")]
    public async Task<IActionResult> UnassignTrade(
        Guid studentId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _studentService.UnassignTradeAsync(
            schoolId.Value,
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


}