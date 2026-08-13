using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Users;
using UserManagementApi.Models;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchoolsController(
    ApplicationDbContext context,
    UserManager<User> userManager) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    // Create a school
    // Ideally this should eventually be SuperAdmin only.
    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpPost]
    public async Task<IActionResult> CreateSchool(
        CreateSchoolRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("School name is required.");
        }

        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Address = request.Address,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        _context.Schools.Add(school);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "School created successfully.",
            school = new
            {
                school.Id,
                school.Name,
                school.Address,
                school.Email,
                school.PhoneNumber
            }
        });
    }

    // Get schools
    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpGet]
    public async Task<IActionResult> GetSchools()
    {
        var schools = await _context.Schools
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(schools);
    }

    // Get one school
    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpGet("{schoolId:guid}")]
    public async Task<IActionResult> GetSchool(Guid schoolId)
    {
        var school = await _context.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == schoolId);

        if (school == null)
        {
            return NotFound("School not found.");
        }

        return Ok(school);
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpPost("{schoolId:guid}/admin")]
    public async Task<IActionResult> CreateSchoolAdmin(
    Guid schoolId,
    CreateSchoolAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new
            {
                message = "Full name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Password is required."
            });
        }

        // Check school
        var school = await _context.Schools
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return NotFound(new
            {
                message = "School not found."
            });
        }

        // Check if email already exists
        var existingUser =
            await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return BadRequest(new
            {
                message = "A user with this email already exists."
            });
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            SchoolId = schoolId,
            EmailConfirmed = false
        };

        var createResult =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = createResult.Errors
                    .Select(x => x.Description)
            });
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                Roles.Admin);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return BadRequest(new
            {
                errors = roleResult.Errors
                    .Select(x => x.Description)
            });
        }

        return Ok(new
        {
            message = "School admin created successfully.",
            admin = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.SchoolId,
                Role = Roles.Admin
            }
        });
    }

    // Get school administrators
    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpGet("{schoolId:guid}/admins")]
    public async Task<IActionResult> GetSchoolAdmins(Guid schoolId)
    {
        // Check if school exists
        var school = await _context.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return NotFound(new
            {
                message = "School not found."
            });
        }

        // Get all users belonging to this school
        var users = await _userManager.Users
            .Where(x => x.SchoolId == schoolId)
            .ToListAsync();

        var admins = new List<object>();

        foreach (var user in users)
        {
            // Check user's roles
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains(Roles.Admin))
            {
                admins.Add(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.SchoolId,
                    Role = Roles.Admin,
                    user.EmailConfirmed
                });
            }
        }

        return Ok(admins);
    }
    
    [Authorize(Roles = Roles.SuperAdmin)]
    [Authorize]
    [HttpGet("debug-auth")]
    public IActionResult DebugAuth()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            AuthenticationType = User.Identity?.AuthenticationType,
            Name = User.Identity?.Name,

            IsSuperAdmin = User.IsInRole(Roles.SuperAdmin),

            Roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList(),

            Claims = User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            })
        });
    }
}
