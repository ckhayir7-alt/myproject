using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WRMS.Web.Extensions;

public static class ModelStateExtensions
{
    public static void AddValidationResult(this ModelStateDictionary modelState, ValidationResult result)
    {
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
