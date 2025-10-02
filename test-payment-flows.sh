#!/bin/bash

# BTCPay Server Payment Flow Testing Script
# This script provides utilities to test various payment scenarios

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

# Function to send Bitcoin to an address
send_bitcoin() {
    local address=$1
    local amount=${2:-0.001}
    
    print_status "Sending $amount BTC to $address"
    
    if [ -f "BTCPayServer.Tests/docker-bitcoin-cli.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-bitcoin-cli.sh
        BTCPayServer.Tests/docker-bitcoin-cli.sh sendtoaddress "$address" "$amount"
        print_success "Bitcoin sent successfully"
    else
        print_error "Bitcoin CLI script not found"
        return 1
    fi
}

# Function to generate blocks
generate_blocks() {
    local count=${1:-1}
    
    print_status "Generating $count blocks..."
    
    if [ -f "BTCPayServer.Tests/docker-bitcoin-generate.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-bitcoin-generate.sh
        BTCPayServer.Tests/docker-bitcoin-generate.sh "$count"
        print_success "Generated $count blocks"
    else
        print_error "Bitcoin generation script not found"
        return 1
    fi
}

# Function to get Bitcoin balance
get_balance() {
    print_status "Getting Bitcoin balance..."
    
    if [ -f "BTCPayServer.Tests/docker-bitcoin-cli.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-bitcoin-cli.sh
        BTCPayServer.Tests/docker-bitcoin-cli.sh getbalance
    else
        print_error "Bitcoin CLI script not found"
        return 1
    fi
}

# Function to create a Lightning invoice
create_lightning_invoice() {
    local amount=${1:-1000}  # Amount in satoshis
    local description=${2:-"Test invoice"}
    
    print_status "Creating Lightning invoice for $amount sats..."
    
    if [ -f "BTCPayServer.Tests/docker-customer-lightning-cli.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-customer-lightning-cli.sh
        BTCPayServer.Tests/docker-customer-lightning-cli.sh invoice "$amount" "$description"
    else
        print_error "Lightning CLI script not found"
        return 1
    fi
}

# Function to pay a Lightning invoice
pay_lightning_invoice() {
    local invoice=$1
    
    print_status "Paying Lightning invoice..."
    
    if [ -f "BTCPayServer.Tests/docker-customer-lightning-cli.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-customer-lightning-cli.sh
        BTCPayServer.Tests/docker-customer-lightning-cli.sh pay "$invoice"
    else
        print_error "Lightning CLI script not found"
        return 1
    fi
}

# Function to setup Lightning channels
setup_lightning_channels() {
    print_status "Setting up Lightning channels..."
    
    if [ -f "BTCPayServer.Tests/docker-lightning-channel-setup.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-lightning-channel-setup.sh
        BTCPayServer.Tests/docker-lightning-channel-setup.sh
        print_success "Lightning channels setup complete"
    else
        print_error "Lightning channel setup script not found"
        return 1
    fi
}

# Function to show Lightning node info
show_lightning_info() {
    print_status "Lightning node information:"
    
    if [ -f "BTCPayServer.Tests/docker-customer-lightning-cli.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-customer-lightning-cli.sh
        echo "Customer Lightning Node:"
        BTCPayServer.Tests/docker-customer-lightning-cli.sh getinfo
        echo ""
    fi
    
    if [ -f "BTCPayServer.Tests/docker-merchant-lightning-cli.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-merchant-lightning-cli.sh
        echo "Merchant Lightning Node:"
        BTCPayServer.Tests/docker-merchant-lightning-cli.sh getinfo
    fi
}

# Function to show Bitcoin node info
show_bitcoin_info() {
    print_status "Bitcoin node information:"
    
    if [ -f "BTCPayServer.Tests/docker-bitcoin-cli.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-bitcoin-cli.sh
        echo "Block count:"
        BTCPayServer.Tests/docker-bitcoin-cli.sh getblockcount
        echo ""
        echo "Balance:"
        BTCPayServer.Tests/docker-bitcoin-cli.sh getbalance
        echo ""
        echo "Network info:"
        BTCPayServer.Tests/docker-bitcoin-cli.sh getnetworkinfo
    fi
}

# Function to test complete payment flow
test_payment_flow() {
    print_status "Testing complete payment flow..."
    
    # Generate some blocks first
    generate_blocks 10
    
    # Get a new address
    print_status "Getting new Bitcoin address..."
    if [ -f "BTCPayServer.Tests/docker-bitcoin-cli.sh" ]; then
        chmod +x BTCPayServer.Tests/docker-bitcoin-cli.sh
        address=$(BTCPayServer.Tests/docker-bitcoin-cli.sh getnewaddress)
        print_success "New address: $address"
        
        # Send some Bitcoin to the address
        send_bitcoin "$address" 0.01
        
        # Generate blocks to confirm the transaction
        generate_blocks 6
        
        print_success "Payment flow test completed!"
    else
        print_error "Bitcoin CLI script not found"
        return 1
    fi
}

# Main menu
show_menu() {
    echo ""
    echo "🔧 BTCPay Server Payment Testing Tools"
    echo "======================================"
    echo "1. Generate Bitcoin blocks"
    echo "2. Send Bitcoin to address"
    echo "3. Get Bitcoin balance"
    echo "4. Setup Lightning channels"
    echo "5. Create Lightning invoice"
    echo "6. Pay Lightning invoice"
    echo "7. Show Lightning node info"
    echo "8. Show Bitcoin node info"
    echo "9. Test complete payment flow"
    echo "0. Exit"
    echo ""
    read -p "Choose an option (0-9): " choice
    
    case $choice in
        1)
            read -p "Number of blocks to generate (default 1): " blocks
            generate_blocks "${blocks:-1}"
            ;;
        2)
            read -p "Enter Bitcoin address: " address
            read -p "Enter amount in BTC (default 0.001): " amount
            send_bitcoin "$address" "${amount:-0.001}"
            ;;
        3)
            get_balance
            ;;
        4)
            setup_lightning_channels
            ;;
        5)
            read -p "Enter amount in satoshis (default 1000): " amount
            read -p "Enter description (default 'Test invoice'): " description
            create_lightning_invoice "${amount:-1000}" "${description:-Test invoice}"
            ;;
        6)
            read -p "Enter Lightning invoice: " invoice
            pay_lightning_invoice "$invoice"
            ;;
        7)
            show_lightning_info
            ;;
        8)
            show_bitcoin_info
            ;;
        9)
            test_payment_flow
            ;;
        0)
            print_success "Goodbye!"
            exit 0
            ;;
        *)
            print_error "Invalid option. Please try again."
            ;;
    esac
    
    # Show menu again
    show_menu
}

# Check if we're in the right directory
if [ ! -d "BTCPayServer.Tests" ]; then
    print_error "Please run this script from the BTCPay Server root directory"
    exit 1
fi

# Check if services are running
if ! nc -z localhost 43782 2>/dev/null; then
    print_error "Bitcoin node is not running. Please run ./test-environment-setup.sh first"
    exit 1
fi

print_success "Payment testing tools ready!"
show_menu