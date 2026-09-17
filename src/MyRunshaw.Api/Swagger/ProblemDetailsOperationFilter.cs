using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyRunshaw.Api.Swagger;

/// <summary>
/// Documents the response produced when an exception escapes a controller.
/// </summary>
public sealed class ProblemDetailsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses ??= [];

        if (operation.Responses.ContainsKey("500"))
            return;

        var schema = context.SchemaGenerator.GenerateSchema(
            typeof(ProblemDetails),
            context.SchemaRepository);

        operation.Responses["500"] = new OpenApiResponse
        {
            Description = "Unexpected server error",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType
                {
                    Schema = schema
                }
            }
        };
    }
}
