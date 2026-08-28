using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Driventa.API.Filters;

public class FormFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) ||
                        p.ParameterType == typeof(IFormFileCollection) ||
                        (p.ParameterType.IsArray && p.ParameterType.GetElementType() == typeof(IFormFile)))
            .ToList();

        if (formFileParameters.Count == 0)
            return;

        var properties = new Dictionary<string, OpenApiSchema>();

        foreach (var param in formFileParameters)
        {
            properties[param.Name!] = new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            };
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties
                    }
                }
            }
        };
    }
}
