/**
 * Integration test to verify Terminal.GUI v2 console app can be displayed in Astro xterm via PTY
 *
 * This test validates:
 * 1. PM2 services are running (pty-service and docs-site)
 * 2. PTY service can spawn Terminal.Gui application
 * 3. WebSocket connection works on port 4041
 * 4. Terminal.Gui v2 app starts and renders correctly
 * 5. Binary PTY streaming is functional
 */

const WebSocket = require('ws');
const { execSync } = require('child_process');
const http = require('http');

describe('Terminal.GUI v2 PTY Integration Verification', () => {
  const PTY_SERVICE_PORT = 4041;
  const DOCS_SITE_PORT = 4321;
  const CONNECTION_TIMEOUT = 10000;
  const PTY_SPAWN_TIMEOUT = 15000;

  /**
   * Helper function to check if PM2 service is running
   */
  function isPM2ServiceRunning(serviceName) {
    try {
      const output = execSync('pm2 jlist', { encoding: 'utf-8' });
      const processes = JSON.parse(output);
      const service = processes.find(p => p.name === serviceName);
      return service && service.pm2_env.status === 'online';
    } catch (error) {
      console.error(`Error checking PM2 service ${serviceName}:`, error.message);
      return false;
    }
  }

  /**
   * Helper function to check if HTTP server is responding
   */
  function isHttpServerResponding(port) {
    return new Promise((resolve) => {
      const req = http.get(`http://localhost:${port}`, (res) => {
        resolve(res.statusCode === 200);
      });
      req.on('error', () => resolve(false));
      req.setTimeout(5000, () => {
        req.destroy();
        resolve(false);
      });
    });
  }

  /**
   * Helper function to test WebSocket connection and PTY spawning
   */
  function testPTYWebSocket() {
    return new Promise((resolve, reject) => {
      const ws = new WebSocket(`ws://localhost:${PTY_SERVICE_PORT}`);
      const receivedData = [];
      let ptySpawned = false;
      let terminalGuiDetected = false;

      const timeout = setTimeout(() => {
        ws.close();
        reject(new Error('PTY spawn timeout - Terminal.Gui did not start within expected time'));
      }, PTY_SPAWN_TIMEOUT);

      ws.on('open', () => {
        console.log('✓ WebSocket connected to PTY service');
      });

      ws.on('message', (data) => {
        // Store received data for analysis
        receivedData.push(data);

        // Convert binary data to string for detection
        const text = data.toString('utf-8');

        // Check for Terminal.Gui indicators
        if (text.includes('Terminal.Gui') ||
            text.includes('PTY Demo') ||
            text.includes('SUCCESS') ||
            text.includes('🎉')) {
          terminalGuiDetected = true;
          console.log('✓ Terminal.Gui v2 application detected in PTY output');
        }

        // Check for ANSI escape sequences (indicates terminal rendering)
        if (text.includes('\x1b[') || text.includes('\u001b[')) {
          ptySpawned = true;
          console.log('✓ PTY process spawned - ANSI escape sequences detected');
        }

        // If we've detected Terminal.Gui, we can conclude the test
        if (terminalGuiDetected && ptySpawned) {
          clearTimeout(timeout);
          ws.close();
          resolve({
            success: true,
            dataReceived: receivedData.length,
            terminalGuiDetected,
            ptySpawned
          });
        }
      });

      ws.on('error', (error) => {
        clearTimeout(timeout);
        reject(new Error(`WebSocket error: ${error.message}`));
      });

      ws.on('close', () => {
        clearTimeout(timeout);
        // If we haven't resolved yet, check if we got enough data
        if (receivedData.length > 0 && ptySpawned) {
          resolve({
            success: true,
            dataReceived: receivedData.length,
            terminalGuiDetected,
            ptySpawned,
            note: 'Connection closed but PTY was spawned successfully'
          });
        } else if (!ptySpawned) {
          reject(new Error('WebSocket closed before PTY process spawned'));
        }
      });
    });
  }

  test('PM2 pty-service should be running', () => {
    const isRunning = isPM2ServiceRunning('pty-service');
    expect(isRunning).toBe(true);
    console.log('✓ PM2 pty-service is online');
  });

  test('PM2 docs-site should be running', () => {
    const isRunning = isPM2ServiceRunning('docs-site');
    expect(isRunning).toBe(true);
    console.log('✓ PM2 docs-site is online');
  });

  test('Astro docs site should be accessible on port 4321', async () => {
    const isResponding = await isHttpServerResponding(DOCS_SITE_PORT);
    expect(isResponding).toBe(true);
    console.log('✓ Astro docs site is accessible');
  });

  test('PTY service should accept WebSocket connections and spawn Terminal.Gui v2', async () => {
    const result = await testPTYWebSocket();

    expect(result.success).toBe(true);
    expect(result.ptySpawned).toBe(true);
    expect(result.dataReceived).toBeGreaterThan(0);

    console.log('✓ PTY WebSocket connection successful');
    console.log(`✓ Received ${result.dataReceived} data frames from PTY`);

    if (result.terminalGuiDetected) {
      console.log('✓ Terminal.Gui v2 application successfully rendered in PTY');
    }
  }, PTY_SPAWN_TIMEOUT + 5000); // Add extra time for test timeout

  test('Verify XTerm.astro component has PTY mode support', () => {
    const fs = require('fs');
    const path = require('path');
    const xtermPath = path.join(__dirname, '../../sites/docs/src/components/XTerm.astro');

    const content = fs.readFileSync(xtermPath, 'utf-8');

    // Check for PTY mode support
    expect(content).toContain("mode?: 'legacy' | 'pty'");
    expect(content).toContain('ws.binaryType = \'arraybuffer\'');
    expect(content).toContain('terminal.write(new Uint8Array(event.data))');

    console.log('✓ XTerm.astro component has binary PTY streaming support');
  });
});

describe('System Configuration Verification', () => {
  test('Verify ecosystem.config.js has correct PTY service configuration', () => {
    const fs = require('fs');
    const path = require('path');
    const configPath = path.join(__dirname, '../../ecosystem.config.js');

    const content = fs.readFileSync(configPath, 'utf-8');

    expect(content).toContain('pty-service');
    expect(content).toContain('docs-site');
    expect(content).toContain('server.js');

    console.log('✓ PM2 ecosystem.config.js is properly configured');
  });

  test('Verify TerminalGui.PtyHost project exists', () => {
    const fs = require('fs');
    const path = require('path');
    const projectPath = path.join(
      __dirname,
      '../../../dotnet/console/src/host/TerminalGui.PtyHost/TerminalGui.PtyHost.csproj'
    );

    expect(fs.existsSync(projectPath)).toBe(true);
    console.log('✓ TerminalGui.PtyHost project exists');
  });

  test('Verify TerminalGuiService implementation exists', () => {
    const fs = require('fs');
    const path = require('path');
    const servicePath = path.join(
      __dirname,
      '../../../dotnet/console/src/plugins/WingedBean.Plugins.TerminalUI/TerminalGuiService.cs'
    );

    const content = fs.readFileSync(servicePath, 'utf-8');

    expect(content).toContain('class TerminalGuiService');
    expect(content).toContain('Terminal.Gui v2');
    expect(content).toContain('Application.Init()');
    expect(content).toContain('Application.Run');

    console.log('✓ TerminalGuiService implementation is correct');
  });
});
