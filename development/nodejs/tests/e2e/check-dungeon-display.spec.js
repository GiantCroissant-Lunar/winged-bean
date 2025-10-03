const { test, expect } = require('@playwright/test');

test.describe('Check actual dungeon display on /demo/', () => {
  test('capture what is actually displayed on the demo page', async ({ page }) => {
    // Navigate to the demo page
    await page.goto('http://localhost:4321/demo/');
    console.log('✓ Navigated to /demo/');

    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Take immediate screenshot
    await page.screenshot({
      path: 'tests/e2e/screenshots/demo-page-initial.png',
      fullPage: true
    });
    console.log('✓ Initial screenshot saved');

    // Check if PTY terminal element exists
    const ptyTerminal = page.locator('#pty-terminal');
    const ptyExists = await ptyTerminal.count() > 0;
    console.log(`PTY terminal element exists: ${ptyExists}`);

    if (ptyExists) {
      // Wait a bit for terminal to initialize
      await page.waitForTimeout(5000);
      
      // Take another screenshot after terminal loads
      await page.screenshot({
        path: 'tests/e2e/screenshots/demo-page-with-terminal.png',
        fullPage: true
      });
      console.log('✓ Terminal screenshot saved');

      // Try to get terminal content
      const terminalHTML = await ptyTerminal.innerHTML();
      console.log('Terminal HTML preview:', terminalHTML.substring(0, 500));

      // Try to read terminal text via xterm
      const terminalText = await page.evaluate(() => {
        const term = window.__xterms && window.__xterms['pty-terminal'];
        if (!term || !term.buffer || !term.buffer.active) {
          return 'Terminal not initialized';
        }
        let text = '';
        const max = Math.min(term.rows || 24, 50);
        for (let y = 0; y < max; y++) {
          const line = term.buffer.active.getLine(y);
          if (line && typeof line.translateToString === 'function') {
            text += line.translateToString(true) + '\n';
          }
        }
        return text;
      });
      
      console.log('\n=== TERMINAL CONTENT ===');
      console.log(terminalText);
      console.log('=== END TERMINAL CONTENT ===\n');
    }

    // Get the full page text content
    const bodyText = await page.locator('body').textContent();
    console.log('\n=== PAGE TEXT (first 1000 chars) ===');
    console.log(bodyText.substring(0, 1000));
    console.log('=== END PAGE TEXT ===\n');

    // Always pass - we just want to see the output
    expect(true).toBe(true);
  });
});
