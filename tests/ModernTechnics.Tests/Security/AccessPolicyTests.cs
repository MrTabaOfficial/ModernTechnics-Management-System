using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Security;

namespace ModernTechnics.Tests.Security;

public class AccessPolicyTests
{
    [Fact]
    public void Administrator_can_open_every_module()
    {
        Assert.All(Enum.GetValues<Module>(), module => Assert.True(AccessPolicy.CanAccess(Role.Administrator, module)));
    }

    [Theory]
    [InlineData(Role.Manager)]
    [InlineData(Role.WarehouseOperator)]
    [InlineData(Role.SalesAssociate)]
    public void Only_administrators_manage_user_accounts(Role role)
    {
        Assert.False(AccessPolicy.CanAccess(role, Module.Users));
    }

    [Fact]
    public void Warehouse_operator_sees_only_the_warehouse()
    {
        Assert.Equal([Module.Warehouse], AccessPolicy.ModulesFor(Role.WarehouseOperator));
    }

    [Theory]
    [InlineData(Module.Employees)]
    [InlineData(Module.Payroll)]
    [InlineData(Module.Warehouse)]
    public void Sales_associate_cannot_reach_back_office_modules(Module module)
    {
        Assert.False(AccessPolicy.CanAccess(Role.SalesAssociate, module));
    }

    [Fact]
    public void Every_role_has_at_least_one_module()
    {
        Assert.All(Enum.GetValues<Role>(), role => Assert.NotEmpty(AccessPolicy.ModulesFor(role)));
    }
}
