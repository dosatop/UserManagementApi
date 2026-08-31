using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.DTOs.Classes;

public class CreateClassRequest
{
    public string Name { get; set; } = string.Empty;
    public SchoolLevel Level { get; set; }

}