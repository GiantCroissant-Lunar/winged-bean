#!/usr/bin/env node
// Connects to the PTY WebSocket (port 4041) and sends a sequence of keys
// to validate input handling end-to-end via PTY + Terminal.Gui app.

const WebSocket = require('ws');

const url = process.argv[2] || 'ws://localhost:4041';
console.log('Connecting to', url);

const ws = new WebSocket(url);

function sleep(ms){ return new Promise(r => setTimeout(r, ms)); }

ws.on('open', async () => {
  console.log('Connected. Sending resize then input sequence...');
  // Resize first so the app sizes deterministically
  ws.send(JSON.stringify({ type: 'resize', cols: 80, rows: 24 }));
  await sleep(200);

  // Wake up
  ws.send('\r');
  await sleep(150);

  const seq = [
    { label: 'ArrowUp', data: '\u001b[A' },
    { label: 'ArrowLeft', data: '\u001b[D' },
    { label: 'ArrowDown', data: '\u001b[B' },
    { label: 'ArrowRight', data: '\u001b[C' },
    { label: 'W', data: 'W' },
    { label: 'A', data: 'A' },
    { label: 'S', data: 'S' },
    { label: 'D', data: 'D' },
    { label: 'M', data: 'M' },
    { label: 'Ctrl+C', data: '\u0003' },
    // Give the app a moment in case Ctrl+C triggers quit
    { label: 'Esc', data: '\u001b' },
    { label: 'q', data: 'q' }
  ];

  for (const { label, data } of seq) {
    console.log('Sending', label, JSON.stringify(data));
    ws.send(data);
    await sleep(120);
  }

  console.log('Done sending inputs; closing soon...');
  await sleep(500);
  ws.close(1000, 'done');
});

ws.on('message', (data) => {
  try {
    const text = Buffer.isBuffer(data) ? data.toString('utf8') : String(data);
    // Preview only
    const preview = text.replace(/\x1b\[[0-9;?]*[A-Za-z]/g, '').slice(0, 120);
    if (preview.trim()) {
      console.log('[preview]', JSON.stringify(preview));
    }
  } catch {}
});

ws.on('close', (code, reason) => {
  console.log('Closed:', code, reason.toString());
  process.exit(0);
});

ws.on('error', (err) => {
  console.error('WS error:', err.message);
  process.exit(1);
});

