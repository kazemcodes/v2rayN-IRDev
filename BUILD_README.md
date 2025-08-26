# v2rayN Windows Build Guide

This guide explains how to build and run v2rayN with Iranian sanctions bypass features on Windows.

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK (download from [microsoft.com](https://dotnet.microsoft.com/download))

### Option 1: Use PowerShell Script (Recommended)
```powershell
# Run the build script
.\build-windows.ps1

# Or with custom options
.\build-windows.ps1 -Configuration Release -SelfContained $true -IncludeArm64 $true
```

### Option 2: Use Batch Script
```cmd
# Run the batch build script
build-windows.bat
```

### Option 3: Manual Build
```cmd
# Restore packages
dotnet restore v2rayN/v2rayN.sln

# Build v2rayN
dotnet publish v2rayN/v2rayN/v2rayN.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o Release/windows-64

# Build AmazTool
dotnet publish v2rayN/AmazTool/AmazTool.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o Release/windows-64
```

## 📁 Build Output

After successful build, you'll find:
```
Release/
└── windows-64/
    ├── v2rayN.exe                    # Main application
    ├── AmazTool.exe                  # Additional tool
    ├── Start v2rayN.bat             # Launcher script
    ├── README.txt                   # Build information
    └── config/                      # Configuration files
        ├── custom_routing_transparent_mirrors
        ├── dns_transparent_mirrors_v2ray
        ├── dns_transparent_mirrors_singbox
        ├── setup_android_development.bat
        └── README_ANDROID_IRAN_SANCTIONS.md
```

## 🎯 Running v2rayN

### Method 1: Use Launcher Script
1. Navigate to `Release/windows-64/`
2. Double-click `Start v2rayN.bat`

### Method 2: Run Directly
1. Open Command Prompt in `Release/windows-64/`
2. Run: `v2rayN.exe`

### Method 3: Run from Explorer
1. Navigate to `Release/windows-64/`
2. Double-click `v2rayN.exe`

## 🔧 Configuration

### For Iranian Sanctions Bypass
1. **Launch v2rayN** using the launcher script
2. **Apply Iran Preset**: Menu → Regional Presets → Iran
3. **Load Configuration**:
   - Go to Settings → Routing Settings
   - Load `config/custom_routing_transparent_mirrors`
4. **Load DNS Configuration**:
   - Go to Settings → DNS Settings
   - Load `config/dns_transparent_mirrors_v2ray`
5. **Access Advanced Settings**:
   - Menu → Settings → Sanctions Bypass Settings
   - Monitor DNS switching and test connections

### For Android Development
1. **Run Setup Script**: `config/setup_android_development.bat`
2. **Follow Instructions** in the setup script
3. **Or Manually Configure** your Android Studio/Gradle projects

## 🛠️ Build Options

### PowerShell Script Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Configuration` | `Release` | Build configuration (Release/Debug) |
| `Runtime` | `win-x64` | Target runtime (win-x64/win-arm64) |
| `OutputPath` | `.\Release\windows-64` | Output directory |
| `SelfContained` | `false` | Include .NET runtime (true/false) |
| `IncludeArm64` | `false` | Also build for ARM64 (true/false) |

### Examples

```powershell
# Self-contained build
.\build-windows.ps1 -SelfContained $true

# ARM64 build
.\build-windows.ps1 -Runtime win-arm64

# Debug build
.\build-windows.ps1 -Configuration Debug

# Complete build with all options
.\build-windows.ps1 -Configuration Release -SelfContained $true -IncludeArm64 $true
```

## 🔍 Troubleshooting

### Build Issues

**Error: .NET SDK not found**
```
Solution: Install .NET 8.0 SDK from https://dotnet.microsoft.com/download
```

**Error: Failed to restore packages**
```
Solution: Check your internet connection and try again
```

**Error: Failed to build application**
```
Solution: Make sure all project dependencies are available
```

### Runtime Issues

**Error: Missing dependencies**
```
Solution: Use -SelfContained $true to include .NET runtime
```

**Error: Configuration files not found**
```
Solution: Make sure you copied all files from config/ directory
```

**Error: Cannot connect to Iranian mirrors**
```
Solution: Check your VPN/proxy connection and try again
```

## 📋 Features Included

### ✅ Iranian Sanctions Bypass
- **Automatic DNS Switching**: 12 Iranian DNS servers for fallback
- **403 Error Detection**: Automatic detection and resolution
- **Transparent Mirroring**: Zero-configuration repository redirection
- **GUI Settings**: Easy configuration and monitoring

### ✅ Repository Mappings
- `maven.google.com` → `maven.myket.ir`
- `gradle.org` → `en-mirror.ir`
- `repo.maven.apache.org` → `maven.myket.ir`
- `central.sonatype.org` → `maven.myket.ir`
- `plugins.gradle.org` → `en-mirror.ir`
- `jcenter.bintray.com` → `maven.myket.ir`

### ✅ Configuration Files
- `custom_routing_transparent_mirrors` - Transparent routing rules
- `dns_transparent_mirrors_v2ray` - DNS with Iranian mirrors
- `dns_transparent_mirrors_singbox` - Singbox DNS configuration
- `setup_android_development.bat` - Android developer setup
- `README_ANDROID_IRAN_SANCTIONS.md` - Complete documentation

## 🎉 What's New in This Build

### ✨ Iranian Sanctions Bypass
- **Automatic Detection**: Monitors for 403 errors and sanctions
- **DNS Switching**: Automatically switches to Iranian DNS servers
- **Transparent Mirroring**: No configuration needed for apps
- **12 Iranian DNS Servers**: Multiple fallback options

### ✨ Enhanced User Experience
- **One-Click Launcher**: `Start v2rayN.bat` for easy startup
- **Built-in Configuration**: All config files included
- **Comprehensive Documentation**: Step-by-step guides
- **Error Handling**: Better error messages and recovery

### ✨ Android Development Optimized
- **Iranian Mirrors**: Myket and EN Mirror for fast downloads
- **Maven & Gradle**: Optimized for both build systems
- **IDE Integration**: Works with Android Studio & IntelliJ
- **Team Ready**: No individual configuration needed

## 🚀 Advanced Usage

### Build for ARM64 (Windows on ARM)
```powershell
.\build-windows.ps1 -Runtime win-arm64
```

### Self-Contained Build (No .NET Required)
```powershell
.\build-windows.ps1 -SelfContained $true
```

### Custom Output Directory
```powershell
.\build-windows.ps1 -OutputPath ".\MyCustomBuild"
```

### Continuous Integration
```yaml
# For GitHub Actions or CI/CD
- name: Build v2rayN
  run: |
    cd v2rayN
    dotnet publish ./v2rayN/v2rayN.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o ../Release/windows-64
```

## 📞 Support

### Documentation
- `config/README_ANDROID_IRAN_SANCTIONS.md` - Complete usage guide
- `Release/windows-64/README.txt` - Build-specific information

### Configuration Files Location
All configuration files are copied to `Release/windows-64/config/` for easy access.

### Troubleshooting
1. Check the build log for error messages
2. Verify .NET SDK installation and version
3. Ensure internet connection for package restore
4. Test the built application in `Release/windows-64/`

---

**Happy building!** 🎉

*Built with ❤️ for Iranian developers facing sanctions*

