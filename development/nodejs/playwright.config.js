const { defineConfig, devices } = require('@playwright/test');
const path = require('path');
const { execSync } = require('child_process');

/**
 * Get version from build system
 */
const getVersion = () => {
  try {
    const version = execSync('cd ../../build && ./get-version.sh', { encoding: 'utf-8' }).trim();
    return version;
  } catch (error) {
    console.warn('Could not get version, using default:', error.message);
    return '0.0.1-dev';
  }
};

/**
 * ALWAYS test versioned artifacts - same path for dev and CI
 * This ensures we test what actually gets deployed
 */
const version = getVersion();
const artifactBase = path.join(__dirname, '../../build/_artifacts', `v${version}`, 'web');
const outputDirs = {
  reportDir: path.join(artifactBase, 'test-reports'),
  resultsDir: path.join(artifactBase, 'test-results'),
  artifactBase
};

console.log('Playwright Test Configuration:');
console.log(`  Version: ${version}`);
console.log(`  Testing artifacts at: ${artifactBase}`);
console.log(`  Test reports: ${outputDirs.reportDir}`);
console.log(`  Test results: ${outputDirs.resultsDir}`);

/**
 * Playwright configuration for Terminal.Gui PTY visual testing
 * @see https://playwright.dev/docs/test-configuration
 */
module.exports = defineConfig({
  testDir: './tests/e2e',

  // Maximum time one test can run
  timeout: 30 * 1000,

  // Test execution settings
  fullyParallel: false, // Run tests sequentially for PTY service
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1, // Single worker to avoid PTY conflicts

  // Output directories
  outputDir: outputDirs.resultsDir,

  // Reporter configuration
  reporter: [
    ['html', { outputFolder: outputDirs.reportDir }],
    ['list']
  ],

  // Shared settings for all projects
  use: {
    // Base URL for the Astro docs site
    baseURL: 'http://localhost:4321',

    // Collect trace on failure
    trace: 'retain-on-failure',

    // Screenshot on failure
    screenshot: 'only-on-failure',

    // Video on failure
    video: 'retain-on-failure',

    // Viewport size
    viewport: { width: 1280, height: 900 },
  },

  // Configure projects for different browsers
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    // Uncomment to test on other browsers
    // {
    //   name: 'firefox',
    //   use: { ...devices['Desktop Firefox'] },
    // },
    // {
    //   name: 'webkit',
    //   use: { ...devices['Desktop Safari'] },
    // },
  ],

  // Run local dev server before starting tests
  // Commented out since we use PM2 to manage services
  // webServer: {
  //   command: 'pnpm run dev',
  //   url: 'http://localhost:4321',
  //   reuseExistingServer: !process.env.CI,
  // },
});
