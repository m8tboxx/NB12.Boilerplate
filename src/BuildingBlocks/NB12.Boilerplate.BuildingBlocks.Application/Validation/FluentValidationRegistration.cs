using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NB12.Boilerplate.BuildingBlocks.Application.Validation
{
    public static class FluentValidationRegistration
    {
        public static IServiceCollection AddValidatorsFromAssemblies(
            this IServiceCollection services,
            IEnumerable<Assembly> assemblies)
        {
            var openGeneric = typeof(IValidator<>);

            foreach (var asm in assemblies.Distinct())
            {
                foreach (var type in asm.DefinedTypes)
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    var validatorInterfaces = type.ImplementedInterfaces
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric)
                        .ToList();

                    foreach (var vi in validatorInterfaces)
                    {
                        services.AddTransient(vi, type.AsType());
                    }
                }
            }

            return services;
        }
    }
}
