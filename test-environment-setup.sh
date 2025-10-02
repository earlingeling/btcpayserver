#!/bin/bash

# BTCPay Server Test Environment Setup Script
# This script sets up a complete test environment with hot reload capabilities

set -e

echo "🚀 Setting up BTCPay Server Test Environment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
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

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    print_error "Docker daemon is not running. Please start Docker first:"
    print_error "  sudo systemctl start docker"
    print_error "  sudo systemctl enable docker"
    print_error "Or start Docker Desktop if you're using it."
    exit 1
fi

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK is not installed. Please install .NET 8 SDK first."
    exit 1
fi

print_status "Starting Docker services for test environment..."

# Start the test environment services
cd BTCPayServer.Tests
docker-compose up -d

print_status "Waiting for services to be ready..."
sleep 10

# Check if services are running
print_status "Checking service status..."

services=("bitcoind:43782" "nbxplorer:32838" "postgres:39372" "merchant_lnd:35531" "customer_lnd:35532")
for service in "${services[@]}"; do
    IFS=':' read -r name port <<< "$service"
    if nc -z localhost $port 2>/dev/null; then
        print_success "$name is running on port $port"
    else
        print_warning "$name might not be ready yet on port $port"
    fi
done

print_status "Setting up Lightning Network channels..."
# Wait a bit more for Lightning nodes to be ready
sleep 15

# Setup Lightning channels
if [ -f "docker-lightning-channel-setup.sh" ]; then
    chmod +x docker-lightning-channel-setup.sh
    ./docker-lightning-channel-setup.sh
    print_success "Lightning channels configured"
else
    print_warning "Lightning channel setup script not found, channels may need manual setup"
fi

print_status "Generating initial Bitcoin blocks..."
# Generate some initial blocks for testing
if [ -f "docker-bitcoin-generate.sh" ]; then
    chmod +x docker-bitcoin-generate.sh
    ./docker-bitcoin-generate.sh 100
    print_success "Generated 100 Bitcoin blocks"
else
    print_warning "Bitcoin generation script not found"
fi

print_success "Test environment setup complete!"
echo ""
echo "🌐 Services available:"
echo "  • BTCPay Server: http://localhost:14142 (when running)"
echo "  • Bitcoin RPC: localhost:43782"
echo "  • NBXplorer: http://localhost:32838"
echo "  • PostgreSQL: localhost:39372"
echo "  • Merchant LND: http://localhost:35531"
echo "  • Customer LND: http://localhost:35532"
echo "  • Tor SOCKS: localhost:9050"
echo ""
echo "🔧 Next steps:"
echo "  1. Run: ./start-dev-with-hot-reload.sh"
echo "  2. Open: http://localhost:14142"
echo "  3. Register admin account"
echo "  4. Start developing with hot reload!"
echo ""
echo "📚 Useful commands:"
echo "  • Stop services: docker-compose down"
echo "  • View logs: docker-compose logs -f"
echo "  • Reset environment: docker-compose down --volumes"