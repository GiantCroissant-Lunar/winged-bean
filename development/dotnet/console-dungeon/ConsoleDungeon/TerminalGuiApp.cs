using System;
using Terminal.Gui;

namespace ConsoleDungeon;

public class TerminalGuiApp
{
    public static void Main(string[] args)
    {
        // Initialize Terminal.Gui
        Application.Init();

        try
        {
            // Create main window using correct syntax
            Window appWindow = new()
            {
                Title = "🎉 Terminal.Gui v2 PTY Demo (Ctrl+Q to quit)",
                BorderStyle = LineStyle.Single
            };

            // Create labels with correct syntax
            var titleLabel = new Label
            {
                X = 1,
                Y = 1,
                Text = "SUCCESS: Real Terminal.Gui v2 in PTY!"
            };

            var infoLabel = new Label
            {
                X = 1,
                Y = 3,
                Text = "✅ This proves Terminal.Gui v2 works in xterm.js via PTY"
            };

            var architectureLabel = new Label
            {
                X = 1,
                Y = 5,
                Text = "🔗 Architecture: Terminal.Gui → PTY → Node.js → WebSocket → xterm.js"
            };

            // Create a live time display
            var timeLabel = new Label
            {
                X = 1,
                Y = 7,
                Text = $"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };

            // Create interactive elements
            var button = new Button
            {
                X = 1,
                Y = 9,
                Text = "_Click Me!",
                IsDefault = true
            };

            var textField = new TextField
            {
                X = 15,
                Y = 9,
                Width = 30,
                Text = "Type something here..."
            };

            var statusLabel = new Label
            {
                X = 1,
                Y = 11,
                Text = "Ready - Terminal.Gui is fully functional!"
            };

            // Button click handler using correct event
            button.Accepting += (s, e) => {
                statusLabel.Text = $"Button clicked at {DateTime.Now:HH:mm:ss}!";
            };

            // Add all controls to window
            appWindow.Add(titleLabel, infoLabel, architectureLabel, timeLabel, button, textField, statusLabel);

            // Create menu bar with correct syntax
            var menu = new MenuBar();
            var fileMenu = new MenuBarItem("_File", new MenuItem[] {
                new("_Quit", "", () => Application.RequestStop())
            });
            var helpMenu = new MenuBarItem("_Help", new MenuItem[] {
                new("_About", "", () => {
                    MessageBox.Query("About", "Terminal.Gui v2 PTY Demo\nRunning in xterm.js via node-pty", "Ok");
                })
            });
            menu.Menus = new[] { fileMenu, helpMenu };
            appWindow.Add(menu);

            // Timer to update the time display every second
            void UpdateTime()
            {
                timeLabel.Text = $"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                Application.Wakeup();
            }

            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (sender, e) => UpdateTime();
            timer.Start();

            // Tip: Close the browser tab to end the PTY session.

            // Run the application
            Application.Run(appWindow);

            timer.Stop();
            timer.Dispose();
            appWindow.Dispose();
        }
        finally
        {
            // Cleanup
            Application.Shutdown();
        }
    }
}
