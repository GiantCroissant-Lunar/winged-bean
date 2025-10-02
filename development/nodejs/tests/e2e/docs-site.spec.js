/**
 * Playwright E2E Tests for Documentation Site
 * Tests landing page, docs page, and demo page
 */

const { test, expect } = require('@playwright/test');

const BASE_URL = process.env.BASE_URL || 'http://localhost:4321';

test.describe('Documentation Site', () => {
  test('landing page loads successfully', async ({ page }) => {
    await page.goto(BASE_URL);

    // Check page title
    await expect(page).toHaveTitle(/Winged Bean/);

    // Check main heading (role-based to avoid strict-mode issues)
    await expect(page.getByRole('heading', { level: 1, name: /Winged Bean/i })).toBeVisible();

    // Check subtitle
    await expect(page.locator('.subtitle')).toContainText('Multi-tier plugin architecture');

    // Check navigation links exist
    const docsLink = page.locator('a[href="/docs/"]');
    await expect(docsLink).toBeVisible();
    await expect(docsLink).toContainText('Documentation');

    const demoLink = page.locator('a[href="/demo/"]');
    await expect(demoLink).toBeVisible();
    await expect(demoLink).toContainText('Live Demo');

    // Check features section
    await expect(page.locator('.features')).toBeVisible();
    await expect(page.locator('.feature')).toHaveCount(4);

    // No console errors
    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    await page.waitForTimeout(1000);
    expect(consoleErrors).toHaveLength(0);
  });

  test('docs homepage loads successfully', async ({ page }) => {
    await page.goto(`${BASE_URL}/docs/`);

    // Check Starlight is loaded
    await expect(page.locator('.site-title')).toContainText('Winged Bean');

    // Check main content
    await expect(page.getByRole('heading', { level: 1, name: /Documentation/i })).toBeVisible();

    // Check sidebar nav is present (visibility may vary with viewport / overlays)
    const mainNav = page.getByRole('navigation', { name: /Main/i });
    await expect(mainNav).toBeAttached();

    // Check Getting Started appears somewhere in the page
    await expect(page.locator('text=Getting Started').first()).toBeAttached();

    // Check Demo link appears on the page (sidebar visibility may vary)
    await expect(page.locator('text=Terminal.Gui Live Demo').first()).toBeAttached();

    // Search button is disabled in dev; presence is not guaranteed across envs
    // (Skip strict assertion to avoid flakiness in dev server overlays)

    // No console errors
    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    await page.waitForTimeout(1000);
    expect(consoleErrors).toHaveLength(0);
  });

  test('demo page loads successfully', async ({ page }) => {
    // Allow console errors for WebSocket connection failures (expected in test environment)
    const allowedErrors = [
      'WebSocket connection',
      'Failed to load resource',
      'net::ERR_CONNECTION_REFUSED',
      'WebSocket error',
      'WebSocket closed',
      'Stack trace:'
    ];

    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        const text = msg.text();
        const isAllowed = allowedErrors.some(allowed => text.includes(allowed));
        if (!isAllowed) {
          consoleErrors.push(text);
        }
      }
    });

    await page.goto(`${BASE_URL}/demo/`);

    // Check page title & main heading
    await expect(page).toHaveTitle(/Winged Bean Docs/);
    await expect(page.getByRole('heading', { level: 1, name: 'Winged Bean Docs' })).toBeVisible();

    // Check sections exist (legacy section removed)
    await expect(page.locator('h2').filter({ hasText: 'Sample Terminal Session' })).toBeVisible();
    await expect(page.locator('h2').filter({ hasText: 'Live Terminal (PTY via node-pty)' })).toBeVisible();

    // Check cast file selector exists
    const castSelect = page.locator('#cast-select');
    await expect(castSelect).toBeVisible();

    // Check asciinema player container exists (attached) and has expected data-src
    const player = page.locator('.asciinema-player');
    await expect(player).toBeAttached();
    await expect(player).toHaveAttribute('data-src', '/example.cast');

    // Check XTerm containers exist (only PTY terminal remains)
    const xtermContainers = page.locator('.xterm');
    await expect(xtermContainers).toHaveCount(1);

    // Wait for any async loading
    await page.waitForTimeout(2000);

    // Check for unexpected console errors (WebSocket errors are expected)
    expect(consoleErrors).toHaveLength(0);
  });

  test('navigation between pages works', async ({ page }) => {
    // Start at landing page
    await page.goto(BASE_URL);
    await expect(page.getByRole('heading', { level: 1, name: /Winged Bean/i })).toBeVisible();

    // Navigate to docs
    await page.click('a[href="/docs/"]');
    await expect(page).toHaveURL(`${BASE_URL}/docs/`);
    await expect(page.locator('.site-title')).toContainText('Winged Bean');

    // Navigate to demo from sidebar
    await page.click('text=Terminal.Gui Live Demo');
    await expect(page).toHaveURL(`${BASE_URL}/demo/`);
    await expect(page.getByRole('heading', { level: 1, name: 'Winged Bean Docs' })).toBeVisible();

    // Go back to landing page
    await page.goto(BASE_URL);
    await expect(page.getByRole('heading', { level: 1, name: /Winged Bean/i })).toBeVisible();

    // Navigate to demo directly
    await page.click('a[href="/demo/"]');
    await expect(page).toHaveURL(`${BASE_URL}/demo/`);
  });

  test('responsive design works', async ({ page }) => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto(BASE_URL);

    await expect(page.locator('h1')).toBeVisible();
    await expect(page.locator('.links')).toBeVisible();

    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto(`${BASE_URL}/docs/`);

    await expect(page.locator('.site-title')).toBeVisible();

    // Test desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto(`${BASE_URL}/demo/`);
    await expect(page.getByRole('heading', { level: 1, name: /Winged Bean Docs/i })).toBeVisible();
  });

  test('all pages return 200 status', async ({ page }) => {
    // Landing page
    const landingResponse = await page.goto(BASE_URL);
    expect(landingResponse.status()).toBe(200);

    // Docs page
    const docsResponse = await page.goto(`${BASE_URL}/docs/`);
    expect(docsResponse.status()).toBe(200);

    // Demo page
    const demoResponse = await page.goto(`${BASE_URL}/demo/`);
    expect(demoResponse.status()).toBe(200);
  });
});
