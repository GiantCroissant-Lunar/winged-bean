const pty = require('node-pty');

describe('PTY Process Spawning', () => {
  test('should spawn bash shell', (done) => {
    const ptyProcess = pty.spawn('/bin/bash', ['-lc', 'echo "test"'], {
      name: 'xterm-256color',
      cols: 80,
      rows: 24,
      env: {
        TERM: 'xterm-256color',
        COLORTERM: 'truecolor'
      }
    });

    let output = '';

    ptyProcess.onData((data) => {
      output += data;
    });

    ptyProcess.onExit(({ exitCode }) => {
      expect(output).toContain('test');
      expect(exitCode).toBe(0);
      done();
    });

    setTimeout(() => {
      if (!ptyProcess.killed) {
        ptyProcess.kill();
        done(new Error('PTY process timeout'));
      }
    }, 3000);
  });

  test('should detect .NET SDK', (done) => {
    const ptyProcess = pty.spawn('dotnet', ['--version'], {
      name: 'xterm-256color',
      cols: 80,
      rows: 24
    });

    let output = '';

    ptyProcess.onData((data) => {
      output += data;
    });

    ptyProcess.onExit(({ exitCode }) => {
      // Should output version like "9.0.100"
      expect(output).toMatch(/\d+\.\d+\.\d+/);
      expect(exitCode).toBe(0);
      done();
    });

    setTimeout(() => {
      if (!ptyProcess.killed) {
        ptyProcess.kill();
        done(new Error('Dotnet version check timeout'));
      }
    }, 3000);
  });

  test('should handle PTY resize', () => {
    const ptyProcess = pty.spawn('/bin/bash', [], {
      name: 'xterm-256color',
      cols: 80,
      rows: 24
    });

    // Should not throw
    expect(() => {
      ptyProcess.resize(100, 30);
    }).not.toThrow();

    // Bounds checking
    expect(() => {
      ptyProcess.resize(20, 5);
    }).not.toThrow();

    ptyProcess.kill();
  });

  test('should handle invalid commands', (done) => {
    const ptyProcess = pty.spawn('/bin/bash', ['-lc', 'nonexistent-command'], {
      name: 'xterm-256color',
      cols: 80,
      rows: 24
    });

    ptyProcess.onExit(({ exitCode }) => {
      expect(exitCode).not.toBe(0);
      done();
    });

    setTimeout(() => {
      if (!ptyProcess.killed) {
        ptyProcess.kill();
        done(new Error('Invalid command test timeout'));
      }
    }, 3000);
  });
});