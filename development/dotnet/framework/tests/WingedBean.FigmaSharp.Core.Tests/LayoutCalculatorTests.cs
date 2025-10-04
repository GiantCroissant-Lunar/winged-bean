using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;

namespace WingedBean.FigmaSharp.Core.Tests;

public class LayoutCalculatorTests
{
    [Fact]
    public void BuildLayoutData_AbsolutePosition_SetsCorrectPositionMode()
    {
        // Arrange
        var figma = new FObject
        {
            LayoutPositioning = LayoutPositioning.ABSOLUTE,
            AbsoluteBoundingBox = new BoundingBox(100, 200, 300, 400)
        };

        var calculator = new LayoutCalculator();

        // Act
        var result = calculator.BuildLayoutData(figma);

        // Assert
        Assert.Equal(PositionMode.Absolute, result.PositionMode);
        Assert.Equal(100, result.AbsolutePosition.X);
        Assert.Equal(200, result.AbsolutePosition.Y);
        Assert.Equal(300, result.FixedWidth);
        Assert.Equal(400, result.FixedHeight);
    }

    [Fact]
    public void BuildLayoutData_AutoLayoutChild_SetsAutoLayoutPositionMode()
    {
        // Arrange
        var parent = new FObject
        {
            LayoutMode = LayoutMode.HORIZONTAL
        };

        var child = new FObject
        {
            Parent = parent,
            LayoutPositioning = LayoutPositioning.AUTO,
            AbsoluteBoundingBox = new BoundingBox(0, 0, 50, 50)
        };

        var calculator = new LayoutCalculator();

        // Act
        var result = calculator.BuildLayoutData(child);

        // Assert
        Assert.Equal(PositionMode.AutoLayout, result.PositionMode);
    }

    [Fact]
    public void BuildLayoutData_WithAutoLayout_CreatesAutoLayoutData()
    {
        // Arrange
        var figma = new FObject
        {
            LayoutMode = LayoutMode.VERTICAL,
            ItemSpacing = 15,
            PrimaryAxisAlignItems = PrimaryAxisAlignItem.CENTER,
            AbsoluteBoundingBox = new BoundingBox(0, 0, 100, 200)
        };

        var calculator = new LayoutCalculator();

        // Act
        var result = calculator.BuildLayoutData(figma);

        // Assert
        Assert.NotNull(result.AutoLayout);
        Assert.Equal(LayoutDirection.Vertical, result.AutoLayout.Direction);
        Assert.Equal(15, result.AutoLayout.Spacing);
    }

    [Fact]
    public void BuildLayoutData_WithPadding_CalculatesPadding()
    {
        // Arrange
        var figma = new FObject
        {
            PaddingLeft = 10,
            PaddingRight = 20,
            PaddingTop = 30,
            PaddingBottom = 40,
            AbsoluteBoundingBox = new BoundingBox(0, 0, 200, 200),
            Children = new List<FObject>()
        };

        var calculator = new LayoutCalculator();

        // Act
        var result = calculator.BuildLayoutData(figma);

        // Assert
        Assert.Equal(10, result.Padding.Left);
        Assert.Equal(20, result.Padding.Right);
        Assert.Equal(30, result.Padding.Top);
        Assert.Equal(40, result.Padding.Bottom);
    }
}
