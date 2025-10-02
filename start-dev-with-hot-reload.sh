#!/bin/bash

# BTCPay Server Development with Hot Reload
# This script starts BTCPay Server with hot reload for development

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

print_status "Starting BTCPay Server with hot reload..."

# Check if test environment is running
if ! nc -z localhost 43782 2>/dev/null; then
    print_error "Bitcoin node is not running. Please run ./test-environment-setup.sh first"
    exit 1
fi

# Navigate to BTCPayServer directory
cd BTCPayServer

print_status "Installing development certificate..."
# Install development HTTPS certificate
dotnet dev-certs https --trust

print_status "Starting BTCPay Server with hot reload..."
print_warning "This will start the server with hot reload enabled."
print_warning "Any changes to .cs files will automatically restart the server."
print_warning "Press Ctrl+C to stop the server."

# Start with hot reload using dotnet watch
dotnet watch run --launch-profile Bitcoin

print_success "BTCPay Server stopped."