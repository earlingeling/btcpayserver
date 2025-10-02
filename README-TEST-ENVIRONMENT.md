# BTCPay Server Test Environment

This document describes the complete test environment setup for BTCPay Server development with hot reload capabilities.

## 🚀 Quick Start

### 1. Initial Setup
```bash
# Make scripts executable
chmod +x *.sh

# Start the test environment
./test-environment-setup.sh
```

### 2. Start Development with Hot Reload
```bash
# Start BTCPay Server with hot reload
./start-dev-with-hot-reload.sh
```

### 3. Access the Application
- **BTCPay Server**: http://localhost:14142
- **NBXplorer**: http://localhost:32838
- **Bitcoin RPC**: localhost:43782
- **PostgreSQL**: localhost:39372

## 🔧 Hot Reload Features

BTCPay Server supports hot reload for development:

### Automatic Reload Triggers
- **C# Code Changes**: Any `.cs` file changes automatically restart the server
- **Razor Views**: View changes are hot-reloaded without restart
- **Configuration**: `appsettings.json` changes trigger restart
- **Static Files**: CSS/JS changes are served immediately

### Development Profile
The `Bitcoin` launch profile is configured for development with:
- `ASPNETCORE_ENVIRONMENT=Development`
- Hot reload enabled via `dotnet watch`
- Debug logging enabled
- Cheat mode enabled for testing

## 🧪 Testing Tools

### Payment Flow Testing
```bash
# Interactive testing menu
./test-payment-flows.sh
```

### Manual Bitcoin Operations
```bash
# Generate blocks
./BTCPayServer.Tests/docker-bitcoin-generate.sh 10

# Send Bitcoin
./BTCPayServer.Tests/docker-bitcoin-cli.sh sendtoaddress "address" 0.001

# Check balance
./BTCPayServer.Tests/docker-bitcoin-cli.sh getbalance
```

### Lightning Network Testing
```bash
# Setup channels
./BTCPayServer.Tests/docker-lightning-channel-setup.sh

# Create invoice
./BTCPayServer.Tests/docker-customer-lightning-cli.sh invoice 1000 "Test invoice"

# Pay invoice
./BTCPayServer.Tests/docker-customer-lightning-cli.sh pay "lnbcrt..."
```

## 🐳 Docker Services

The test environment includes:

| Service | Port | Description |
|---------|------|-------------|
| bitcoind | 43782 (RPC), 39388 (P2P) | Bitcoin regtest node |
| nbxplorer | 32838 | Bitcoin blockchain indexer |
| postgres | 39372 | PostgreSQL database |
| merchant_lnd | 35531 (REST), 30894 (P2P) | Lightning Network Daemon (merchant) |
| customer_lnd | 35532 (REST), 30895 (P2P) | Lightning Network Daemon (customer) |
| merchant_lightningd | 30993 (API), 30893 (P2P) | Core Lightning (merchant) |
| customer_lightningd | 30992 (API), 30892 (P2P) | Core Lightning (customer) |
| tor | 9050 (SOCKS), 9051 (Control) | Tor proxy |
| sshd | 21622 | SSH server for testing |

## 🔄 Development Workflow

### 1. Start Environment
```bash
./test-environment-setup.sh
```

### 2. Start Development Server
```bash
./start-dev-with-hot-reload.sh
```

### 3. Make Changes
- Edit any `.cs` file → Server automatically restarts
- Edit Razor views → Hot reloaded without restart
- Edit static files → Served immediately

### 4. Test Changes
- Use the web interface at http://localhost:14142
- Use testing tools: `./test-payment-flows.sh`
- Check logs in terminal

## 🛠️ Troubleshooting

### Services Not Starting
```bash
# Check Docker status
docker ps

# View logs
cd BTCPayServer.Tests
docker-compose logs -f

# Reset environment
docker-compose down --volumes
docker-compose up -d
```

### Hot Reload Not Working
```bash
# Ensure you're using the correct profile
dotnet watch run --launch-profile Bitcoin

# Check if files are being watched
dotnet watch --list
```

### Lightning Issues
```bash
# Reset Lightning channels
./BTCPayServer.Tests/docker-lightning-channel-teardown.sh
./BTCPayServer.Tests/docker-lightning-channel-setup.sh
```

### Database Issues
```bash
# Reset database
cd BTCPayServer.Tests
docker-compose down --volumes
docker-compose up -d postgres
```

## 📝 Development Tips

### 1. Use Cheat Mode
The test environment has `BTCPAY_CHEATMODE=true` enabled, which provides:
- Admin registration without email verification
- Bypass certain security checks
- Faster development cycles

### 2. Debug Logging
Enable verbose logging with `BTCPAY_VERBOSE=true` (already enabled in dev profile).

### 3. Lightning Testing
- Use Polar (https://lightningpolar.com/) for visual Lightning Network testing
- Channels are automatically funded in regtest mode
- Fast block generation for quick confirmations

### 4. Bitcoin Testing
- Regtest mode allows instant block generation
- No real Bitcoin required
- All transactions are free

## 🔐 Security Notes

⚠️ **This is a TEST environment only!**
- Uses regtest Bitcoin (fake Bitcoin)
- Cheat mode enabled
- No real funds at risk
- Not suitable for production

## 📚 Additional Resources

- [BTCPay Server Documentation](https://docs.btcpayserver.org/)
- [Local Development Guide](https://docs.btcpayserver.org/Development/LocalDevelopment/)
- [API Documentation](https://docs.btcpayserver.org/API/Greenfield/v1/)
- [Lightning Network Guide](https://docs.btcpayserver.org/LightningNetwork/)

## 🆘 Getting Help

If you encounter issues:
1. Check the troubleshooting section above
2. View Docker logs: `docker-compose logs -f`
3. Check BTCPay Server logs in the terminal
4. Refer to the official documentation
5. Join the community chat: https://chat.btcpayserver.org/