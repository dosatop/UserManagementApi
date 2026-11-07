using System.ComponentModel.DataAnnotations;

namespace UserManagementApi.DTOs.SuperAdmin;

// ====================================================================
// SUPER ADMIN — REQUEST DTOs
// ====================================================================

public class SuperAdminCreateTeacherRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class SuperAdminCreateStudentRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// Leave empty to auto-generate one (e.g. STU-2026-48213).
    [MaxLength(50)]
    public string? StudentNumber { get; set; }

    [Required]
    public Guid ClassId { get; set; }

    public Guid? DepartmentId { get; set; }

    /// Optional.
    public Guid? TradeId { get; set; }

    /// Optional. Must belong to TradeId.
    public Guid? TradeSubjectId { get; set; }
}

public class SuperAdminCreateClassRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// School level (same value the admin create-class page sends).
    public int? Level { get; set; }
}
