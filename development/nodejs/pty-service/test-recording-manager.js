/**
 * Test suite for RecordingManager
 * Verifies dynamic recording start/stop functionality
 */

const RecordingManager = require('./recording-manager');
const fs = require('fs');
const path = require('path');

// Test output directory
const TEST_RECORDINGS_DIR = path.join(__dirname, 'test-recordings');

console.log('='.repeat(60));
console.log('RecordingManager Test Suite');
console.log('='.repeat(60));
console.log('');

// Clean up test directory
if (fs.existsSync(TEST_RECORDINGS_DIR)) {
  fs.rmSync(TEST_RECORDINGS_DIR, { recursive: true });
}
fs.mkdirSync(TEST_RECORDINGS_DIR, { recursive: true });

let testsPassed = 0;
let testsFailed = 0;

function assert(condition, message) {
  if (condition) {
    console.log(`✅ PASS: ${message}`);
    testsPassed++;
  } else {
    console.log(`❌ FAIL: ${message}`);
    testsFailed++;
  }
}

// Test 1: RecordingManager initialization
console.log('Test 1: RecordingManager Initialization');
console.log('-'.repeat(60));
const manager = new RecordingManager(TEST_RECORDINGS_DIR);
assert(manager !== null, 'RecordingManager created');
assert(!manager.isRecording(), 'Initially not recording');
assert(manager.getCurrentRecording() === null, 'No current recording');
console.log('');

// Test 2: Start recording
console.log('Test 2: Start Recording');
console.log('-'.repeat(60));
const filename1 = manager.startRecording();
assert(filename1 !== null, 'Recording started successfully');
assert(manager.isRecording(), 'Manager reports recording active');
assert(filename1.endsWith('.cast'), 'Filename has .cast extension');
assert(filename1.includes('session-1'), 'Filename includes session number');

const currentRecording = manager.getCurrentRecording();
assert(currentRecording !== null, 'Current recording info available');
assert(currentRecording.filename === filename1, 'Filename matches');
assert(currentRecording.frameCount === 0, 'Initial frame count is 0');
console.log('');

// Test 3: Write data to recording
console.log('Test 3: Write Data to Recording');
console.log('-'.repeat(60));
manager.writeData('Test data 1\n');
manager.writeData('Test data 2\n');
manager.writeData('Test data 3\n');

const recording = manager.getCurrentRecording();
assert(recording.frameCount === 3, 'Frame count is 3 after 3 writes');
console.log('');

// Test 4: Stop recording (async)
console.log('Test 4: Stop Recording');
console.log('-'.repeat(60));

(async () => {
  const info1 = await manager.stopRecording();
  assert(info1 !== null, 'Recording stopped successfully');
  assert(info1.filename === filename1, 'Stopped recording filename matches');
  assert(info1.frameCount === 3, 'Frame count is 3');
  assert(parseFloat(info1.duration) >= 0, 'Duration is valid (>= 0)');
  assert(!manager.isRecording(), 'Manager reports not recording');
  console.log('');

  // Test 5: Verify .cast file created
  console.log('Test 5: Verify .cast File Created');
  console.log('-'.repeat(60));
  const filepath1 = path.join(TEST_RECORDINGS_DIR, filename1);
  assert(fs.existsSync(filepath1), '.cast file exists');

  const fileContent = fs.readFileSync(filepath1, 'utf-8');
  const lines = fileContent.trim().split('\n');
  assert(lines.length === 4, 'File has 4 lines (1 header + 3 data)');

  // Verify header
  const header = JSON.parse(lines[0]);
  assert(header.version === 2, 'Header version is 2');
  assert(header.width === 80, 'Header width is 80');
  assert(header.height === 24, 'Header height is 24');

  // Verify data frames
  const frame1 = JSON.parse(lines[1]);
  assert(Array.isArray(frame1), 'Frame 1 is an array');
  assert(frame1.length === 3, 'Frame has 3 elements [timestamp, type, data]');
  assert(frame1[1] === 'o', 'Frame type is "o" (output)');
  assert(frame1[2] === 'Test data 1\n', 'Frame data matches');
  console.log('');

  // Test 6: Multiple recording sessions
  console.log('Test 6: Multiple Recording Sessions');
  console.log('-'.repeat(60));
  const filename2 = manager.startRecording();
  assert(filename2 !== null, 'Second recording started');
  assert(filename2 !== filename1, 'Second filename is different');
  assert(filename2.includes('session-2'), 'Second session number is 2');

  manager.writeData('Session 2 data\n');
  const info2 = await manager.stopRecording();
  assert(info2.frameCount === 1, 'Second recording has 1 frame');
  console.log('');

  // Test 7: Cannot start recording while already recording
  console.log('Test 7: Cannot Start While Recording');
  console.log('-'.repeat(60));
  manager.startRecording();
  const result = manager.startRecording();
  assert(result === null, 'Cannot start second recording while first is active');
  await manager.stopRecording();
  console.log('');

  // Test 8: Cannot stop when not recording
  console.log('Test 8: Cannot Stop When Not Recording');
  console.log('-'.repeat(60));
  const stopResult = manager.stopRecording();
  assert(stopResult === null, 'Cannot stop when not recording');
  console.log('');

  // Test 9: Write data when not recording (should be ignored)
  console.log('Test 9: Write Data When Not Recording');
  console.log('-'.repeat(60));
  manager.writeData('This should be ignored\n');
  assert(!manager.isRecording(), 'Still not recording');
  console.log('');

  // Test 10: Verify file sizes
  console.log('Test 10: Verify File Sizes');
  console.log('-'.repeat(60));
  const files = fs.readdirSync(TEST_RECORDINGS_DIR);
  assert(files.length === 3, 'Three .cast files created (including test 7)');
  files.forEach(file => {
    const stats = fs.statSync(path.join(TEST_RECORDINGS_DIR, file));
    assert(stats.size > 0, `${file} has non-zero size (${stats.size} bytes)`);
  });
  console.log('');

  // Summary
  console.log('='.repeat(60));
  console.log('Test Summary');
  console.log('='.repeat(60));
  console.log(`Total tests: ${testsPassed + testsFailed}`);
  console.log(`✅ Passed: ${testsPassed}`);
  console.log(`❌ Failed: ${testsFailed}`);
  console.log('');

  if (testsFailed === 0) {
    console.log('🎉 All tests passed!');
    console.log('');
    console.log('Test recordings created in:', TEST_RECORDINGS_DIR);
    console.log('You can play them with:');
    console.log(`  asciinema play ${path.join(TEST_RECORDINGS_DIR, files[0])}`);
    console.log('');
    process.exit(0);
  } else {
    console.log('❌ Some tests failed!');
    process.exit(1);
  }
})().catch(err => {
  console.error('Test error:', err);
  process.exit(1);
});
