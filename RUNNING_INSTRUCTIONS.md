# 🚀 v2rayN Iranian Sanctions Bypass - Complete Setup Guide

## 🎯 What You Got

You now have a complete v2rayN setup with Iranian sanctions bypass features that include:

### ✅ **Automatic Repository Redirection**
- **Zero Configuration**: Your Android Studio will automatically use Iranian mirrors
- **Transparent DNS**: Repository domains resolve to Iranian mirror IPs
- **Smart Fallback**: Falls back to international repositories if needed

### ✅ **Iranian DNS Servers**
All 12 Iranian DNS servers pre-configured:
- Shecan (178.22.122.100, 185.51.200.2)
- Radar (10.202.10.10, 10.202.10.11)
- Shelter (94.103.125.157, 94.103.125.158)
- Electro (78.157.42.100, 78.157.42.101)
- And more...

### ✅ **Repository Mappings**
- `maven.google.com` → `maven.myket.ir`
- `gradle.org` → `en-mirror.ir`
- `repo.maven.apache.org` → `maven.myket.ir`
- All major repositories automatically redirected

## 🔧 How To Build and Run

### Option 1: Use the Provided Build Scripts (Recommended)

#### Prerequisites
1. **Install .NET 8.0 SDK** from https://dotnet.microsoft.com/download
2. **Windows 10/11** with administrator privileges

#### Build Steps
```cmd
# Method 1: Use Batch Script (Simplest)
build-windows.bat

# Method 2: Use PowerShell Script
# Note: Run PowerShell as Administrator first
powershell -ExecutionPolicy Bypass -File "build-windows-simple.ps1"
```

### Option 2: Manual Build (If Scripts Don't Work)

```cmd
# 1. Install .NET 8.0 SDK first from https://dotnet.microsoft.com/download

# 2. Navigate to the project directory
cd v2rayN

# 3. Restore packages
dotnet restore v2rayN.sln

# 4. Build v2rayN
dotnet publish v2rayN/v2rayN.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o ../Release/windows-64

# 5. Build AmazTool
dotnet publish AmazTool/AmazTool.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o ../Release/windows-64
```

### Option 3: Self-Contained Build (No .NET Required on Target Machine)

```cmd
# Build with all dependencies included
dotnet publish v2rayN/v2rayN.csproj -c Release -r win-x64 --self-contained=true -p:EnableWindowsTargeting=true -o Release/windows-64
```

## 🎯 Running v2rayN

### After Successful Build

1. **Navigate to the output directory**:
   ```
   cd Release\windows-64
   ```

2. **Run the launcher**:
   ```
   Start v2rayN.bat
   ```

3. **Or run directly**:
   ```
   v2rayN.exe
   ```

## ⚙️ Configuration (Important!)

### For Iranian Sanctions Bypass

1. **Launch v2rayN** using the launcher script

2. **Apply Iran Preset**:
   - Menu → Regional Presets → Iran
   - This automatically configures Iranian DNS servers

3. **Load Configuration Files**:
   - Go to **Settings → Routing Settings**
   - Load: `config/custom_routing_transparent_mirrors`

4. **Load DNS Configuration**:
   - Go to **Settings → DNS Settings**
   - Load: `config/dns_transparent_mirrors_v2ray`

5. **Access Advanced Settings** (Optional):
   - Menu → Settings → Sanctions Bypass Settings
   - Monitor DNS switching and test connections

### For Android Development

**Option A: Automatic (Recommended)**
- Just build and run - repositories are automatically redirected!

**Option B: Manual Configuration**
1. Run: `config/setup_android_development.bat`
2. Follow the setup instructions

## 🌐 How It Works

### **Transparent Redirection**
```
Your Android Studio → v2rayN → Iranian Mirror → Fast Download
```

1. **DNS Level**: `maven.google.com` resolves to Iranian mirror IP
2. **Automatic Routing**: Traffic goes through Iranian mirror servers
3. **Zero Configuration**: Your IDE and build tools work normally

