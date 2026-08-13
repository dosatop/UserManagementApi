// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using UserManagementApi.DTOs.Auth.Roles;
// using UserManagementApi.DTOs.Users;
// using UserManagementApi.Services.Interfaces;

// namespace UserManagementApi.Controllers;

// [ApiController]
// [Route("api/users")]
// [Authorize(Roles = Roles.Admin)]
// public class UserManagementController(
//     IUserManagementService userManagementService) : ControllerBase
// {
//     private readonly IUserManagementService _userManagementService =
//         userManagementService;

//     [HttpPost]
//     public async Task<IActionResult> CreateUser(
//         [FromBody] CreateUserRequest request)
//     {
//         var result =
//             await _userManagementService.CreateUserAsync(
//                 request.FullName,
//                 request.Email,
//                 request.Password,
//                 request.Role);

//         if (!result.Success)
//         {
//             return BadRequest(new
//             {
//                 message = result.Error
//             });
//         }

//         return StatusCode(
//             StatusCodes.Status201Created,
//             new
//             {
//                 message = "User created successfully.",
//                 user = new
//                 {
//                     result.User!.Id,
//                     result.User.FullName,
//                     result.User.Email,
//                     result.User.UserName
//                 }
//             });
//     }
// }