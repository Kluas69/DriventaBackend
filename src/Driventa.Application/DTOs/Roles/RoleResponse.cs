namespace Driventa.Application.DTOs.Roles;

public class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PermissionResponse> Permissions { get; set; } = new();
}

public class PermissionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
