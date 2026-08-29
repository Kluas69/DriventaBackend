namespace Driventa.Application.DTOs.Roles;

public class UpdateRolePermissionsRequest
{
    public List<Guid> PermissionIds { get; set; } = new();
}
