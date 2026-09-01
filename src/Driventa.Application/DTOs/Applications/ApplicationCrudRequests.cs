using System.Text.Json;
using System.Text.Json.Serialization;
using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Applications;

public class CreateApplicationRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }
    public int TruckCount { get; set; }
    public string? McNumber { get; set; }
    public string? DotNumber { get; set; }
    public string? PreferredLanes { get; set; }
    public string? AdditionalDetails { get; set; }
}

public class UpdateApplicationRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public int? TruckCount { get; set; }
    public string? McNumber { get; set; }
    public string? DotNumber { get; set; }
    public string? PreferredLanes { get; set; }
    public string? AdditionalDetails { get; set; }
    public ApplicationStatus? Status { get; set; }
}

public class AssignApplicationRequest
{
    public Guid UserId { get; set; }
}

public class ApplicationNoteRequest
{
    public string Content { get; set; } = string.Empty;
}

public class ConvertToCarrierRequest
{
    [JsonConverter(typeof(NullableGuidJsonConverter))]
    public Guid? AssignedDispatcherId { get; set; }
    public string? Notes { get; set; }
}

public sealed class NullableGuidJsonConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (Guid.TryParse(value, out var guid))
                return guid;
        }

        throw new JsonException("assignedDispatcherId must be a valid GUID or null.");
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}