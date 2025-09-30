module.exports = {
  testEnvironment: 'node',
  testMatch: [
    '**/__tests__/**/*.js',
    '**/tests/**/*.test.js'
  ],
  testPathIgnorePatterns: [
    '/node_modules/',
    '/dist/',
    '/.astro/'
  ],
  collectCoverageFrom: [
    'pty-service/**/*.js',
    '!pty-service/node_modules/**',
    '!pty-service/test-*.js'
  ],
  coverageDirectory: 'coverage',
  coverageReporters: ['text', 'lcov', 'html'],
  testTimeout: 10000,
  verbose: true
};