#!/bin/bash
# Quick diagnostic to check console state

cat > /tmp/console-check.cs << 'EOF'
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine($"TERM: {Environment.GetEnvironmentVariable("TERM")}");
        Console.WriteLine($"Console.IsInputRedirected: {Console.IsInputRedirected}");
        Console.WriteLine($"Console.IsOutputRedirected: {Console.IsOutputRedirected}");
        Console.WriteLine($"Console.IsErrorRedirected: {Console.IsErrorRedirected}");

        var term = Environment.GetEnvironmentVariable("TERM");
        var redirected = Console.IsInputRedirected || Console.IsOutputRedirected;
        var headlessMode = string.IsNullOrEmpty(term) || redirected;

        Console.WriteLine("");
        Console.WriteLine($"Headless mode would be: {headlessMode}");

        if (headlessMode)
        {
            Console.WriteLine("PROBLEM: Application will run in HEADLESS mode (no Terminal.Gui UI)");
            if (redirected)
                Console.WriteLine("  Reason: Console redirection detected");
            if (string.IsNullOrEmpty(term))
                Console.WriteLine("  Reason: TERM environment variable is empty");
        }
        else
        {
            Console.WriteLine("SUCCESS: Application will show Terminal.Gui UI");
        }
    }
}
EOF

echo "Compiling diagnostic..."
dotnet new console -n ConsoleCheck -o /tmp/console-check --force > /dev/null 2>&1
cp /tmp/console-check.cs /tmp/console-check/Program.cs
cd /tmp/console-check && dotnet build -c Release > /dev/null 2>&1

echo ""
echo "=== Console State Check ==="
echo ""
/tmp/console-check/bin/Release/net*/ConsoleCheck

echo ""
echo "If it shows 'HEADLESS mode', the UI won't appear!"
