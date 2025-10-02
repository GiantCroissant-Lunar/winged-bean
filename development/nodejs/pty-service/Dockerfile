# Multi-stage Dockerfile for PTY service + .NET runtime + Terminal.Gui console app
FROM node:20-slim AS node-base

# Install .NET 8 SDK and runtime
RUN apt-get update && apt-get install -y \
    wget \
    ca-certificates \
    && wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet-install.sh \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install Node.js dependencies
RUN npm ci --only=production

# Copy application code
COPY . .

# Copy Terminal.Gui console app (if built separately)
# Adjust path based on your project structure
# COPY ../../dotnet/console-dungeon/bin/Release/net8.0/ ./console-app/

# Expose PTY service port
EXPOSE 4041

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:4041/health', (r) => {process.exit(r.statusCode === 200 ? 0 : 1)})"

# Start PTY service with PM2
CMD ["npm", "start"]
