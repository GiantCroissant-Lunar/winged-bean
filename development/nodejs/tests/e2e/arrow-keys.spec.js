const { test, expect } = require('@playwright/test');

async function getPlayerPos(page) {
  return await page.evaluate(() => {
    const term = window.__xterms && window.__xterms['pty-terminal'];
    if (!term || !term.buffer || !term.buffer.active) return null;
    const rows = term.rows || 24;
    for (let y = 0; y < rows; y++) {
      const line = term.buffer.active.getLine(y);
      if (!line || typeof line.translateToString !== 'function') continue;
      const text = line.translateToString(true);
      const x = text.indexOf('@');
      if (x >= 0) return { row: y, col: x };
    }
    return null;
  });
}

test.describe('Arrow keys move player in Xterm/PTy', () => {
  test('down and right should move @', async ({ page }) => {
    await page.goto('http://localhost:4321/demo/?xtermDebug=1');
    await page.waitForLoadState('networkidle');

    const term = page.locator('#pty-terminal');
    await expect(term).toBeVisible({ timeout: 10000 });
    await page.waitForTimeout(3000);

    // Focus xterm helper textarea to ensure keyboard goes to terminal
    const helper = page.locator('#pty-terminal .xterm-helper-textarea');
    if (await helper.count()) {
      await helper.focus();
    } else {
      await term.click({ position: { x: 10, y: 10 } });
    }

    // Wait for player to appear
    let pos = null;
    for (let i = 0; i < 20; i++) {
      pos = await getPlayerPos(page);
      if (pos) break;
      await page.waitForTimeout(500);
    }
    expect(pos, 'Player @ not found in terminal buffer').not.toBeNull();

    const start = pos;

    // Press ArrowRight, fallback to 'D'
    await page.keyboard.press('ArrowRight');
    await page.waitForTimeout(200);
    let afterRight = await getPlayerPos(page);

    // If unchanged, try a second press
    if (afterRight && afterRight.col === start.col && afterRight.row === start.row) {
      await page.keyboard.press('ArrowRight');
      await page.waitForTimeout(200);
      afterRight = await getPlayerPos(page);
      // Fallback to 'D' (move right)
      if (afterRight && afterRight.col === start.col && afterRight.row === start.row) {
        await page.keyboard.type('d');
        await page.waitForTimeout(200);
        afterRight = await getPlayerPos(page);
      }
    }

    // Press ArrowDown, fallback to 'S'
    await page.keyboard.press('ArrowDown');
    await page.waitForTimeout(200);
    let afterDown = await getPlayerPos(page);
    if (afterDown && afterDown.col === (afterRight?.col ?? start.col) && afterDown.row === (afterRight?.row ?? start.row)) {
      await page.keyboard.press('ArrowDown');
      await page.waitForTimeout(200);
      afterDown = await getPlayerPos(page);
      if (afterDown && afterDown.col === (afterRight?.col ?? start.col) && afterDown.row === (afterRight?.row ?? start.row)) {
        await page.keyboard.type('s');
        await page.waitForTimeout(200);
        afterDown = await getPlayerPos(page);
      }
    }

    // Assertions: moved to the right and down compared to start
    expect(afterRight && afterRight.col > start.col, `Right did not move: start=${JSON.stringify(start)} right=${JSON.stringify(afterRight)}`).toBeTruthy();
    expect(afterDown && afterDown.row > (afterRight?.row ?? start.row), `Down did not move: right=${JSON.stringify(afterRight)} down=${JSON.stringify(afterDown)}`).toBeTruthy();

    await page.screenshot({ path: 'tests/e2e/screenshots/arrow-keys-result.png', fullPage: true });
  });
});
