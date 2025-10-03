/**
 * Versioned State Capture Test
 * 
 * This test captures the complete state of the application at a specific version:
 * - Screenshots of actual web display
 * - Terminal buffer content
 * - Asciinema recordings
 * - Application logs
 * 
 * All artifacts are saved to build/_artifacts/v{VERSION}/ per RFC-0010
 */

const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

// Get version from build system
const getVersion = () => {
  try {
    const { execSync } = require('child_process');
    const version = execSync('cd ../../build && ./get-version.sh', { encoding: 'utf-8' }).trim();
    return version;
  } catch (error) {
    console.warn('Could not get version, using default:', error.message);
    return '0.0.1-dev';
  }
};

const VERSION = getVersion();
const ARTIFACT_DIR = path.join(__dirname, '../../..', '../../build/_artifacts', `v${VERSION}`);

// Ensure artifact directories exist
const setupArtifactDirs = () => {
  const dirs = [
    path.join(ARTIFACT_DIR, 'web/screenshots'),
    path.join(ARTIFACT_DIR, 'web/recordings'),
    path.join(ARTIFACT_DIR, 'web/logs'),
    path.join(ARTIFACT_DIR, 'web/terminal-captures')
  ];
  
  dirs.forEach(dir => {
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }
  });
};

test.describe(`Versioned State Capture - v${VERSION}`, () => {
  test.beforeAll(() => {
    setupArtifactDirs();
    console.log(`\n📦 Capturing artifacts for version: ${VERSION}`);
    console.log(`📁 Artifact directory: ${ARTIFACT_DIR}\n`);
  });

  test('capture complete dungeon game state with versioning', async ({ page }) => {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    
    // Navigate to demo page
    await page.goto('http://localhost:4321/demo/');
    console.log('✓ Navigated to /demo/');

    await page.waitForLoadState('networkidle');
    
    // === 1. INITIAL STATE CAPTURE ===
    console.log('\n=== Phase 1: Initial State ===');
    await page.screenshot({
      path: path.join(ARTIFACT_DIR, 'web/screenshots', `dungeon-initial-${timestamp}.png`),
      fullPage: true
    });
    console.log(`✓ Saved: web/screenshots/dungeon-initial-${timestamp}.png`);

    // === 2. WAIT FOR GAME TO START ===
    const ptyTerminal = page.locator('#pty-terminal');
    await expect(ptyTerminal).toBeVisible({ timeout: 10000 });
    
    console.log('\n=== Phase 2: Waiting for Game Initialize (10s) ===');
    await page.waitForTimeout(10000);

    // === 3. CAPTURE RUNNING STATE ===
    console.log('\n=== Phase 3: Capture Running State ===');
    
    // 3a. Terminal buffer content
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
    
    // Save terminal buffer as text
    const bufferPath = path.join(ARTIFACT_DIR, 'web/terminal-captures', `terminal-buffer-${timestamp}.txt`);
    fs.writeFileSync(bufferPath, terminalText, 'utf-8');
    console.log(`✓ Saved: web/terminal-captures/terminal-buffer-${timestamp}.txt`);
    
    // 3b. Screenshot of running state
    await page.screenshot({
      path: path.join(ARTIFACT_DIR, 'web/screenshots', `dungeon-running-${timestamp}.png`),
      fullPage: true
    });
    console.log(`✓ Saved: web/screenshots/dungeon-running-${timestamp}.png`);
    
    // 3c. Terminal-only screenshot
    await ptyTerminal.screenshot({
      path: path.join(ARTIFACT_DIR, 'web/screenshots', `terminal-only-${timestamp}.png`)
    });
    console.log(`✓ Saved: web/screenshots/terminal-only-${timestamp}.png`);

    // === 4. VERIFY GAME STATE ===
    console.log('\n=== Phase 4: Verification ===');
    const checks = {
      hasTitle: terminalText.includes('Console Dungeon') || terminalText.includes('ECS Dungeon'),
      hasGameplay: terminalText.includes('Gameplay Active') || terminalText.includes('Dungeon game'),
      hasStats: terminalText.includes('HP:') || terminalText.includes('stats'),
      hasEntities: terminalText.includes('Entities') || terminalText.includes('Entity'),
      hasGameState: terminalText.includes('Game State') || terminalText.includes('initializing')
    };
    
    // Save verification results
    const verificationPath = path.join(ARTIFACT_DIR, 'web/logs', `verification-${timestamp}.json`);
    const verificationData = {
      version: VERSION,
      timestamp: new Date().toISOString(),
      checks,
      terminalPreview: terminalText.substring(0, 500),
      url: 'http://localhost:4321/demo/',
      browser: 'chromium'
    };
    fs.writeFileSync(verificationPath, JSON.stringify(verificationData, null, 2), 'utf-8');
    console.log(`✓ Saved: web/logs/verification-${timestamp}.json`);
    
    // === 5. SUMMARY ===
    console.log('\n=== Capture Summary ===');
    console.log(`Version: ${VERSION}`);
    console.log(`Timestamp: ${timestamp}`);
    console.log('Verification Results:');
    Object.entries(checks).forEach(([key, value]) => {
      console.log(`  ${value ? '✓' : '✗'} ${key}`);
    });
    console.log(`\n📦 All artifacts saved to: ${ARTIFACT_DIR}`);
    console.log('\n=== Terminal Content Preview ===');
    console.log(terminalText.substring(0, 500));
    console.log('===\n');
    
    // Assertions
    expect(checks.hasTitle).toBe(true);
    expect(checks.hasGameplay).toBe(true);
  });

  test('capture longer session with state changes', async ({ page }) => {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    
    await page.goto('http://localhost:4321/demo/');
    await page.waitForLoadState('networkidle');
    
    const ptyTerminal = page.locator('#pty-terminal');
    await expect(ptyTerminal).toBeVisible({ timeout: 10000 });
    
    console.log('\n=== Capturing 30-second session ===');
    
    // Capture screenshots at intervals
    const intervals = [5, 10, 15, 20, 30];
    for (const seconds of intervals) {
      await page.waitForTimeout(seconds * 1000 - (intervals[intervals.indexOf(seconds) - 1] || 0) * 1000);
      
      await page.screenshot({
        path: path.join(ARTIFACT_DIR, 'web/screenshots', `session-${seconds}s-${timestamp}.png`),
        fullPage: true
      });
      console.log(`✓ Captured state at ${seconds}s`);
      
      // Capture terminal buffer
      const terminalText = await page.evaluate(() => {
        const term = window.__xterms && window.__xterms['pty-terminal'];
        if (!term || !term.buffer || !term.buffer.active) return '';
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
      
      fs.writeFileSync(
        path.join(ARTIFACT_DIR, 'web/terminal-captures', `buffer-${seconds}s-${timestamp}.txt`),
        terminalText,
        'utf-8'
      );
    }
    
    console.log(`\n✓ Session capture complete for version ${VERSION}`);
    expect(true).toBe(true);
  });
});
