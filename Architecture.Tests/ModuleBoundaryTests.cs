using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Enforces the modular-monolith rule: a module's Domain/Application/Infrastructure/Presentation
/// layers may never reference another module's Domain/Application/Infrastructure/Presentation —
/// the only sanctioned crossing point is another module's *.Contracts assembly (MediatR
/// requests/DTOs). Contracts assemblies themselves must stay dependency-free of other modules
/// entirely, including their Contracts, so they remain safe to reference from anywhere.
/// </summary>
public class ModuleBoundaryTests
{
    private sealed record ModuleAssemblies(
        string Name,
        Assembly Domain,
        Assembly Application,
        Assembly Contracts,
        Assembly Infrastructure,
        Assembly Presentation);

    private static readonly ModuleAssemblies[] Modules =
    [
        new ModuleAssemblies(
            "Users",
            typeof(Users.Domain.Vehicle).Assembly,
            typeof(Users.Application.Services.IVehicleService).Assembly,
            typeof(Users.Contracts.IUsersUnitOfWork).Assembly,
            typeof(Users.Infrastructure.UsersDbContext).Assembly,
            typeof(Users.Presentation.ApiControllers.VehiclesController).Assembly),
        new ModuleAssemblies(
            "Workshops",
            typeof(Workshops.Domain.Workshop).Assembly,
            typeof(Workshops.Application.Services.IMechanicService).Assembly,
            typeof(Workshops.Contracts.IWorkshopsUnitOfWork).Assembly,
            typeof(Workshops.Infrastructure.WorkshopsDbContext).Assembly,
            typeof(Workshops.Presentation.ApiControllers.MechanicsController).Assembly),
        new ModuleAssemblies(
            "Orders",
            typeof(Orders.Domain.ServiceOrder).Assembly,
            typeof(Orders.Application.Services.IServiceOrderService).Assembly,
            typeof(Orders.Contracts.IOrdersUnitOfWork).Assembly,
            typeof(Orders.Infrastructure.OrdersDbContext).Assembly,
            typeof(Orders.Presentation.ApiControllers.PaymentsController).Assembly),
    ];

    /// <summary>Every non-Contracts (layer, assembly) pair across all modules, for the Theory below.</summary>
    public static IEnumerable<object[]> NonContractsLayers()
    {
        foreach (var m in Modules)
        {
            yield return new object[] { m.Name, "Domain", m.Domain };
            yield return new object[] { m.Name, "Application", m.Application };
            yield return new object[] { m.Name, "Infrastructure", m.Infrastructure };
            yield return new object[] { m.Name, "Presentation", m.Presentation };
        }
    }

    /// <summary>Every (module, Contracts assembly) pair, for the Theory below.</summary>
    public static IEnumerable<object[]> ContractsLayers()
    {
        foreach (var m in Modules)
            yield return new object[] { m.Name, m.Contracts };
    }

    [Theory]
    [MemberData(nameof(NonContractsLayers))]
    public void Non_contracts_layer_must_not_depend_on_another_modules_non_contracts_layer(
        string moduleName, string layerName, Assembly assembly)
    {
        var forbiddenNamespaces = Modules
            .Where(m => m.Name != moduleName)
            .SelectMany(m => new[]
            {
                $"{m.Name}.Domain",
                $"{m.Name}.Application",
                $"{m.Name}.Infrastructure",
                $"{m.Name}.Presentation",
            })
            .ToArray();

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"{moduleName}.{layerName} has a forbidden cross-module dependency. " +
            $"Offending types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Theory]
    [MemberData(nameof(ContractsLayers))]
    public void Contracts_layer_must_not_depend_on_any_other_module_at_all(string moduleName, Assembly assembly)
    {
        var forbiddenNamespaces = Modules
            .Where(m => m.Name != moduleName)
            .SelectMany(m => new[]
            {
                $"{m.Name}.Domain",
                $"{m.Name}.Application",
                $"{m.Name}.Contracts",
                $"{m.Name}.Infrastructure",
                $"{m.Name}.Presentation",
            })
            .ToArray();

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"{moduleName}.Contracts has a forbidden dependency on another module. " +
            $"Offending types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
