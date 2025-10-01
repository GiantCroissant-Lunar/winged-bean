/**
 * RecordingManager - Manages asciinema recording sessions
 *
 * Allows dynamic start/stop of recordings controlled by Terminal.Gui
 * via OSC escape sequences (F9/F10 keyboard shortcuts)
 */

const fs = require('fs');
const path = require('path');

class RecordingManager {
  constructor(outputDir) {
    this.outputDir = outputDir;
    this.currentRecording = null;
    this.recordingCount = 0;

    // Ensure output directory exists
    if (!fs.existsSync(outputDir)) {
      fs.mkdirSync(outputDir, { recursive: true });
    }
  }

  /**
   * Start a new recording session
   * @returns {string} Filename of the recording
   */
  startRecording() {
    if (this.currentRecording) {
      console.log('Recording already in progress');
      return null;
    }

    this.recordingCount++;
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-').substring(0, 19);
    const filename = `session-${this.recordingCount}-${timestamp}.cast`;
    const filepath = path.join(this.outputDir, filename);

    this.currentRecording = {
      filename,
      filepath,
      stream: fs.createWriteStream(filepath),
      startTime: Date.now(),
      frameCount: 0,
    };

    // Write asciinema v2 header
    const header = {
      version: 2,
      width: 80,
      height: 24,
      timestamp: Math.floor(Date.now() / 1000),
      title: `Terminal.Gui PTY Session ${this.recordingCount}`,
    };
    this.currentRecording.stream.write(JSON.stringify(header) + '\n');

    console.log(`[RecordingManager] Recording started: ${filename}`);
    return filename;
  }

  /**
   * Write data to current recording
   * @param {string|Buffer} data - Data to write
   */
  writeData(data) {
    if (!this.currentRecording) return;

    const timestamp = (Date.now() - this.currentRecording.startTime) / 1000;
    const dataStr = typeof data === 'string' ? data : data.toString('utf-8');
    const event = [timestamp, 'o', dataStr];

    this.currentRecording.stream.write(JSON.stringify(event) + '\n');
    this.currentRecording.frameCount++;
  }

  /**
   * Stop current recording session
   * @returns {Promise<Object>} Recording info {filename, frameCount, duration}
   */
  stopRecording() {
    if (!this.currentRecording) {
      console.log('No recording in progress');
      return null;
    }

    const duration = (Date.now() - this.currentRecording.startTime) / 1000;
    const info = {
      filename: this.currentRecording.filename,
      filepath: this.currentRecording.filepath,
      frameCount: this.currentRecording.frameCount,
      duration: duration.toFixed(2),
    };

    // Close the stream and wait for it to finish
    this.currentRecording.stream.end();

    // Wait for stream to finish writing
    return new Promise((resolve) => {
      this.currentRecording.stream.on('finish', () => {
        console.log(`[RecordingManager] Recording stopped: ${info.filename}`);
        console.log(`[RecordingManager] Frames: ${info.frameCount}, Duration: ${info.duration}s`);
        this.currentRecording = null;
        resolve(info);
      });
    });
  }

  /**
   * Check if recording is active
   * @returns {boolean}
   */
  isRecording() {
    return this.currentRecording !== null;
  }

  /**
   * Get current recording info
   * @returns {Object|null}
   */
  getCurrentRecording() {
    if (!this.currentRecording) return null;

    return {
      filename: this.currentRecording.filename,
      frameCount: this.currentRecording.frameCount,
      duration: ((Date.now() - this.currentRecording.startTime) / 1000).toFixed(2),
    };
  }
}

module.exports = RecordingManager;
