#!/bin/bash

# Docker Startup Helper Script
# This script helps start Docker daemon and check status

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

echo "🐳 Docker Startup Helper"
echo "========================"

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed!"
    print_error "Please install Docker first:"
    print_error "  sudo apt update"
    print_error "  sudo apt install docker.io docker-compose"
    print_error "  sudo usermod -aG docker \$USER"
    print_error "  # Then logout and login again"
    exit 1
fi

# Check if Docker daemon is running
if docker info > /dev/null 2>&1; then
    print_success "Docker daemon is already running!"
    docker --version
    docker info --format "{{.ServerVersion}}"
    exit 0
fi

print_status "Docker daemon is not running. Starting Docker..."

# Try to start Docker daemon
if sudo systemctl start docker 2>/dev/null; then
    print_success "Docker daemon started successfully!"
    
    # Enable Docker to start on boot
    if sudo systemctl enable docker 2>/dev/null; then
        print_success "Docker enabled to start on boot"
    fi
    
    # Wait a moment for Docker to fully start
    sleep 3
    
    # Verify Docker is running
    if docker info > /dev/null 2>&1; then
        print_success "Docker is now running!"
        docker --version
    else
        print_error "Docker started but is not responding properly"
        print_error "Try: sudo systemctl status docker"
        exit 1
    fi
    
else
    print_error "Failed to start Docker daemon"
    print_error "Please try manually:"
    print_error "  sudo systemctl start docker"
    print_error "  sudo systemctl status docker"
    exit 1
fi

print_success "Docker is ready! You can now run:"
print_success "  ./simple-test-setup.sh"