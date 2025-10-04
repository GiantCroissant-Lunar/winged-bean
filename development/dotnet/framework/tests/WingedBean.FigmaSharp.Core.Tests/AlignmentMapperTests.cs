using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;

namespace WingedBean.FigmaSharp.Core.Tests;

public class AlignmentMapperTests
{
    [Fact]
    public void GetAlignment_HorizontalLayout_CenterCenter_ReturnsCorrectAlignment()
    {
        // Arrange
        var parent = new FObject
        {
            LayoutMode = LayoutMode.HORIZONTAL,
            PrimaryAxisAlignItems = PrimaryAxisAlignItem.CENTER,
            CounterAxisAlignItems = CounterAxisAlignItem.CENTER
        };

        var child = new FObject
        {
            Parent = parent
        };

        var mapper = new AlignmentMapper();

        // Act
        var result = mapper.GetAlignment(child);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HorizontalAlign.Center, result.Horizontal);
        Assert.Equal(VerticalAlign.Center, result.Vertical);
    }

    [Fact]
    public void GetAlignment_VerticalLayout_MaxMax_ReturnsCorrectAlignment()
    {
        // Arrange
        var parent = new FObject
        {
            LayoutMode = LayoutMode.VERTICAL,
            PrimaryAxisAlignItems = PrimaryAxisAlignItem.MAX,
            CounterAxisAlignItems = CounterAxisAlignItem.MAX
        };

        var child = new FObject
        {
            Parent = parent
        };

        var mapper = new AlignmentMapper();

        // Act
        var result = mapper.GetAlignment(child);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HorizontalAlign.Right, result.Horizontal);
        Assert.Equal(VerticalAlign.Bottom, result.Vertical);
    }

    [Fact]
    public void GetAlignment_NoParent_ReturnsNull()
    {
        // Arrange
        var child = new FObject
        {
            Parent = null
        };

        var mapper = new AlignmentMapper();

        // Act
        var result = mapper.GetAlignment(child);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAlignment_ParentNoLayout_ReturnsNull()
    {
        // Arrange
        var parent = new FObject
        {
            LayoutMode = LayoutMode.NONE
        };

        var child = new FObject
        {
            Parent = parent
        };

        var mapper = new AlignmentMapper();

        // Act
        var result = mapper.GetAlignment(child);

        // Assert
        Assert.Null(result);
    }
}
