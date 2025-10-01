using WingedBean.Plugins.TerminalUI;

namespace TerminalGui.PtyHost;

public class Program
{
    public static void Main(string[] args)
    {
        var ui = new TerminalGuiService();
        ui.Initialize();
        ui.Run();
    }
}
