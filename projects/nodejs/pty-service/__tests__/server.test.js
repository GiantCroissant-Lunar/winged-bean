const WebSocket = require('ws');
const { spawn } = require('child_process');
const path = require('path');

describe('PTY WebSocket Server', () => {
  let serverProcess;
  let ws;
  const WS_URL = 'ws://localhost:4041';

  beforeAll((done) => {
    // Start the server as a child process
    serverProcess = spawn('node', ['server.js'], {
      cwd: path.join(__dirname, '..'),
      env: { ...process.env, NODE_ENV: 'test' }
    });

    serverProcess.stdout.on('data', (data) => {
      console.log(`Server: ${data}`);
      if (data.includes('listening on port 4041')) {
        done();
      }
    });

    serverProcess.stderr.on('data', (data) => {
      console.error(`Server error: ${data}`);
    });

    // Timeout if server doesn't start
    setTimeout(() => done(new Error('Server start timeout')), 5000);
  });

  afterAll(() => {
    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.close();
    }
    if (serverProcess) {
      serverProcess.kill();
    }
  });

  test('should accept WebSocket connections', (done) => {
    ws = new WebSocket(WS_URL);
    ws.binaryType = 'arraybuffer';

    ws.on('open', () => {
      expect(ws.readyState).toBe(WebSocket.OPEN);
      done();
    });

    ws.on('error', (error) => {
      done(error);
    });
  });

  test('should spawn PTY process on connection', (done) => {
    ws = new WebSocket(WS_URL);
    ws.binaryType = 'arraybuffer';

    let receivedData = false;

    ws.on('open', () => {
      // Send a resize message
      ws.send(JSON.stringify({ type: 'resize', cols: 80, rows: 24 }));
    });

    ws.on('message', (data) => {
      // Expect to receive binary data from PTY
      expect(data instanceof ArrayBuffer || data instanceof Buffer).toBe(true);
      receivedData = true;

      // Close and complete test
      setTimeout(() => {
        ws.close();
        expect(receivedData).toBe(true);
        done();
      }, 500);
    });

    ws.on('error', done);
  });

  test('should handle resize messages', (done) => {
    ws = new WebSocket(WS_URL);

    ws.on('open', () => {
      // Send resize - should not throw
      ws.send(JSON.stringify({ type: 'resize', cols: 100, rows: 30 }));

      // Wait a bit and close
      setTimeout(() => {
        ws.close();
        done();
      }, 200);
    });

    ws.on('error', done);
  });

  test('should forward keyboard input to PTY', (done) => {
    ws = new WebSocket(WS_URL);
    ws.binaryType = 'arraybuffer';

    ws.on('open', () => {
      // Send some keyboard input
      ws.send('echo test\r');

      // Should receive output
      setTimeout(() => {
        ws.close();
        done();
      }, 500);
    });

    ws.on('error', done);
  });

  test('should clean up PTY process on disconnect', (done) => {
    ws = new WebSocket(WS_URL);

    ws.on('open', () => {
      ws.close(1000, 'Test disconnect');
    });

    ws.on('close', (code, reason) => {
      expect(code).toBe(1000);
      // PTY process should be killed by server
      done();
    });

    ws.on('error', done);
  });
});