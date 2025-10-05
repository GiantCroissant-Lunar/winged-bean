const { execSync } = require("child_process");
const path = require("path");

/**
 * Gets the current semantic version from GitVersion with fallback
 * @returns {string} Semantic version string (e.g., "0.1.0-dev+abc1234")
 */
function getVersion() {
  try {
    // Try to use GitVersion
    const version = execSync("dotnet gitversion /nofetch /showvariable SemVer", {
      encoding: "utf-8",
      stdio: ["pipe", "pipe", "pipe"],
    }).trim();

    if (version) {
      return version;
    }
  } catch {
    // Fall through to fallback
  }

  // Fallback: use git commit hash
  try {
    const commitHash = execSync("git rev-parse --short HEAD", {
      encoding: "utf-8",
      stdio: ["pipe", "pipe", "pipe"],
    }).trim();

    if (commitHash) {
      return `0.1.0-dev+${commitHash}`;
    }
  } catch {
    // Fall through to final fallback
  }

  // Final fallback
  return "0.1.0-dev+unknown";
}

/**
 * Gets the versioned artifacts directory path for a given component
 * @param {string} component - Component name (dotnet, web, pty)
 * @param {string} subdir - Subdirectory name (logs, recordings, etc.)
 * @returns {string} Full path to the artifacts directory
 */
function getArtifactsPath(component, subdir) {
  const version = getVersion();
  const repoRoot = findRepositoryRoot();

  if (repoRoot) {
    return path.join(repoRoot, "build", "_artifacts", `v${version}`, component, subdir);
  }

  // Fallback: use relative path
  return path.join(process.cwd(), "build", "_artifacts", `v${version}`, component, subdir);
}

/**
 * Finds the repository root by walking up the directory tree looking for .git
 * @returns {string|null} Repository root path or null if not found
 */
function findRepositoryRoot() {
  let currentDir = process.cwd();

  while (currentDir) {
    const gitPath = path.join(currentDir, ".git");
    try {
      const fs = require("fs");
      if (fs.existsSync(gitPath)) {
        return currentDir;
      }
    } catch {
      // Continue searching
    }

    const parent = path.dirname(currentDir);
    if (parent === currentDir) {
      break;
    }
    currentDir = parent;
  }

  return null;
}

module.exports = {
  getVersion,
  getArtifactsPath,
  findRepositoryRoot,
};
