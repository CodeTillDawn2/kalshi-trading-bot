using System.Reflection;

namespace BacklashBot.Shared.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }

    [Fact]
    public void HandshakeMethodSignaturesMatch()
    {
        // Get the interface method
        var interfaceType = typeof(OverseerBotShared.IOverseerHub);
        var interfaceMethod = interfaceType.GetMethod("Handshake");

        Assert.NotNull(interfaceMethod);

        // Get the implementation method
        var implementationType = typeof(BacklashOverseer.OverseerHub);
        var implementationMethod = implementationType.GetMethod("Handshake", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(implementationMethod);

        // Verify return types match
        Assert.Equal(interfaceMethod.ReturnType, implementationMethod.ReturnType);

        // Verify parameter counts match
        var interfaceParams = interfaceMethod.GetParameters();
        var implementationParams = implementationMethod.GetParameters();

        Assert.Equal(interfaceParams.Length, implementationParams.Length);

        // Verify each parameter matches
        for (int i = 0; i < interfaceParams.Length; i++)
        {
            var interfaceParam = interfaceParams[i];
            var implementationParam = implementationParams[i];

            Assert.Equal(interfaceParam.ParameterType, implementationParam.ParameterType);
            Assert.Equal(interfaceParam.Name, implementationParam.Name);
            Assert.Equal(interfaceParam.IsOptional, implementationParam.IsOptional);
        }
    }
}
