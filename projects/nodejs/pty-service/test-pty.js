const pty = require("node-pty");

console.log("Testing node-pty directly...");

// Test 1: Simple command
console.log("\n=== Test 1: Echo command ===");
const echoProcess = pty.spawn("echo", ["Hello PTY World!"], {
  name: "xterm-color",
  cols: 80,
  rows: 24,
  env: process.env,
});

echoProcess.onData((data) => {
  console.log("Echo output:", JSON.stringify(data));
});

echoProcess.onExit((exitCode, signal) => {
  console.log(`Echo process exited with code ${exitCode}, signal ${signal}`);

  // Test 2: Bash shell
  console.log("\n=== Test 2: Bash shell ===");
  const bashProcess = pty.spawn("bash", [], {
    name: "xterm-color",
    cols: 80,
    rows: 24,
    env: {
      ...process.env,
      TERM: "xterm-color",
      SHELL: "/bin/bash",
    },
  });

  bashProcess.onData((data) => {
    console.log("Bash output:", JSON.stringify(data));
  });

  bashProcess.onExit((exitCode, signal) => {
    console.log(`Bash process exited with code ${exitCode}, signal ${signal}`);

    // Test 3: Direct dotnet test
    console.log("\n=== Test 3: Dotnet version ===");
    const dotnetProcess = pty.spawn("dotnet", ["--version"], {
      name: "xterm-color",
      cols: 80,
      rows: 24,
      env: process.env,
    });

    dotnetProcess.onData((data) => {
      console.log("Dotnet output:", JSON.stringify(data));
    });

    dotnetProcess.onExit((exitCode, signal) => {
      console.log(
        `Dotnet process exited with code ${exitCode}, signal ${signal}`,
      );
      console.log("\nPTY tests completed.");
      process.exit(0);
    });
  });

  // Send a command to bash after a short delay
  setTimeout(() => {
    console.log("Sending command to bash...");
    bashProcess.write('echo "Hello from bash"\r');
    bashProcess.write("exit\r");
  }, 500);
});

// Exit after 10 seconds if nothing happens
setTimeout(() => {
  console.log("Tests timed out after 10 seconds");
  process.exit(1);
}, 10000);
