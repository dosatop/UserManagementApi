using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Models;
using UserManagementApi.Models.AuthModels;

namespace UserManagementApi.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<User>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
}