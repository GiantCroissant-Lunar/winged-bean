using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;

namespace WingedBean.FigmaSharp.Core.Tests;

public class AutoLayoutTransformerTests
{
    [Fact]
    public void BuildAutoLayoutData_HorizontalLayout_ReturnsCorrectDirection()
    {
        // Arrange
        var figma = new FObject
        {
            LayoutMode = LayoutMode.HORIZONTAL,
            ItemSpacing = 10,
            PrimaryAxisAlignItems = PrimaryAxisAlignItem.CENTER,
            CounterAxisAlignItems = CounterAxisAlignItem.MIN
        };

        var transformer = new AutoLayoutTransformer();

        // Act
        var result = transformer.BuildAutoLayoutData(figma);

        // Assert
        Assert.Equal(LayoutDirection.Horizontal, result.Direction);
        Assert.Equal(10, result.Spacing);
        Assert.Equal(PrimaryAxisAlign.Center, result.PrimaryAlign);
        Assert.Equal(CrossAxisAlign.Start, result.CrossAlign);
    }

    [Fact]
    public void BuildAutoLayoutData_VerticalLayout_ReturnsCorrectDirection()
    {
        // Arrange
        var figma = new FObject
        {
            LayoutMode = LayoutMode.VERTICAL,
            ItemSpacing = 20,
            PrimaryAxisAlignItems = PrimaryAxisAlignItem.MAX,
            CounterAxisAlignItems = CounterAxisAlignItem.CENTER
        };

        var transformer = new AutoLayoutTransformer();

        // Act
        var result = transformer.BuildAutoLayoutData(figma);

        // Assert
        Assert.Equal(LayoutDirection.Vertical, result.Direction);
        Assert.Equal(20, result.Spacing);
        Assert.Equal(PrimaryAxisAlign.End, result.PrimaryAlign);
        Assert.Equal(CrossAxisAlign.Center, result.CrossAlign);
    }

    [Fact]
    public void BuildAutoLayoutData_SpaceBetween_CalculatesCorrectSpacing()
    {
        // Arrange
        var figma = new FObject
        {
            LayoutMode = LayoutMode.HORIZONTAL,
            PrimaryAxisAlignItems = PrimaryAxisAlignItem.SPACE_BETWEEN,
            Size = new Vector2(1000, 100),
            PaddingLeft = 0,
            PaddingRight = 0,
            Children = new List<FObject>
            {
                new FObject { Size = new Vector2(200, 100) },
                new FObject { Size = new Vector2(200, 100) },
                new FObject { Size = new Vector2(200, 100) }
            }
        };

        var transformer = new AutoLayoutTransformer();

        // Act
        var result = transformer.BuildAutoLayoutData(figma);

        // Assert
        // (1000 - 600) / 2 = 200
        Assert.Equal(200, result.Spacing);
        Assert.Equal(PrimaryAxisAlign.SpaceBetween, result.PrimaryAlign);
    }

    [Fact]
    public void BuildAutoLayoutData_WithPadding_IncludesPaddingInResult()
    {
        // Arrange
        var figma = new FObject
        {
            LayoutMode = LayoutMode.VERTICAL,
            PaddingLeft = 10,
            PaddingRight = 15,
            PaddingTop = 20,
            PaddingBottom = 25
        };

        var transformer = new AutoLayoutTransformer();

        // Act
        var result = transformer.BuildAutoLayoutData(figma);

        // Assert
        Assert.NotNull(result.Padding);
        Assert.Equal(10, result.Padding.Value.Left);
        Assert.Equal(15, result.Padding.Value.Right);
        Assert.Equal(20, result.Padding.Value.Top);
        Assert.Equal(25, result.Padding.Value.Bottom);
    }

    [Fact]
    public void BuildAutoLayoutData_WrapEnabled_SetsWrapFlag()
    {
        // Arrange
        var figma = new FObject
        {
            LayoutMode = LayoutMode.HORIZONTAL,
            LayoutWrap = LayoutWrap.WRAP
        };

        var transformer = new AutoLayoutTransformer();

        // Act
        var result = transformer.BuildAutoLayoutData(figma);

        // Assert
        Assert.True(result.WrapEnabled);
    }
}
