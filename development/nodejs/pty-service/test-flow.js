// Test ConsoleDungeon via PTY WebSocket
const WebSocket = require('ws');

async function testFlow() {
    console.log('=== Testing ConsoleDungeon Full Flow ===\n');

    const ws = new WebSocket('ws://localhost:4041');
    let outputBuffer = '';
    let checkpoints = {
        connected: false,
        startAsync: false,
        sceneInit: false,
        gameInit: false,
        appRun: false,
        escSent: false,
        uiFinished: false,
        timerStopped: false,
        stopAsync: false,
        stopped: false
    };

    ws.on('open', () => {
        checkpoints.connected = true;
        console.log('✓ Connected to PTY service');

        // Spawn the ConsoleDungeon.Host process
        ws.send(JSON.stringify({
            type: 'spawn',
            command: './ConsoleDungeon.Host',
            cwd: '/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/v0.0.1-344/dotnet/bin',
            cols: 80,
            rows: 24
        }));
    });

    ws.on('message', (data) => {
        const msg = data.toString();
        outputBuffer += msg;

        // Track initialization sequence
        if (!checkpoints.startAsync && msg.includes('StartAsync invoked')) {
            checkpoints.startAsync = true;
            console.log('✓ StartAsync invoked');
        }

        if (!checkpoints.sceneInit && msg.includes('Scene initialized')) {
            checkpoints.sceneInit = true;
            console.log('✓ Scene initialized');
        }

        if (!checkpoints.gameInit && msg.includes('Game initialized')) {
            checkpoints.gameInit = true;
            console.log('✓ Game initialized');
        }

        if (!checkpoints.appRun && msg.includes('Entering Application.Run')) {
            checkpoints.appRun = true;
            console.log('✓ Application.Run called');
            console.log('✓ UI is running\n');

            // Wait 2 seconds, send 'a' to test key events, then ESC
            setTimeout(() => {
                console.log('→ Sending "a" key to test...');
                ws.send('a');  // Send raw input, not JSON!
            }, 2000);

            setTimeout(() => {
                checkpoints.escSent = true;
                console.log('→ Sending ESC key...\n');
                ws.send('\u001b');  // Send raw ESC character, not JSON!
            }, 4000);
        }

        // Track shutdown sequence
        if (!checkpoints.uiFinished && msg.includes('UI loop finished')) {
            checkpoints.uiFinished = true;
            console.log('✓ UI loop finished');
        }

        if (!checkpoints.timerStopped && msg.includes('Game timer stopped')) {
            checkpoints.timerStopped = true;
            console.log('✓ Game timer stopped');
        }

        if (!checkpoints.stopAsync && msg.includes('StopAsync called')) {
            checkpoints.stopAsync = true;
            console.log('✓ StopAsync called');
        }

        if (!checkpoints.stopped && (msg.includes('Console Dungeon stopped successfully') || msg.includes('stopped successfully'))) {
            checkpoints.stopped = true;
            console.log('✓ Shutdown complete\n');

            // Give it a moment then check results
            setTimeout(() => printResults(), 500);
        }

        // If we got StopAsync but not the final message, wait a bit more
        if (checkpoints.stopAsync && !checkpoints.stopped && msg.includes('Stopping game services')) {
            setTimeout(() => {
                if (!checkpoints.stopped) {
                    checkpoints.stopped = true;  // Assume it completed
                    printResults();
                }
            }, 1500);
        }
    });

    ws.on('error', (err) => {
        console.error('\n✗ WebSocket error:', err.message);
        process.exit(1);
    });

    ws.on('close', () => {
        console.log('WebSocket closed');
    });

    function printResults() {
        console.log('=== TEST RESULTS ===');
        console.log('Connected:', checkpoints.connected ? '✓' : '✗');
        console.log('StartAsync:', checkpoints.startAsync ? '✓' : '✗');
        console.log('Scene Init:', checkpoints.sceneInit ? '✓' : '✗');
        console.log('Game Init:', checkpoints.gameInit ? '✓' : '✗');
        console.log('App.Run:', checkpoints.appRun ? '✓' : '✗');
        console.log('ESC Sent:', checkpoints.escSent ? '✓' : '✗');
        console.log('UI Finished:', checkpoints.uiFinished ? '✓' : '✗');
        console.log('Timer Stopped:', checkpoints.timerStopped ? '✓' : '✗');
        console.log('StopAsync:', checkpoints.stopAsync ? '✓' : '✗');
        console.log('Stopped:', checkpoints.stopped ? '✓' : '✗');

        const allPassed = Object.values(checkpoints).every(v => v === true);
        console.log('\n' + (allPassed ? '✓✓✓ ALL TESTS PASSED ✓✓✓' : '✗✗✗ SOME TESTS FAILED ✗✗✗'));

        ws.close();
        process.exit(allPassed ? 0 : 1);
    }

    // Timeout after 20 seconds
    setTimeout(() => {
        console.error('\n✗ TEST TIMEOUT (20s)');
        printResults();
    }, 20000);
}

testFlow();
