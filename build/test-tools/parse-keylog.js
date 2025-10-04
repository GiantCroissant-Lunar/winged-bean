#!/usr/bin/env node
// Parse console-dungeon-*.log to extract and analyze KeyDown events
// Usage: node parse-keylog.js /path/to/console-dungeon-*.log

const fs = require('fs');

const logPath = process.argv[2];
if (!logPath) {
  console.error('Usage: node parse-keylog.js <log-file>');
  process.exit(1);
}

const content = fs.readFileSync(logPath, 'utf8');
const lines = content.split(/\r?\n/);

console.log('=== KeyDown Events ===\n');

const keyEvents = [];
for (const line of lines) {
  // Match: KeyDown: KeyCode=XXX, Rune=YYY, AsRune.Value=ZZZ
  const match = line.match(/KeyDown: KeyCode=(\w+), Rune=([^,]+), AsRune\.Value=(\d+)/);
  if (match) {
    const [, keyCode, rune, runeValue] = match;
    keyEvents.push({ keyCode, rune, runeValue: parseInt(runeValue, 10), line });
  }
}

if (keyEvents.length === 0) {
  console.log('No KeyDown events found in log');
  process.exit(0);
}

// Group by keyCode
const grouped = {};
for (const evt of keyEvents) {
  const key = `${evt.keyCode} (rune=${evt.rune})`;
  if (!grouped[key]) grouped[key] = [];
  grouped[key].push(evt);
}

console.log('Summary:');
for (const [key, events] of Object.entries(grouped)) {
  console.log(`  ${key}: ${events.length} events`);
}

console.log('\n=== Arrow Key Analysis ===\n');

const arrowKeys = keyEvents.filter(e =>
  e.keyCode.includes('Cursor') ||
  (e.runeValue >= 65 && e.runeValue <= 68) // A-D
);

if (arrowKeys.length === 0) {
  console.log('No arrow key events detected');
} else {
  for (const evt of arrowKeys) {
    let direction = '';
    if (evt.keyCode === 'CursorUp') direction = '↑ UP';
    else if (evt.keyCode === 'CursorDown') direction = '↓ DOWN';
    else if (evt.keyCode === 'CursorLeft') direction = '← LEFT';
    else if (evt.keyCode === 'CursorRight') direction = '→ RIGHT';
    else if (evt.runeValue === 65) direction = '(rune A - may be UP)';
    else if (evt.runeValue === 66) direction = '(rune B - may be DOWN)';
    else if (evt.runeValue === 67) direction = '(rune C - may be RIGHT)';
    else if (evt.runeValue === 68) direction = '(rune D - may be LEFT)';

    console.log(`${direction}: KeyCode=${evt.keyCode}, Rune=${evt.rune}, Value=${evt.runeValue}`);
  }
}

console.log('\n=== Game Input Events ===\n');

const gameInputs = lines.filter(l => l.includes('Game input received:'));
if (gameInputs.length === 0) {
  console.log('No game inputs processed');
} else {
  for (const line of gameInputs) {
    const match = line.match(/Game input received: (\w+)/);
    if (match) {
      console.log(`  ${match[1]}`);
    }
  }
}

console.log('\n=== Position Changes ===\n');

const positions = [];
for (const line of lines) {
  const match = line.match(/Emitted POS x=(-?\d+) y=(-?\d+)/);
  if (match) {
    positions.push({ x: parseInt(match[1], 10), y: parseInt(match[2], 10) });
  }
}

if (positions.length < 2) {
  console.log('Not enough position samples');
} else {
  const first = positions[0];
  const last = positions[positions.length - 1];
  console.log(`First position: x=${first.x}, y=${first.y}`);
  console.log(`Last position:  x=${last.x}, y=${last.y}`);
  console.log(`Delta: dx=${last.x - first.x}, dy=${last.y - first.y}`);

  if (last.x > first.x) console.log('✓ Moved RIGHT');
  if (last.x < first.x) console.log('✓ Moved LEFT');
  if (last.y > first.y) console.log('✓ Moved DOWN');
  if (last.y < first.y) console.log('✓ Moved UP');
}
