using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRock.Api;

public static class ValidationErrorResponseHelper
{
    public static ProblemDetails CreateValidationProblemDetails(this ValidationResult failedValidationResult, HttpContext ctx)
    {        
        var problemDetails = new ProblemDetails
        {
            Title = "Validation error",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = ctx.Request.Path,

        };
        foreach (var (key, value) in failedValidationResult.ToDictionary())
        {
            problemDetails.Extensions.Add(key, string.Join('|', value));
        }
        return problemDetails;
    }
}
