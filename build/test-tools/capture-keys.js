#!/usr/bin/env node
// Capture raw key input sequences to debug arrow key handling
// Usage: node capture-keys.js
//
// This tool reads raw input from stdin in raw mode and logs the exact
// byte sequences received when pressing arrow keys.
//
// Press Ctrl+C to exit.

const readline = require('readline');

console.log('=== Arrow Key Capture Tool ===');
console.log('Press arrow keys to see their raw sequences');
console.log('Press Ctrl+C to exit\n');

// Set raw mode
if (process.stdin.setRawMode) {
  process.stdin.setRawMode(true);
}
process.stdin.resume();
process.stdin.setEncoding('utf8');

process.stdin.on('data', (key) => {
  // Convert to byte array for inspection
  const bytes = Buffer.from(key, 'utf8');
  const hex = Array.from(bytes).map(b => '0x' + b.toString(16).padStart(2, '0')).join(' ');
  const ascii = Array.from(bytes).map(b => {
    if (b === 27) return 'ESC';
    if (b === 13) return 'CR';
    if (b === 10) return 'LF';
    if (b === 127) return 'DEL';
    if (b < 32) return `^${String.fromCharCode(b + 64)}`;
    if (b >= 32 && b < 127) return String.fromCharCode(b);
    return `\\x${b.toString(16)}`;
  }).join(' ');

  // Detect arrow keys
  let keyName = '';
  if (key === '\x1b[A' || key === '\x1bOA') keyName = ' → UP';
  else if (key === '\x1b[B' || key === '\x1bOB') keyName = ' → DOWN';
  else if (key === '\x1b[C' || key === '\x1bOC') keyName = ' → RIGHT';
  else if (key === '\x1b[D' || key === '\x1bOD') keyName = ' → LEFT';
  else if (key === '\x03') {
    console.log('\nExiting...');
    process.exit(0);
  }

  console.log(`Bytes: ${hex}`);
  console.log(`ASCII: ${ascii}${keyName}`);
  console.log('---');
});

process.on('SIGINT', () => {
  console.log('\nExiting...');
  process.exit(0);
});
