using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;

namespace WingedBean.FigmaSharp.Core.Tests;

public class SizeModeCalculatorTests
{
    [Fact]
    public void DetermineSizeMode_LayoutGrow_ReturnsFill()
    {
        // Arrange
        var figma = new FObject
        {
            LayoutGrow = 1
        };

        var calculator = new SizeModeCalculator();

        // Act
        var result = calculator.DetermineSizeMode(figma, isWidth: true);

        // Assert
        Assert.Equal(SizeMode.Fill, result);
    }

    [Fact]
    public void DetermineSizeMode_LayoutAlignStretch_CrossAxis_ReturnsFill()
    {
        // Arrange
        var parent = new FObject
        {
            LayoutMode = LayoutMode.HORIZONTAL
        };

        var child = new FObject
        {
            Parent = parent,
            LayoutAlign = LayoutAlign.STRETCH
        };

        var calculator = new SizeModeCalculator();

        // Act - Height is cross-axis for horizontal layout
        var result = calculator.DetermineSizeMode(child, isWidth: false);

        // Assert
        Assert.Equal(SizeMode.Fill, result);
    }

    [Fact]
    public void DetermineSizeMode_ParentAutoSizing_ReturnsAuto()
    {
        // Arrange
        var parent = new FObject
        {
            PrimaryAxisSizingMode = PrimaryAxisSizingMode.AUTO
        };

        var child = new FObject
        {
            Parent = parent
        };

        var calculator = new SizeModeCalculator();

        // Act
        var result = calculator.DetermineSizeMode(child, isWidth: true);

        // Assert
        Assert.Equal(SizeMode.Auto, result);
    }

    [Fact]
    public void DetermineSizeMode_NoSpecialCases_ReturnsFixed()
    {
        // Arrange
        var figma = new FObject();

        var calculator = new SizeModeCalculator();

        // Act
        var result = calculator.DetermineSizeMode(figma, isWidth: true);

        // Assert
        Assert.Equal(SizeMode.Fixed, result);
    }
}
