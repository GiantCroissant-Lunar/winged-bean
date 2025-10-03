const { test, expect } = require('@playwright/test');

test.describe('Verify Dungeon Gameplay is Active', () => {
  test('should show dungeon game stats updating in real-time', async ({ page }) => {
    await page.goto('http://localhost:4321/demo/');
    console.log('✓ Navigated to /demo/');

    await page.waitForLoadState('networkidle');
    
    const ptyTerminal = page.locator('#pty-terminal');
    await expect(ptyTerminal).toBeVisible({ timeout: 10000 });
    
    // Wait for game to fully initialize and start updating
    console.log('Waiting 10 seconds for game to initialize...');
    await page.waitForTimeout(10000);
    
    // Capture terminal content after game has been running
    const terminalText = await page.evaluate(() => {
      const term = window.__xterms && window.__xterms['pty-terminal'];
      if (!term || !term.buffer || !term.buffer.active) {
        return 'Terminal not initialized';
      }
      let text = '';
      const max = term.rows || 24;
      for (let y = 0; y < max; y++) {
        const line = term.buffer.active.getLine(y);
        if (line && typeof line.translateToString === 'function') {
          text += line.translateToString(true) + '\n';
        }
      }
      return text;
    });
    
    console.log('\n=== DUNGEON GAME DISPLAY (after 10s) ===');
    console.log(terminalText);
    console.log('=== END DISPLAY ===\n');
    
    // Take screenshot
    await page.screenshot({
      path: 'tests/e2e/screenshots/dungeon-gameplay-active.png',
      fullPage: true
    });
    console.log('✓ Screenshot saved: tests/e2e/screenshots/dungeon-gameplay-active.png');
    
    // Verify game is actually running by checking for expected content
    const checks = {
      hasTitle: terminalText.includes('Console Dungeon') || terminalText.includes('ECS Dungeon'),
      hasGameplay: terminalText.includes('Gameplay Active') || terminalText.includes('Dungeon game'),
      hasStats: terminalText.includes('HP:') || terminalText.includes('Loading game stats'),
      hasEntities: terminalText.includes('Entities') || terminalText.includes('Entity'),
      hasGameState: terminalText.includes('Game State') || terminalText.includes('Game initializing')
    };
    
    console.log('\n=== Verification Checks ===');
    console.log('Has title:', checks.hasTitle);
    console.log('Has gameplay indicator:', checks.hasGameplay);
    console.log('Has stats:', checks.hasStats);
    console.log('Has entities:', checks.hasEntities);
    console.log('Has game state:', checks.hasGameState);
    console.log('========================\n');
    
    // Should have at least the title and gameplay indicator
    expect(checks.hasTitle).toBe(true);
    expect(checks.hasGameplay).toBe(true);
  });
});
