using Terminal.Gui;
using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;
using WingedBean.Plugins.FigmaSharp.TerminalGui;
using FigmaColor = WingedBean.Contracts.FigmaSharp.Color;

namespace WingedBean.FigmaSharp.Demo;

class Program
{
    static void Main(string[] args)
    {
        // Create a stub Figma design
        var figmaDesign = CreateStubFigmaDesign();
        
        // Transform Figma to abstract UI
        var transformer = new FigmaTransformer();
        var abstractUI = transformer.Transform(figmaDesign);
        
        // Render to Terminal.Gui
        var renderer = new TerminalGuiRenderer();
        var pipeline = new FigmaToUIPipeline(transformer, renderer);
        var terminalGuiView = (View)pipeline.Convert(figmaDesign);
        
        // Run Terminal.Gui application
        Application.Init();
        
        var top = new Window()
        {
            Title = "FigmaSharp Demo - Figma to Terminal.Gui",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        
        top.Add(terminalGuiView);
        
        Application.Run(top);
        Application.Shutdown();
    }
    
    /// <summary>
    /// Create a stub Figma design for testing
    /// Simulates a simple UI with a title, button, and text
    /// </summary>
    static FObject CreateStubFigmaDesign()
    {
        // Create a main container (Frame)
        var mainFrame = new FObject
        {
            Id = "1:1",
            Name = "MainContainer",
            Type = NodeType.FRAME,
            Visible = true,
            Size = new Vector2(640, 480),
            AbsoluteBoundingBox = new BoundingBox(0, 0, 640, 480),
            LayoutMode = LayoutMode.VERTICAL,
            PrimaryAxisAlignItems = PrimaryAxisAlignItem.CENTER,
            CounterAxisAlignItems = CounterAxisAlignItem.CENTER,
            ItemSpacing = 20,
            PaddingLeft = 40,
            PaddingRight = 40,
            PaddingTop = 40,
            PaddingBottom = 40,
            Fills = new List<Paint>
            {
                new Paint
                {
                    Type = "SOLID",
                    Visible = true,
                    Color = new FigmaColor(0.95f, 0.95f, 0.95f, 1.0f)
                }
            }
        };
        
        // Create title text
        var titleText = new FObject
        {
            Id = "1:2",
            Name = "Title",
            Type = NodeType.TEXT,
            Visible = true,
            Size = new Vector2(560, 40),
            AbsoluteBoundingBox = new BoundingBox(40, 40, 560, 40),
            Characters = "Welcome to FigmaSharp!",
            Style = new TypeStyle
            {
                FontFamily = "Arial",
                FontSize = 24,
                FontWeight = 700,
                TextAlignHorizontal = "CENTER"
            },
            Fills = new List<Paint>
            {
                new Paint
                {
                    Type = "SOLID",
                    Visible = true,
                    Color = new FigmaColor(0.2f, 0.2f, 0.8f, 1.0f)
                }
            }
        };
        
        // Create description text
        var descriptionText = new FObject
        {
            Id = "1:3",
            Name = "Description",
            Type = NodeType.TEXT,
            Visible = true,
            Size = new Vector2(560, 60),
            AbsoluteBoundingBox = new BoundingBox(40, 100, 560, 60),
            Characters = "This UI was generated from a Figma design\nand rendered to Terminal.Gui v2",
            Style = new TypeStyle
            {
                FontFamily = "Arial",
                FontSize = 14,
                FontWeight = 400,
                TextAlignHorizontal = "CENTER"
            },
            Fills = new List<Paint>
            {
                new Paint
                {
                    Type = "SOLID",
                    Visible = true,
                    Color = new FigmaColor(0.3f, 0.3f, 0.3f, 1.0f)
                }
            }
        };
        
        // Create a button
        var button = new FObject
        {
            Id = "1:4",
            Name = "ClickMeButton",
            Type = NodeType.FRAME,
            Visible = true,
            Size = new Vector2(200, 40),
            AbsoluteBoundingBox = new BoundingBox(220, 180, 200, 40),
            Fills = new List<Paint>
            {
                new Paint
                {
                    Type = "SOLID",
                    Visible = true,
                    Color = new FigmaColor(0.2f, 0.6f, 0.2f, 1.0f)
                }
            }
        };
        
        // Create button text
        var buttonText = new FObject
        {
            Id = "1:5",
            Name = "ButtonText",
            Type = NodeType.TEXT,
            Visible = true,
            Size = new Vector2(180, 30),
            AbsoluteBoundingBox = new BoundingBox(230, 185, 180, 30),
            Characters = "Click Me!",
            Style = new TypeStyle
            {
                FontFamily = "Arial",
                FontSize = 16,
                FontWeight = 600,
                TextAlignHorizontal = "CENTER"
            },
            Fills = new List<Paint>
            {
                new Paint
                {
                    Type = "SOLID",
                    Visible = true,
                    Color = new FigmaColor(1.0f, 1.0f, 1.0f, 1.0f)
                }
            }
        };
        
        // Create info text
        var infoText = new FObject
        {
            Id = "1:6",
            Name = "InfoText",
            Type = NodeType.TEXT,
            Visible = true,
            Size = new Vector2(560, 40),
            AbsoluteBoundingBox = new BoundingBox(40, 240, 560, 40),
            Characters = "Press ESC to exit",
            Style = new TypeStyle
            {
                FontFamily = "Arial",
                FontSize = 12,
                FontWeight = 400,
                TextAlignHorizontal = "CENTER"
            },
            Fills = new List<Paint>
            {
                new Paint
                {
                    Type = "SOLID",
                    Visible = true,
                    Color = new FigmaColor(0.5f, 0.5f, 0.5f, 1.0f)
                }
            }
        };
        
        // Assemble the hierarchy
        button.Children = new List<FObject> { buttonText };
        buttonText.Parent = button;
        
        mainFrame.Children = new List<FObject> 
        { 
            titleText, 
            descriptionText, 
            button, 
            infoText 
        };
        
        titleText.Parent = mainFrame;
        descriptionText.Parent = mainFrame;
        button.Parent = mainFrame;
        infoText.Parent = mainFrame;
        
        return mainFrame;
    }
}
