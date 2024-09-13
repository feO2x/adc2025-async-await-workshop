using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace WebApi.CommonValidation;

public static class ValidationExtensions
{
    public static bool CheckForErrors<TValidator, TDto>(
        this TValidator validator,
        TDto dto,
        [NotNullWhen(true)] out IDictionary<string, string[]>? errors
    )
        where TValidator : IValidator<TDto>
    {
        var validationResult = validator.Validate(dto);
        if (validationResult.IsValid)
        {
            errors = null;
            return false;
        }

        errors = validationResult.ToDictionary();
        return true;
    }
}