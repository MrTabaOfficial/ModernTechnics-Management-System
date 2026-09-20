using ModernTechnics.Core.Domain;

namespace ModernTechnics.Core.Security;

/// <summary>The screens of the application; each one is guarded by <see cref="AccessPolicy"/>.</summary>
public enum Module
{
    Dashboard,
    Employees,
    Payroll,
    Customers,
    Applications,
    Warehouse,
    Store,
    Orders,
    Users,
}

/// <summary>Single source of truth for which role may open which module.</summary>
public static class AccessPolicy
{
    private static readonly Dictionary<Role, Module[]> Grants = new()
    {
        [Role.Administrator] = Enum.GetValues<Module>(),
        [Role.Manager] =
        [
            Module.Dashboard, Module.Employees, Module.Payroll, Module.Customers,
            Module.Applications, Module.Warehouse, Module.Store, Module.Orders,
        ],
        [Role.WarehouseOperator] = [Module.Warehouse],
        [Role.SalesAssociate] = [Module.Store, Module.Orders, Module.Customers],
    };

    public static IReadOnlyList<Module> ModulesFor(Role role) =>
        Grants.TryGetValue(role, out var modules) ? modules : [];

    public static bool CanAccess(Role role, Module module) => ModulesFor(role).Contains(module);
}
