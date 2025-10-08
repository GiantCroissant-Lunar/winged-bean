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
 * Get output directories based on environment
 * - In CI or with ARTIFACT_OUTPUT=1: Use versioned artifacts
 * - In development: Use local directories (for quick iteration)
 */
const getOutputDirs = () => {
  const useArtifacts = process.env.CI || process.env.ARTIFACT_OUTPUT === '1';

  if (useArtifacts) {
    const version = getVersion();
    const artifactBase = path.join(__dirname, '../../build/_artifacts', `v${version}`, 'web');
    return {
      reportDir: path.join(artifactBase, 'test-reports'),
      resultsDir: path.join(artifactBase, 'test-results'),
      artifactBase
    };
  }

  // Development mode - use local directories
  return {
    reportDir: 'playwright-report',
    resultsDir: 'test-results',
    artifactBase: null
  };
};

const outputDirs = getOutputDirs();

console.log('Playwright output configuration:');
console.log(`  Report: ${outputDirs.reportDir}`);
console.log(`  Results: ${outputDirs.resultsDir}`);
if (outputDirs.artifactBase) {
  console.log(`  Using versioned artifacts at: ${outputDirs.artifactBase}`);
}

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
