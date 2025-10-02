using FluentAssertions;
using ConsoleDungeon.Components;
using Xunit;

namespace ConsoleDungeon.Tests;

/// <summary>
/// Tests for core ECS components.
/// </summary>
public class CoreComponentsTests
{
    [Fact]
    public void Position_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var position = new Position();

        // Assert
        position.X.Should().Be(0);
        position.Y.Should().Be(0);
        position.Floor.Should().Be(0);
    }

    [Fact]
    public void Position_ParameterizedConstructor_SetsCorrectValues()
    {
        // Arrange & Act
        var position = new Position(10, 20, 3);

        // Assert
        position.X.Should().Be(10);
        position.Y.Should().Be(20);
        position.Floor.Should().Be(3);
    }

    [Fact]
    public void Position_ParameterizedConstructor_DefaultFloor_SetsFloorTo1()
    {
        // Arrange & Act
        var position = new Position(10, 20);

        // Assert
        position.X.Should().Be(10);
        position.Y.Should().Be(20);
        position.Floor.Should().Be(1);
    }

    [Fact]
    public void Stats_CanSetAllFields()
    {
        // Arrange & Act
        var stats = new Stats
        {
            MaxHP = 100,
            CurrentHP = 80,
            MaxMana = 50,
            CurrentMana = 30,
            Strength = 15,
            Dexterity = 12,
            Intelligence = 10,
            Defense = 8,
            Level = 5,
            Experience = 250
        };

        // Assert
        stats.MaxHP.Should().Be(100);
        stats.CurrentHP.Should().Be(80);
        stats.MaxMana.Should().Be(50);
        stats.CurrentMana.Should().Be(30);
        stats.Strength.Should().Be(15);
        stats.Dexterity.Should().Be(12);
        stats.Intelligence.Should().Be(10);
        stats.Defense.Should().Be(8);
        stats.Level.Should().Be(5);
        stats.Experience.Should().Be(250);
    }

    [Fact]
    public void Renderable_CanSetAllFields()
    {
        // Arrange & Act
        var renderable = new Renderable
        {
            Symbol = '@',
            ForegroundColor = ConsoleColor.White,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 2
        };

        // Assert
        renderable.Symbol.Should().Be('@');
        renderable.ForegroundColor.Should().Be(ConsoleColor.White);
        renderable.BackgroundColor.Should().Be(ConsoleColor.Black);
        renderable.RenderLayer.Should().Be(2);
    }

    [Fact]
    public void Components_AreValueTypes()
    {
        // Assert
        typeof(Position).IsValueType.Should().BeTrue("Position should be a struct");
        typeof(Stats).IsValueType.Should().BeTrue("Stats should be a struct");
        typeof(Renderable).IsValueType.Should().BeTrue("Renderable should be a struct");
    }
}
