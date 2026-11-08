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

// ====================================================================
// UPDATE REQUESTS
// ====================================================================
// Send every field (the form is pre-filled). Nullable placement fields
// (department, trade, trade subject) set to null are CLEARED.
// NewPassword is optional — leave empty to keep the current password.

public class SuperAdminUpdateTeacherRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [MinLength(6)]
    public string? NewPassword { get; set; }
}

public class SuperAdminUpdateStudentRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [MinLength(6)]
    public string? NewPassword { get; set; }

    [Required, MaxLength(50)]
    public string StudentNumber { get; set; } = string.Empty;

    [Required]
    public Guid ClassId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? TradeId { get; set; }

    public Guid? TradeSubjectId { get; set; }
}

public class SuperAdminUpdateClassRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

// ====================================================================
// SUBJECTS
// ====================================================================
// Create / update reuse your existing CreateSubjectRequest
// (UserManagementApi.DTOs.Subjects).

public class SuperAdminAssignSubjectRequest
{
    [Required, MinLength(1)]
    public List<Guid> ClassIds { get; set; } = [];

    /// Optional — also make this teacher teach it in those classes.
    public Guid? TeacherId { get; set; }
}