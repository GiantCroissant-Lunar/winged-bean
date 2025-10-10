const { test, expect } = require("@playwright/test");
const path = require("path");
const fs = require("fs");
const { getVersion, getArtifactsPath } = require("../../get-version");

test.describe("Check log console display", () => {
  test("capture full terminal buffer including log console", async ({ page }) => {
    await page.goto("http://localhost:4321/demo/?xtermDebug=1");

    console.log("✓ Navigated to /demo/");

    // Wait for terminal to initialize
    await page.waitForSelector('#pty-terminal', { timeout: 30000 });
    await page.waitForFunction(() => {
      return !!(window.__xterms && window.__xterms['pty-terminal']);
    }, { timeout: 30000 });

    console.log("✓ Terminal initialized");

    await page.waitForTimeout(2000);

    // Get full terminal buffer
    const fullBuffer = await page.evaluate(() => {
      const term = window.__xterms && window.__xterms['pty-terminal'];
      if (!term) return "No terminal found";

      const buffer = term.buffer.active;
      const lines = [];
      for (let i = 0; i < buffer.length; i++) {
        const line = buffer.getLine(i);
        if (line) {
          lines.push(line.translateToString(true));
        }
      }
      return lines.join('\n');
    });

    console.log('\n=== INITIAL TERMINAL BUFFER (ALL ROWS) ===');
    console.log(fullBuffer);
    console.log('=== END BUFFER ===\n');

    // Focus terminal and send arrow keys to generate log entries
    const terminalElement = await page.locator('.xterm-helper-textarea').first();
    await terminalElement.focus();

    console.log('Sending 5 arrow down keys...');
    for (let i = 0; i < 5; i++) {
      await page.keyboard.press('ArrowDown');
      await page.waitForTimeout(300);
    }

    // Wait a bit for log updates
    await page.waitForTimeout(1000);

    // Get updated buffer
    const updatedBuffer = await page.evaluate(() => {
      const term = window.__xterms && window.__xterms['pty-terminal'];
      const buffer = term.buffer.active;
      const lines = [];
      for (let i = 0; i < buffer.length; i++) {
        const line = buffer.getLine(i);
        if (line) {
          lines.push(line.translateToString(true));
        }
      }
      return lines.join('\n');
    });

    console.log('\n=== AFTER ARROW KEYS (ALL ROWS) ===');
    console.log(updatedBuffer);
    console.log('=== END BUFFER ===\n');

    // Check if log console is visible
    const hasLogConsole = updatedBuffer.includes('Log Console') ||
                          updatedBuffer.includes('INFO') ||
                          updatedBuffer.includes('INPUT');

    console.log(`Log console visible: ${hasLogConsole}`);
  });
});
