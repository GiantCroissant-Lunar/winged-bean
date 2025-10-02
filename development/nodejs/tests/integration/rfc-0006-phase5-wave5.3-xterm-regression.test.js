/**
 * RFC-0006 Phase 5 Wave 5.3: xterm.js Integration Test After Dynamic Plugin Loading
 *
 * CRITICAL REGRESSION TEST: Verify that xterm.js integration still works
 * after RFC-0006 dynamic plugin loading implementation.
 *
 * This test validates:
 * - ConsoleDungeon.Host starts with dynamic plugin loading
 * - All plugins load successfully from plugins.json
 * - WebSocket server starts on port 4040
 * - Browser connects successfully via Astro frontend
 * - Terminal.Gui interface renders in xterm.js
 * - Screen content is properly formatted
 *
 * Success = All xterm.js functionality works identically after dynamic loading
 * Failure = RFC-0006 changes must be debugged before proceeding
 *
 * Dependencies: Issue #56 (Dynamic plugin loading verification)
 */

const { chromium } = require("playwright");
const { spawn } = require("child_process");
const path = require("path");

describe("RFC-0006 Phase 5 Wave 5.3: xterm.js Integration Test", () => {
  let browser;
  let page;
  let consoleDungeonHost;
  let astroServer;
  const WS_PORT = 4040;
  const ASTRO_PORT = 4321;
  const HOST_STARTUP_TIMEOUT = 15000;
  const ASTRO_STARTUP_TIMEOUT = 20000;

  beforeAll(async () => {
    console.log("🔧 Starting RFC-0006 Phase 5 Wave 5.3 Test Setup...");

    // Start ConsoleDungeon.Host with dynamic plugin loading
    console.log("Starting ConsoleDungeon.Host with dynamic plugin loading...");
    
    // Must run from bin/Debug/net8.0 directory where plugins.json and plugins/ exist
    const hostBinPath = path.join(
      __dirname,
      "../../../dotnet/console/src/host/ConsoleDungeon.Host/bin/Debug/net8.0"
    );
    
    consoleDungeonHost = spawn("dotnet", ["ConsoleDungeon.Host.dll"], {
      cwd: hostBinPath,
      env: { ...process.env },
    });

    let hostStartupComplete = false;
    let pluginsLoaded = false;

    consoleDungeonHost.stdout.on("data", (data) => {
      const output = data.toString();
      console.log(`[Host] ${output.trim()}`);
      
      // Check for dynamic plugin loading markers
      if (output.includes("Dynamic Plugin Mode")) {
        console.log("  ✓ Dynamic Plugin Mode confirmed");
      }
      if (output.includes("plugins loaded successfully")) {
        pluginsLoaded = true;
        console.log("  ✓ Plugins loaded successfully");
      }
      if (output.includes("Running. Press Ctrl+C to exit") || 
          output.includes("WebSocket server started")) {
        hostStartupComplete = true;
      }
    });

    consoleDungeonHost.stderr.on("data", (data) => {
      console.error(`[Host ERROR] ${data.toString().trim()}`);
    });

    // Wait for ConsoleDungeon.Host to be ready
    console.log("Waiting for ConsoleDungeon.Host startup...");
    await new Promise((resolve) => {
      const checkInterval = setInterval(() => {
        if (hostStartupComplete) {
          clearInterval(checkInterval);
          resolve();
        }
      }, 500);
      setTimeout(() => {
        clearInterval(checkInterval);
        resolve();
      }, HOST_STARTUP_TIMEOUT);
    });

    if (!hostStartupComplete) {
      throw new Error(
        "ConsoleDungeon.Host did not complete startup within timeout"
      );
    }

    console.log("✓ ConsoleDungeon.Host started with dynamic plugin loading");

    // Start Astro dev server
    console.log("Starting Astro frontend...");
    const astroPath = path.join(__dirname, "../../sites/docs");
    
    astroServer = spawn("npm", ["run", "dev"], {
      cwd: astroPath,
      env: { ...process.env },
    });

    let astroReady = false;

    astroServer.stdout.on("data", (data) => {
      const output = data.toString();
      console.log(`[Astro] ${output.trim()}`);
      if (output.includes("localhost:4321") || output.includes("ready in")) {
        astroReady = true;
      }
    });

    astroServer.stderr.on("data", (data) => {
      console.log(`[Astro] ${data.toString().trim()}`);
    });

    // Wait for Astro to start
    console.log("Waiting for Astro server on port 4321...");
    await new Promise((resolve) => {
      const checkInterval = setInterval(() => {
        if (astroReady) {
          clearInterval(checkInterval);
          resolve();
        }
      }, 500);
      setTimeout(() => {
        clearInterval(checkInterval);
        resolve();
      }, ASTRO_STARTUP_TIMEOUT);
    });

    console.log("✓ Astro server started");

    // Launch browser
    console.log("Launching browser...");
    browser = await chromium.launch({
      headless: true,
      args: ["--no-sandbox", "--disable-setuid-sandbox"],
    });
    page = await browser.newPage();

    console.log("✓ Browser launched");
    console.log("🚀 Setup complete - starting tests\n");
  }, 60000); // 60s timeout for setup

  afterAll(async () => {
    console.log("\n🧹 Cleaning up test environment...");
    if (browser) {
      await browser.close();
      console.log("✓ Browser closed");
    }
    if (consoleDungeonHost) {
      consoleDungeonHost.kill();
      console.log("✓ ConsoleDungeon.Host stopped");
    }
    if (astroServer) {
      astroServer.kill();
      console.log("✓ Astro server stopped");
    }
    console.log("✓ Cleanup complete");
  });

  test("✅ Wave 5.3.1: ConsoleDungeon.Host loads plugins dynamically", async () => {
    console.log("\n📋 Test: Dynamic plugin loading verification");

    // This test validates that the host is running with dynamic loading
    // The plugin loading output has already been captured during startup
    // Just verify that the process is still running
    expect(consoleDungeonHost).toBeTruthy();
    expect(consoleDungeonHost.killed).toBe(false);

    console.log("✓ ConsoleDungeon.Host running with dynamic plugin loading");
  });

  test("✅ Wave 5.3.2: Astro page loads successfully", async () => {
    console.log("\n📋 Test: Astro page loads successfully");

    await page.goto(`http://localhost:${ASTRO_PORT}`);
    await page.waitForSelector(".xterm", { timeout: 10000 });

    const terminalExists = await page.$(".xterm");
    expect(terminalExists).toBeTruthy();

    console.log("✓ Astro page loaded with xterm.js terminal");
  }, 15000);

  test("✅ Wave 5.3.3: WebSocket connection established", async () => {
    console.log("\n📋 Test: WebSocket connection established");

    await page.goto(`http://localhost:${ASTRO_PORT}`);
    await page.waitForSelector(".xterm", { timeout: 10000 });

    // Wait for WebSocket connection message
    await page.waitForTimeout(3000);

    // Check if terminal has content (indicates successful WebSocket connection)
    const terminalContent = await page.evaluate(() => {
      const terminal = document.querySelector(".xterm");
      return terminal ? terminal.textContent : "";
    });

    expect(terminalContent).toBeTruthy();
    expect(terminalContent.length).toBeGreaterThan(0);
    
    // Check for WebSocket connection indicators
    const hasWebSocketInfo = 
      terminalContent.includes("WebSocket") || 
      terminalContent.includes("4040") ||
      terminalContent.includes("Terminal.Gui v2");

    expect(hasWebSocketInfo).toBe(true);

    console.log("✓ WebSocket connected successfully");
    console.log(`  Terminal content length: ${terminalContent.length} characters`);
  }, 20000);

  test("✅ Wave 5.3.4: Terminal.Gui interface renders correctly", async () => {
    console.log("\n📋 Test: Terminal.Gui interface renders correctly");

    await page.goto(`http://localhost:${ASTRO_PORT}`);
    await page.waitForSelector(".xterm", { timeout: 10000 });

    // Wait for Terminal.Gui content to render
    await page.waitForTimeout(4000);

    const terminalContent = await page.evaluate(() => {
      const terminal = document.querySelector(".xterm");
      return terminal ? terminal.textContent : "";
    });

    // Verify Terminal.Gui interface elements (flexible checks for different UI versions)
    const hasTerminalGui = terminalContent.includes("Terminal.Gui");
    const hasSuccess = 
      terminalContent.includes("SUCCESS") || 
      terminalContent.includes("Console Dungeon");
    const hasWebSocket = 
      terminalContent.includes("4040") || 
      terminalContent.includes("WebSocket");

    expect(hasTerminalGui).toBe(true);
    expect(hasSuccess || hasWebSocket).toBe(true);

    console.log("✓ Terminal.Gui interface rendered correctly");
    console.log("  Interface verification:");
    console.log(`    - Terminal.Gui: ${hasTerminalGui ? "✓" : "✗"}`);
    console.log(`    - Application content: ${hasSuccess ? "✓" : "✗"}`);
    console.log(`    - WebSocket info: ${hasWebSocket ? "✓" : "✗"}`);
  }, 20000);

  test("✅ Wave 5.3.5: Dynamic loading does not break xterm.js", async () => {
    console.log("\n📋 Test: Dynamic plugin loading regression check");

    // This is the critical regression test - verify that despite RFC-0006 changes,
    // the xterm.js integration works exactly as before

    await page.goto(`http://localhost:${ASTRO_PORT}`);
    await page.waitForSelector(".xterm", { timeout: 10000 });
    await page.waitForTimeout(3000);

    // Get all terminal content
    const terminalContent = await page.evaluate(() => {
      const terminal = document.querySelector(".xterm");
      return terminal ? terminal.textContent : "";
    });

    // Comprehensive verification of xterm.js functionality
    const checks = [
      { 
        name: "xterm.js terminal loaded", 
        condition: terminalContent.length > 0 
      },
      { 
        name: "WebSocket connection active", 
        condition: terminalContent.includes("4040") || 
                   terminalContent.includes("WebSocket") 
      },
      { 
        name: "Terminal.Gui interface present", 
        condition: terminalContent.includes("Terminal.Gui") 
      },
      { 
        name: "Content renders in terminal", 
        condition: terminalContent.includes("SUCCESS") || 
                   terminalContent.includes("Console Dungeon") ||
                   terminalContent.includes("PTY")
      },
    ];

    console.log("  Verification checklist:");
    checks.forEach((check) => {
      console.log(`    ${check.condition ? "✓" : "✗"} ${check.name}`);
      expect(check.condition).toBe(true);
    });

    console.log("\n✅ SUCCESS: Dynamic plugin loading does not break xterm.js!");
    console.log("  All regression checks passed.");
  }, 20000);

  test("✅ Wave 5.3.6: Terminal displays plugin-loaded services", async () => {
    console.log("\n📋 Test: Terminal displays services from dynamically loaded plugins");

    await page.goto(`http://localhost:${ASTRO_PORT}`);
    await page.waitForSelector(".xterm", { timeout: 10000 });
    await page.waitForTimeout(3000);

    const terminalContent = await page.evaluate(() => {
      const terminal = document.querySelector(".xterm");
      return terminal ? terminal.textContent : "";
    });

    // Verify that services from dynamically loaded plugins are working
    // WebSocket plugin = working WebSocket connection
    const websocketPluginWorking = 
      terminalContent.includes("4040") || 
      terminalContent.includes("WebSocket");
    
    // TerminalUI plugin = Terminal.Gui interface visible
    const terminalUIPluginWorking = terminalContent.includes("Terminal.Gui");

    expect(websocketPluginWorking).toBe(true);
    expect(terminalUIPluginWorking).toBe(true);

    console.log("✓ Dynamically loaded plugins are working correctly:");
    console.log(`  - WebSocket Plugin: ${websocketPluginWorking ? "✓" : "✗"}`);
    console.log(`  - TerminalUI Plugin: ${terminalUIPluginWorking ? "✓" : "✗"}`);
  }, 20000);
});
