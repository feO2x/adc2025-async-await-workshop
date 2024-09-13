using Microsoft.Extensions.DependencyInjection;

namespace WebApi.CommonValidation;

public static class CommonValidationModule
{
    public static IServiceCollection AddCommonValidation(this IServiceCollection services) =>
        services.AddSingleton<PagingParametersValidator>();
}