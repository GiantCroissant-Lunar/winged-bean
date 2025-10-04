#!/usr/bin/env node
// Verify movement by parsing "Emitted POS x=.. y=.." lines in app log
// Usage: node verify-log-pos.js /path/to/console-dungeon-*.log

const fs = require('fs');
const path = process.argv[2];
if (!path) { console.error('log path required'); process.exit(2); }
const txt = fs.readFileSync(path, 'utf8');
const lines = txt.split(/\r?\n/);
const pos = [];
for (const line of lines) {
  const m = line.match(/Emitted\s+POS\s+x=(\-?\d+)\s+y=(\-?\d+)/);
  if (m) pos.push({ x: parseInt(m[1], 10), y: parseInt(m[2], 10) });
}
if (pos.length < 2) {
  console.error('Not enough POS samples found');
  process.exit(3);
}
const first = pos[0];
const last = pos[pos.length - 1];
const movedRight = last.x > first.x;
const movedDown = last.y > first.y;
console.log(JSON.stringify({ first, last, movedRight, movedDown }));
if (!movedRight) { console.error('Did not move right'); process.exit(4); }
if (!movedDown) { console.error('Did not move down'); process.exit(5); }
