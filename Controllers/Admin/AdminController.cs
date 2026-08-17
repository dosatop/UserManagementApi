using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Classes;
using UserManagementApi.DTOs.Parents;
using UserManagementApi.DTOs.Students;
using UserManagementApi.DTOs.Subjects;
using UserManagementApi.DTOs.Teachers;
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
public class AdminController(IAdminService adminService, ITeacherService teacherService, IStudentService studentService, IClassService classService, ISubjectService subjectService, IParentService parentService) : SchoolAdminControllerBase
{
    private readonly IAdminService _adminService = adminService;
    private readonly ITeacherService _teacherService = teacherService;
    private readonly IStudentService _studentService = studentService;
    private readonly IClassService _classService = classService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IParentService _parentService = parentService;

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

}