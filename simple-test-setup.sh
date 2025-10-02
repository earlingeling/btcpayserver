#!/bin/bash

# Simple BTCPay Server Test Environment Setup
# This script handles docker-compose version compatibility issues

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

echo "🚀 BTCPay Server Simple Test Setup"
echo "=================================="

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker daemon is running
if ! docker info > /dev/null 2>&1; then
    # Try with sudo first
    if sudo docker info > /dev/null 2>&1; then
        print_warning "Docker daemon is running but requires sudo."
        print_warning "This means your user needs to be in the docker group."
        print_warning "Run: ./fix-docker-permissions.sh"
        print_warning "Or continue with sudo (not recommended for development)"
        echo ""
        read -p "Continue with sudo? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            print_error "Please fix Docker permissions first:"
            print_error "  ./fix-docker-permissions.sh"
            exit 1
        fi
        COMPOSE_CMD="sudo docker-compose"
    else
        print_error "Docker daemon is not running!"
        print_error "Please start Docker first:"
        print_error "  sudo systemctl start docker"
        print_error "  sudo systemctl enable docker"
        print_error ""
        print_error "Or if you're using Docker Desktop, start it from the applications menu."
        exit 1
    fi
fi

# Check docker-compose version
if command -v docker-compose &> /dev/null; then
    COMPOSE_CMD="docker-compose"
    COMPOSE_VERSION=$(docker-compose --version | cut -d' ' -f3 | cut -d',' -f1)
    print_success "Found docker-compose version $COMPOSE_VERSION"
elif docker compose version &> /dev/null; then
    COMPOSE_CMD="docker compose"
    print_success "Found docker compose (plugin version)"
else
    print_error "Neither 'docker-compose' nor 'docker compose' found!"
    print_error "Please install docker-compose:"
    print_error "  sudo apt install docker-compose"
    exit 1
fi

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK is not installed. Please install .NET 8 SDK first."
    exit 1
fi

print_status "Starting BTCPay Server test environment..."

# Navigate to tests directory
cd BTCPayServer.Tests

# Pull images first
print_status "Pulling Docker images..."
$COMPOSE_CMD pull

# Start services
print_status "Starting services..."
$COMPOSE_CMD up -d

print_status "Waiting for services to initialize..."
sleep 15

# Check service status
print_status "Checking service status..."

services=("bitcoind:43782" "nbxplorer:32838" "postgres:39372")
for service in "${services[@]}"; do
    IFS=':' read -r name port <<< "$service"
    if nc -z localhost $port 2>/dev/null; then
        print_success "$name is running on port $port"
    else
        print_warning "$name might not be ready yet on port $port"
    fi
done

# Generate initial blocks
print_status "Generating initial Bitcoin blocks..."
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
echo "  • Bitcoin RPC: localhost:43782"
echo "  • NBXplorer: http://localhost:32838"
echo "  • PostgreSQL: localhost:39372"
echo "  • Merchant LND: http://localhost:35531"
echo "  • Customer LND: http://localhost:35532"
echo ""
echo "🔧 Next steps:"
echo "  1. Run: ./start-dev-with-hot-reload.sh"
echo "  2. Open: http://localhost:14142"
echo "  3. Register admin account"
echo "  4. Start developing!"
echo ""
echo "📚 Useful commands:"
echo "  • Stop services: cd BTCPayServer.Tests && $COMPOSE_CMD down"
echo "  • View logs: cd BTCPayServer.Tests && $COMPOSE_CMD logs -f"
echo "  • Reset environment: cd BTCPayServer.Tests && $COMPOSE_CMD down --volumes"