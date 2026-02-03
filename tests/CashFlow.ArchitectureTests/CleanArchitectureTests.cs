using CashFlow.Consolidation.Application;
using CashFlow.Transactions.Application;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace CashFlow.ArchitectureTests;

/// <summary>
/// Architecture tests to enforce Clean Architecture principles
/// Ensures proper layer dependencies and separation of concerns
/// </summary>
public class CleanArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotHaveDependencyOnOtherLayers()
    {
        // Arrange
        var domainAssemblies = new[]
        {
            typeof(Transactions.Domain.Entities.Transaction).Assembly,
            typeof(Consolidation.Domain.Entities.DailyConsolidation).Assembly
        };

        // Act & Assert
        foreach (var assembly in domainAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAll(
                    "Application",
                    "Infrastructure",
                    "API")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Domain layer should not depend on other layers in {assembly.GetName().Name}");
        }
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        // Arrange
        var applicationAssemblies = new[]
        {
            typeof(CashFlow.Transactions.Application.DependencyInjection).Assembly,
            typeof(CashFlow.Consolidation.Application.DependencyInjection).Assembly
        };

        // Act & Assert
        foreach (var assembly in applicationAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAll(
                    "Infrastructure",
                    "API")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Application layer should not depend on Infrastructure or API in {assembly.GetName().Name}");
        }
    }

    [Fact]
    public void Controllers_ShouldHaveSuffix()
    {
        // Arrange
        var apiAssembly = typeof(Transactions.API.Controllers.TransactionsController).Assembly;

        // Act
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("Controllers")
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"All controllers should end with 'Controller' suffix in {apiAssembly.GetName().Name}");
    }

    [Fact]
    public void Handlers_ShouldHaveSuffix()
    {
        // Arrange
        var applicationAssemblies = new[]
        {
            typeof(CashFlow.Transactions.Application.DependencyInjection).Assembly,
            typeof(CashFlow.Consolidation.Application.DependencyInjection).Assembly
        };

        // Act & Assert
        foreach (var assembly in applicationAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
                .Should()
                .HaveNameEndingWith("Handler")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"All handlers should end with 'Handler' suffix in {assembly.GetName().Name}");
        }
    }

    [Fact]
    public void Repositories_ShouldBeInInfrastructureLayer()
    {
        // Arrange
        var infrastructureAssembly = typeof(Transactions.Infrastructure.DependencyInjection).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceContaining("Infrastructure.Repositories")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"All repository implementations should be in Infrastructure.Repositories namespace in {infrastructureAssembly.GetName().Name}");
    }

    [Fact]
    public void Entities_ShouldBeSealed_OrAbstract()
    {
        // Arrange
        var domainAssemblies = new[]
        {
            typeof(Transactions.Domain.Entities.Transaction).Assembly
        };

        // Act & Assert
        foreach (var assembly in domainAssemblies)
        {
            var types = Types.InAssembly(assembly)
                .That()
                .ResideInNamespace("Entities")
                .GetTypes();

            foreach (var type in types)
            {
                (type.IsSealed || type.IsAbstract).Should().BeTrue(
                    $"Entity {type.Name} should be sealed or abstract to prevent inheritance issues");
            }
        }
    }
}
