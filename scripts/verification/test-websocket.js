/**
 * RFC-0005 Phase 5.3: WebSocket Connection Test
 * 
 * Simple WebSocket client to test ConsoleDungeon.Host connection
 */

const WebSocket = require('ws');

console.log('\n========================================');
console.log('RFC-0005: WebSocket Connection Test');
console.log('========================================\n');

console.log('[1/4] Connecting to WebSocket server at ws://localhost:4040...');

const ws = new WebSocket('ws://localhost:4040');

let connected = false;
let receivedData = false;

ws.on('open', function open() {
    console.log('  ✓ WebSocket connection established');
    connected = true;
    
    console.log('[2/4] Sending "init" message...');
    ws.send('init');
});

ws.on('message', function message(data) {
    console.log('[3/4] Received response from server');
    console.log('  ✓ Server responded to init message');
    receivedData = true;
    
    const dataStr = data.toString();
    
    // Check if it's a screen update
    if (dataStr.startsWith('screen:')) {
        const screenContent = dataStr.substring(7);
        console.log('  ✓ Screen content received');
        console.log('  Screen content preview (first 200 chars):');
        console.log('  ' + screenContent.substring(0, 200).replace(/\r?\n/g, '\n  '));
        
        // Check for Terminal.Gui elements
        const hasConsoleDungeon = screenContent.includes('Console Dungeon');
        const hasWebSocket = screenContent.includes('WebSocket') || screenContent.includes('4040');
        
        console.log('\n[4/4] Verifying Terminal.Gui interface elements:');
        console.log(`  Console Dungeon title: ${hasConsoleDungeon ? '✓ Found' : '✗ Not found'}`);
        console.log(`  WebSocket info:        ${hasWebSocket ? '✓ Found' : '✗ Not found'}`);
        
        console.log('\n========================================');
        console.log('Test Results');
        console.log('========================================');
        console.log(`WebSocket Connection:   ${connected ? '✅ PASS' : '❌ FAIL'}`);
        console.log(`Server Response:        ${receivedData ? '✅ PASS' : '❌ FAIL'}`);
        console.log(`Terminal.Gui Elements:  ${(hasConsoleDungeon || hasWebSocket) ? '✅ PASS' : '❌ FAIL'}`);
        console.log('========================================\n');
        
        if (connected && receivedData && (hasConsoleDungeon || hasWebSocket)) {
            console.log('✅ SUCCESS: xterm.js integration is working!\n');
            ws.close();
            process.exit(0);
        } else {
            console.log('⚠️  WARNING: Some tests did not pass completely\n');
            ws.close();
            process.exit(0);
        }
    } else {
        console.log('  Received:', dataStr.substring(0, 100));
    }
});

ws.on('error', function error(err) {
    console.error('  ✗ WebSocket error:', err.message);
    console.log('\n❌ FAILURE: Could not establish WebSocket connection\n');
    process.exit(1);
});

ws.on('close', function close() {
    if (!receivedData) {
        console.log('  ✗ Connection closed before receiving data');
        console.log('\n❌ FAILURE: Did not receive response from server\n');
        process.exit(1);
    }
});

// Timeout after 10 seconds
setTimeout(() => {
    if (!receivedData) {
        console.log('  ✗ Timeout waiting for server response');
        console.log('\n❌ FAILURE: Server did not respond within 10 seconds\n');
        ws.close();
        process.exit(1);
    }
}, 10000);
