#!/bin/bash

# Docker Permissions Fix Script
# This script helps fix Docker group permissions

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

echo "🔧 Docker Permissions Fix"
echo "========================"

# Check if user is in docker group
if groups | grep -q docker; then
    print_success "User is in docker group"
else
    print_warning "User is not in docker group. Adding to docker group..."
    sudo usermod -aG docker $USER
    print_success "User added to docker group"
fi

# Check Docker socket permissions
if [ -S /var/run/docker.sock ]; then
    SOCKET_PERMS=$(ls -la /var/run/docker.sock | awk '{print $1}')
    SOCKET_GROUP=$(ls -la /var/run/docker.sock | awk '{print $4}')
    
    if [ "$SOCKET_GROUP" = "docker" ]; then
        print_success "Docker socket has correct group permissions"
    else
        print_warning "Docker socket group is '$SOCKET_GROUP', should be 'docker'"
    fi
else
    print_error "Docker socket not found at /var/run/docker.sock"
fi

# Test Docker access
print_status "Testing Docker access..."

if docker info > /dev/null 2>&1; then
    print_success "Docker access works without sudo!"
    docker --version
else
    print_warning "Docker access requires sudo. This means:"
    print_warning "1. You need to logout and login again, OR"
    print_warning "2. Start a new shell session, OR"
    print_warning "3. Use 'newgrp docker' to activate the group"
    echo ""
    print_status "Quick fix - run this command:"
    print_status "newgrp docker"
    echo ""
    print_status "Or logout and login again to activate the docker group membership."
fi

echo ""
print_status "Alternative: Use sudo for Docker commands"
print_status "The test environment will work with sudo, but it's better to fix permissions."