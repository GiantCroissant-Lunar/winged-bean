using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;

namespace WingedBean.FigmaSharp.Core.Tests;

public class FigmaTransformerTests
{
    [Fact]
    public void Transform_SimpleFrame_CreatesContainer()
    {
        // Arrange
        var figma = new FObject
        {
            Id = "1:1",
            Name = "TestFrame",
            Type = NodeType.FRAME,
            Size = new Vector2(100, 100),
            AbsoluteBoundingBox = new BoundingBox(0, 0, 100, 100)
        };

        var transformer = new FigmaTransformer();

        // Act
        var result = transformer.Transform(figma);

        // Assert
        Assert.Equal("1:1", result.Id);
        Assert.Equal("TestFrame", result.Name);
        Assert.Equal(UIElementType.Container, result.Type);
        Assert.Equal(100, result.Layout.FixedWidth);
        Assert.Equal(100, result.Layout.FixedHeight);
    }

    [Fact]
    public void Transform_TextNode_CreatesTextElement()
    {
        // Arrange
        var figma = new FObject
        {
            Id = "2:1",
            Name = "HelloText",
            Type = NodeType.TEXT,
            Characters = "Hello World",
            Size = new Vector2(200, 50),
            AbsoluteBoundingBox = new BoundingBox(10, 10, 200, 50)
        };

        var transformer = new FigmaTransformer();

        // Act
        var result = transformer.Transform(figma);

        // Assert
        Assert.Equal(UIElementType.Text, result.Type);
        Assert.Equal("Hello World", result.Style.Text?.Content);
    }

    [Fact]
    public void Transform_WithChildren_TransformsRecursively()
    {
        // Arrange
        var child1 = new FObject
        {
            Id = "child1",
            Name = "Child1",
            Type = NodeType.FRAME,
            Size = new Vector2(50, 50),
            AbsoluteBoundingBox = new BoundingBox(0, 0, 50, 50)
        };

        var child2 = new FObject
        {
            Id = "child2",
            Name = "Child2",
            Type = NodeType.TEXT,
            Size = new Vector2(50, 20),
            AbsoluteBoundingBox = new BoundingBox(0, 60, 50, 20)
        };

        var parent = new FObject
        {
            Id = "parent",
            Name = "Parent",
            Type = NodeType.FRAME,
            Size = new Vector2(100, 100),
            AbsoluteBoundingBox = new BoundingBox(0, 0, 100, 100),
            Children = new List<FObject> { child1, child2 }
        };

        var transformer = new FigmaTransformer();

        // Act
        var result = transformer.Transform(parent);

        // Assert
        Assert.Equal(2, result.Children.Count);
        Assert.Equal("child1", result.Children[0].Id);
        Assert.Equal("child2", result.Children[1].Id);
    }

    [Fact]
    public void Transform_ButtonPattern_CreatesButton()
    {
        // Arrange
        var figma = new FObject
        {
            Id = "3:1",
            Name = "SubmitButton",
            Type = NodeType.FRAME,
            Size = new Vector2(120, 40),
            AbsoluteBoundingBox = new BoundingBox(0, 0, 120, 40)
        };

        var transformer = new FigmaTransformer();

        // Act
        var result = transformer.Transform(figma);

        // Assert
        Assert.Equal(UIElementType.Button, result.Type);
    }
}