### **Smart Fallback System**
- **Primary**: Iranian mirrors (fastest for Iranian users)
- **Secondary**: Chinese mirrors (reliable international)
- **Fallback**: Original repositories (always available)

## 🔍 Troubleshooting

### Build Issues

**Problem**: `.NET SDK not found`
```
Solution: Download and install .NET 8.0 SDK from:
https://dotnet.microsoft.com/download/dotnet/8.0
```

**Problem**: `Build failed`
```
Solution:
1. Make sure you're in the correct directory
2. Check if all project files exist
3. Try: dotnet restore v2rayN/v2rayN.sln
```

### Runtime Issues

**Problem**: `403 Forbidden` errors still occur
```
Solution:
1. Make sure Iran preset is applied
2. Check if transparent routing is loaded
3. Try: Menu → Settings → Sanctions Bypass Settings → Check Now
```

**Problem**: Slow downloads
```
Solution:
1. Verify Iranian DNS servers are working
2. Check your VPN/proxy connection
3. Try different DNS servers in the settings
```

**Problem**: Cannot connect to Iranian mirrors
```
Solution:
1. Check your internet connection
2. Try using a different VPN/proxy
3. Test mirror connectivity in Sanctions Bypass Settings
```

## 📁 File Structure

```
v2rayN/
├── v2rayN/                    # Main WPF application
├── ServiceLib/                # Core services
├── AmazTool/                  # Additional tools
├── build-windows.bat          # Simple build script
├── build-windows-simple.ps1   # PowerShell build script
├── BUILD_README.md           # This documentation
├── ServiceLib/Sample/         # Configuration files
│   ├── custom_routing_transparent_mirrors
│   ├── dns_transparent_mirrors_v2ray
│   ├── setup_android_development.bat
│   └── README_ANDROID_IRAN_SANCTIONS.md
└── Release/windows-64/        # Build output (after build)
    ├── v2rayN.exe
    ├── AmazTool.exe
    ├── Start v2rayN.bat
    └── config/
```

## 🎉 Expected Results

### ✅ **After Setup**
- Android Studio builds work without 403 errors
- Gradle dependencies download from Iranian mirrors
- No manual configuration needed for new projects
- Automatic DNS switching for sanctions bypass

### ✅ **Performance Benefits**
- **Faster Downloads**: Iranian servers provide local speed
- **Reliable Access**: Multiple mirror options
- **Automatic Optimization**: Smart DNS switching
- **Zero Maintenance**: Self-healing configuration

## 🚀 Advanced Usage

### Build for ARM64 (Windows on ARM)
```cmd
dotnet publish v2rayN/v2rayN.csproj -c Release -r win-arm64 --self-contained=false -p:EnableWindowsTargeting=true -o Release/windows-arm64
```

### Continuous Integration
```yaml
# For GitHub Actions
- name: Build v2rayN
  run: |
    cd v2rayN
    dotnet publish ./v2rayN/v2rayN.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o ../Release/windows-64
```

### Custom Configuration
- Edit `ServiceLib/Sample/custom_routing_transparent_mirrors` for custom routing rules
- Modify `ServiceLib/Sample/dns_transparent_mirrors_v2ray` for custom DNS settings
- Add new mirror mappings in `TransparentMirrorService.cs`

## 📞 Support

### Documentation
- `BUILD_README.md` - Build instructions
- `ServiceLib/Sample/README_ANDROID_IRAN_SANCTIONS.md` - Usage guide
- `Release/windows-64/README.txt` - Build-specific information

### Configuration Files Location
All configuration files are available in `ServiceLib/Sample/` and copied to `Release/windows-64/config/`

### Community
- Report issues in the project repository
- Check for updates regularly
- Join Iranian developer communities for support

---

**🎊 Enjoy seamless Android development with Iranian sanctions bypass!**

*Built with ❤️ for Iranian developers facing connectivity challenges*

