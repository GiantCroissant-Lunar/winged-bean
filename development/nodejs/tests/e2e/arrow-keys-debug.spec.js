const { test, expect } = require('@playwright/test');

test.describe('Arrow keys debug', () => {
  test('test each arrow key individually', async ({ page }) => {
    await page.goto('http://localhost:4321/demo/?xtermDebug=1');
    await page.waitForLoadState('networkidle');

    const term = page.locator('#pty-terminal');
    await expect(term).toBeVisible({ timeout: 10000 });
    await page.waitForTimeout(3000);

    // Focus xterm
    const helper = page.locator('#pty-terminal .xterm-helper-textarea');
    if (await helper.count()) {
      await helper.focus();
    }

    // Test UP arrow
    console.log('===== Testing UP arrow =====');
    await page.keyboard.press('ArrowUp');
    await page.waitForTimeout(1000);

    // Test DOWN arrow
    console.log('===== Testing DOWN arrow =====');
    await page.keyboard.press('ArrowDown');
    await page.waitForTimeout(1000);

    // Test LEFT arrow
    console.log('===== Testing LEFT arrow =====');
    await page.keyboard.press('ArrowLeft');
    await page.waitForTimeout(1000);

    // Test RIGHT arrow
    console.log('===== Testing RIGHT arrow =====');
    await page.keyboard.press('ArrowRight');
    await page.waitForTimeout(1000);

    // Test letters A, B, C, D
    console.log('===== Testing letter A =====');
    await page.keyboard.press('a');
    await page.waitForTimeout(500);

    console.log('===== Testing letter B =====');
    await page.keyboard.press('b');
    await page.waitForTimeout(500);

    console.log('===== Testing letter C =====');
    await page.keyboard.press('c');
    await page.waitForTimeout(500);

    console.log('===== Testing letter D =====');
    await page.keyboard.press('d');
    await page.waitForTimeout(500);

    await page.screenshot({ path: 'tests/e2e/screenshots/arrow-keys-debug.png', fullPage: true });

    // Success - we're just debugging, not asserting
    expect(true).toBeTruthy();
  });
});
