#!/usr/bin/env node
// Verify an asciinema .cast file moved the player right and down
// Prefers explicit lines like `POS x=<X> y=<Y>` emitted by the app,
// and falls back to locating '@' in frames if no POS lines are present.
// Usage: node verify-cast.js /path/to/record.cast

const fs = require('fs');

function loadCast(p) {
  const txt = fs.readFileSync(p, 'utf8');
  const lines = txt.split(/\r?\n/).filter(Boolean);
  // First line is header JSON; subsequent lines are [time, type, data]
  const header = JSON.parse(lines[0]);
  const frames = lines.slice(1).map(l => JSON.parse(l));
  return { header, frames };
}

function parsePosLines(frames) {
  const positions = [];
  for (const f of frames) {
    const [, type, data] = f;
    if (type !== 'o' || typeof data !== 'string') continue;
    const lines = data.split('\n');
    for (const line of lines) {
      const m = line.match(/\bPOS\s+x=(\-?\d+)\s+y=(\-?\d+)/);
      if (m) positions.push({ x: parseInt(m[1], 10), y: parseInt(m[2], 10) });
    }
  }
  return positions;
}

function findLastScreen(frames) {
  // Accumulate text output; last text blob is the final screen
  let last = '';
  for (const f of frames) {
    const [, type, data] = f;
    if (type === 'o' && typeof data === 'string') last = data;
  }
  return last;
}

function locateAt(screen) {
  const rows = screen.split('\
');
  let pos = null;
  for (let r = 0; r < rows.length; r++) {
    const c = rows[r].indexOf('@');
    if (c >= 0) { pos = { row: r, col: c }; }
  }
  return pos;
}

function main() {
  const file = process.argv[2];
  if (!file) { console.error('cast path required'); process.exit(2); }
  const { frames } = loadCast(file);
  // First attempt: POS lines
  const pos = parsePosLines(frames);
  if (pos.length >= 2) {
    const first = { x: pos[0].x, y: pos[0].y };
    const last = { x: pos[pos.length - 1].x, y: pos[pos.length - 1].y };
    const movedRight = last.x > first.x;
    const movedDown = last.y > first.y;
    console.log(JSON.stringify({ mode: 'POS', first, last, movedRight, movedDown }));
    if (!movedRight) { console.error('Did not move right'); process.exit(4); }
    if (!movedDown) { console.error('Did not move down'); process.exit(5); }
    return;
  }
  // Fallback: locate '@' in full-screen frames
  let first = null, last = null;
  let screen = '';
  for (const f of frames) {
    const [, type, data] = f;
    if (type !== 'o' || typeof data !== 'string') continue;
    screen = data;
    const p = locateAt(screen);
    if (p) {
      if (!first) first = p; else last = p;
    }
  }
  if (!first || !last) {
    console.error('Could not locate POS or @ in frames');
    process.exit(3);
  }
  const movedRight = last.col > first.col;
  const movedDown = last.row > first.row;
  console.log(JSON.stringify({ mode: '@', first, last, movedRight, movedDown }));
  if (!movedRight) {
    console.error('Did not move right');
    process.exit(4);
  }
  if (!movedDown) {
    console.error('Did not move down');
    process.exit(5);
  }
}

if (require.main === module) main();
