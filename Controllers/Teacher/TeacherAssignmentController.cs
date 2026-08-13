// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using UserManagementApi.DTOs.Auth.Roles;
// using UserManagementApi.Services.Interfaces;

// namespace UserManagementApi.Controllers;

// [ApiController]
// [Route("api/schools/{schoolId:guid}/teachers/{teacherId:guid}")]
// [Authorize(Roles = Roles.Admin)]
// public class TeacherAssignmentsController : ControllerBase
// {
//     private readonly ITeacherAssignmentService _service;

//     public TeacherAssignmentsController(
//         ITeacherAssignmentService service)
//     {
//         _service = service;
//     }

//     [HttpPost("classes/{classId:guid}")]
//     public async Task<IActionResult> AssignClass(
//         Guid schoolId,
//         Guid teacherId,
//         Guid classId)
//     {
//         var result = await _service.AssignClassAsync(
//             schoolId,
//             teacherId,
//             classId);

//         if (!result.Success)
//         {
//             return BadRequest(new
//             {
//                 message = result.Error
//             });
//         }

//         return Ok(new
//         {
//             message = "Class assigned successfully.",
//             assignment = result.Data
//         });
//     }

//     [HttpDelete("classes/{classId:guid}")]
//     public async Task<IActionResult> RemoveClass(
//         Guid schoolId,
//         Guid teacherId,
//         Guid classId)
//     {
//         var result = await _service.RemoveClassAsync(
//             schoolId,
//             teacherId,
//             classId);

//         if (!result.Success)
//         {
//             return NotFound(new
//             {
//                 message = result.Error
//             });
//         }

//         return Ok(new
//         {
//             message = "Class assignment removed successfully."
//         });
//     }

//     [HttpPost("subjects/{subjectId:guid}")]
//     public async Task<IActionResult> AssignSubject(
//         Guid schoolId,
//         Guid teacherId,
//         Guid subjectId)
//     {
//         var result = await _service.AssignSubjectAsync(
//             schoolId,
//             teacherId,
//             subjectId);

//         if (!result.Success)
//         {
//             return BadRequest(new
//             {
//                 message = result.Error
//             });
//         }

//         return Ok(new
//         {
//             message = "Subject assigned successfully.",
//             assignment = result.Data
//         });
//     }

//     [HttpDelete("subjects/{subjectId:guid}")]
//     public async Task<IActionResult> RemoveSubject(
//         Guid schoolId,
//         Guid teacherId,
//         Guid subjectId)
//     {
//         var result = await _service.RemoveSubjectAsync(
//             schoolId,
//             teacherId,
//             subjectId);

//         if (!result.Success)
//         {
//             return NotFound(new
//             {
//                 message = result.Error
//             });
//         }

//         return Ok(new
//         {
//             message = "Subject assignment removed successfully."
//         });
//     }
// }