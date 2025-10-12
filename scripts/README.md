# Scripts Directory

Utility scripts for development, testing, and project management.

## Contents

### UI and Testing Scripts
- **`check-console.sh`** - Diagnostic script to check Terminal.Gui console state and detect headless mode issues
- **`run-ui.sh`** - Launch the Terminal.Gui UI application
- **`test-ui.sh`** - Test UI functionality

### Project Management
- **`create-github-issues.sh`** - Automate GitHub issue creation
- **`organize-chat-history.sh`** - Organize chat history files
- **`organize-chat-history-fixed.sh`** - Fixed version of chat history organizer

### Recording and Verification
- **`record-terminal-gui-session.sh`** - Record Terminal.Gui sessions using asciinema
- **`verify-runtime-artifacts.sh`** - Verify build artifacts and runtime dependencies

### Subdirectories
- **`hooks/`** - Git hooks
- **`verification/`** - Verification and validation scripts

## Usage

Most scripts are executable and can be run directly:

```bash
# Check console state
./scripts/check-console.sh

# Run the UI
./scripts/run-ui.sh

# Test UI functionality
./scripts/test-ui.sh
```

## Root-Level Configuration Files

Note: The following files remain at the project root for functional reasons:
- `package.json` & `package-lock.json` - WebSocket dependency for PM2
- `ecosystem.config.js` - PM2 process management configuration
