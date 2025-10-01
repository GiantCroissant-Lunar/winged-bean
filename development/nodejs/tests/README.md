# Testing Documentation

## Test Structure

```
projects/nodejs/
├── pty-service/
│   └── __tests__/              # Unit tests for PTY service
│       ├── server.test.js      # WebSocket server tests
│       └── pty-spawn.test.js   # PTY spawning tests
├── tests/
│   └── integration/            # Integration tests
│       ├── e2e.test.js         # End-to-end browser tests
│       └── pm2-lifecycle.test.js # PM2 workflow tests
└── jest.config.js              # Jest configuration
```

## Running Tests

### All Tests

```bash
pnpm test
```

### Unit Tests Only

```bash
pnpm test:unit
```

### Integration Tests

```bash
pnpm test:integration
```

### E2E Tests (requires running services)

```bash
pnpm test:e2e
```

### PM2 Workflow Tests

```bash
pnpm test:pm2
```

### Watch Mode (for development)

```bash
pnpm test:watch
```

### Coverage Report

```bash
pnpm test:coverage
```

### Manual Tests

```bash
cd pty-service
npm run test:manual    # Test PTY spawning directly
npm run test:client    # Test WebSocket client connection
```

## Test Categories

### 1. Unit Tests (`__tests__/`)

- Test individual components in isolation
- Mock external dependencies
- Fast execution
- Run on every commit

**Examples:**

- PTY process spawning
- WebSocket message handling
- Error handling
- Input/output formatting

### 2. Integration Tests (`tests/integration/`)

- Test interaction between components
- May require running services
- Moderate execution time
- Run before deployment

**Examples:**

- Full WebSocket communication flow
- PTY → Server → Browser pipeline
- pm2 lifecycle management
- File watching and auto-reload

### 3. E2E Tests (`tests/integration/e2e.test.js`)

- Test complete user workflows
- Requires browser automation (Playwright)
- Slow execution
- Run before releases

**Examples:**

- Browser loads Astro page
- Terminal renders correctly
- Keyboard/mouse input works
- Terminal.Gui interaction

## Prerequisites

### For Unit Tests

```bash
pnpm install
```

### For Integration Tests

```bash
# Ensure services can start
pnpm run dev
pnpm run dev:stop
```

### For E2E Tests

```bash
# Install Playwright browsers
npx playwright install
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: "18"
      - run: pnpm install
      - run: pnpm test:unit
      - run: pnpm test:integration
```

## Writing New Tests

### Unit Test Template

```javascript
describe("Feature Name", () => {
  test("should do something", () => {
    // Arrange
    const input = "test";

    // Act
    const result = myFunction(input);

    // Assert
    expect(result).toBe("expected");
  });
});
```

### Integration Test Template

```javascript
describe("Integration: Feature", () => {
  beforeAll(async () => {
    // Start services
  });

  afterAll(async () => {
    // Clean up
  });

  test("should integrate correctly", async () => {
    // Test multi-component interaction
  });
});
```

## Test Coverage Goals

- **Unit Tests:** >80% coverage
- **Integration Tests:** Critical paths covered
- **E2E Tests:** Main user workflows

## Debugging Tests

### Run specific test file

```bash
npx jest path/to/test.js
```

### Run specific test

```bash
npx jest -t "test name"
```

### Debug with Node inspector

```bash
node --inspect-brk node_modules/.bin/jest --runInBand
```

### View detailed output

```bash
pnpm test -- --verbose
```

## Known Issues

1. **WebSocket tests may be flaky**: Add retry logic or increase timeouts
2. **E2E tests require stable network**: Use local services, not remote
3. **pm2 tests may interfere**: Run in isolation or use separate pm2 instances

## Future Improvements

- [ ] Add performance benchmarks
- [ ] Add visual regression tests for Terminal.Gui UI
- [ ] Add load testing for WebSocket connections
- [ ] Add mutation testing
- [ ] Add contract tests for API boundaries
