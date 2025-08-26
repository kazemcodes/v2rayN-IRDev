# v2rayN Android Development Iran Sanctions Bypass

This guide provides comprehensive instructions for optimizing v2rayN to bypass Iran sanctions specifically for Android development and Gradle builds.

## 🚨 New Features: Automatic Sanctions Detection & DNS Switching

The latest version includes **automatic sanctions detection** and **intelligent DNS switching** to handle 403 errors and corrupted Gradle cache downloads.

### ✨ Key Improvements
- **Automatic 403 Detection**: Monitors Google domains for sanctions-related 403 responses
- **Intelligent DNS Switching**: Automatically switches to Iranian DNS servers when needed
- **Sanctions Bypass Verification**: Tests proxy configuration before allowing connections
- **Enhanced DNS Configurations**: Pre-configured Iranian DNS servers as fallbacks
- **Real-time Monitoring**: Continuous health checking of DNS servers
- **User-friendly Interface**: GUI settings window for easy configuration

## 🚀 Quick Setup

### Option 1: Automatic Transparent Mirroring (RECOMMENDED)
1. **Apply Iran Preset**: Menu → Regional Presets → Iran
2. **Load Transparent Routing**: Load `custom_routing_transparent_mirrors`
3. **Load Transparent DNS**: Load `dns_transparent_mirrors_v2ray` or `dns_transparent_mirrors_singbox`
4. **Done!** Your apps will automatically use Iranian mirrors without any configuration changes

### Option 2: Manual Configuration
1. **Apply Iran Preset**: Menu → Regional Presets → Iran
2. **Load Android Development Routing**: Load `custom_routing_android_development`
3. **Apply DNS Configuration**: Load `dns_android_development_v2ray` or `dns_android_development_singbox`
4. **Configure Your Projects**: Update `settings.gradle` and `build.gradle` files

### Step 3: Access Advanced Settings (Optional)
1. Go to **Menu → Settings → Sanctions Bypass Settings**
2. **Check Current Status**: Click "Check Now" to see if sanctions are active
3. **Test DNS Servers**: Use "Test All" to verify Iranian DNS server connectivity
4. **Configure Automatic Switching**: Enable auto-switching for seamless operation
5. **Monitor Activity**: Check the log for real-time DNS switching activity

### Step 4: Configure Development Environment

#### For Android Studio/Gradle Projects:
1. Copy `gradle-wrapper-iran-mirrors` to your project's `gradle/wrapper/gradle-wrapper.properties`
2. Copy `maven-settings-iran-mirrors` to `~/.m2/settings.xml`

#### For Gradle Projects (Kotlin DSL):
1. Copy `settings.gradle.kts.iran-mirrors` to your project's `settings.gradle.kts`
2. Or manually add Iranian mirrors to your existing `settings.gradle.kts`:

```kotlin
dependencyResolutionManagement {
    repositories {
        // Iranian Mirrors (Primary)
        maven("https://maven.myket.ir")
        maven("https://en-mirror.ir")

        // Chinese Mirrors (Secondary)
        maven("https://maven.aliyun.com/repository/central")

        // Original repositories (Fallback)
        google()
        mavenCentral()
    }
}
```

#### For Gradle Projects (Groovy DSL):
1. Copy `settings.gradle.iran-mirrors` to your project's `settings.gradle`
2. Or manually add Iranian mirrors to your existing `settings.gradle`:

```groovy
dependencyResolutionManagement {
    repositories {
        // Iranian Mirrors (Primary)
        maven {
            url 'https://maven.myket.ir'
        }
        maven {
            url 'https://en-mirror.ir'
        }

        // Chinese Mirrors (Secondary)
        maven {
            url 'https://maven.aliyun.com/repository/central'
        }

        // Original repositories (Fallback)
        google()
        mavenCentral()
    }
}
```

#### For IDE Proxy Settings:
1. Configure your IDE to use v2rayN proxy:
   - HTTP Proxy: `127.0.0.1:10809`
   - HTTPS Proxy: `127.0.0.1:10809`

## 🔧 How Transparent Mirroring Works

### What is Transparent Mirroring?
Transparent mirroring automatically redirects your repository requests to Iranian mirrors **without any changes to your project configuration**. When your Android Studio or Gradle requests dependencies from:

- `maven.google.com` → Automatically redirected to `maven.myket.ir`
- `gradle.org` → Automatically redirected to `en-mirror.ir`
- `repo.maven.apache.org` → Automatically redirected to `maven.myket.ir`

### DNS-Level Redirection
The system works by:
1. **DNS Resolution**: When your app requests `maven.google.com`, the DNS resolves it to the Iranian mirror's IP
2. **Automatic Routing**: Traffic is routed through v2rayN's proxy to the Iranian mirror
3. **Seamless Experience**: Your IDE and build tools work normally, but use faster Iranian servers

### Benefits of Transparent Mirroring
- **Zero Configuration**: No changes needed to your `build.gradle` or `settings.gradle` files
- **Faster Downloads**: Iranian servers provide faster speeds for Iranian developers
- **Automatic Fallback**: If Iranian mirrors fail, the system falls back to international repositories
- **Sanctions Bypass**: Automatic handling of 403 errors and DNS blocking

## 🔧 Sanctions Bypass Settings GUI

### Current Status Panel
- **Sanctions Status**: Shows if sanctions are currently active (Red = Active, Green = Inactive)
- **Current DNS**: Displays the currently active DNS server
- **Check Now Button**: Manually test sanctions status

### Iranian DNS Servers Panel
- **Server List**: All 12 Iranian DNS servers with status indicators
- **Test Button**: Test individual DNS server connectivity
- **Test All Button**: Test all DNS servers at once

### Transparent Mirroring Panel (NEW!)
- **Enable Transparent Mirroring**: Toggle automatic repository redirection
- **Test Mirrors Button**: Test all mirror server connectivity
- **Refresh Mappings Button**: Update mirror URL mappings
- **Mirror Mappings Grid**: Shows all domain → mirror URL mappings
- **Add Custom Mapping**: Add your own mirror mappings

### Protected Domains
- **Google Domains List**: All domains that require sanctions bypass
- **Iranian Mirrors**: Myket and EN Mirror domains are automatically protected
- **Add Domain**: Manually add additional domains for protection

### Advanced Settings
- **Check Interval**: How often to monitor sanctions status (default: 5 minutes)
- **Timeout**: Connection timeout for DNS tests (default: 10 seconds)
- **Auto Start**: Enable monitoring on application startup
- **Enable Transparent Mirroring**: Enable automatic repository redirection on startup

## 📋 What's Included

### Routing Rules (`custom_routing_android_development`)
- **Gradle Distribution**: Direct access to Gradle downloads
- **Google Maven Repository**: Essential for Android SDK
- **Maven Central & Apache**: Core Java libraries
- **JCenter/Bintray**: Legacy Android libraries
- **JetBrains/IntelliJ**: IDE updates and plugins
- **GitHub/GitLab/Bitbucket**: Version control and dependencies
- **Stack Overflow**: Documentation and community support
- **DNS Services**: Reliable DNS resolution

### DNS Configuration
- **Google DNS**: Primary DNS for development domains
- **Cloudflare DNS**: Backup for Google services
- **AliDNS**: Local DNS for Iran and private networks
- **Smart Routing**: Automatic DNS server selection based on domain

### Iranian DNS Servers (NEW!)
All 12 Iranian DNS servers are pre-configured for automatic fallback:

#### Primary Servers
- **Shecan**: 178.22.122.100, 185.51.200.2 (Most popular)
- **Radar**: 10.202.10.10, 10.202.10.11 (Gaming optimized)
- **Electro**: 78.157.42.100, 78.157.42.101 (Filtering)

#### Secondary Servers
- **Shelter**: 94.103.125.157, 94.103.125.158 (Gaming)
- **403**: 10.202.10.202, 10.202.10.102 (Filtering)
- **Begzar**: 185.55.226.26, 185.55.225.25 (Filtering)

### Iranian Mirrors (NEW!)
- **Myket Maven** (`https://maven.myket.ir`): Primary Iranian mirror for Android libraries
- **EN Mirror** (`https://en-mirror.ir`): Iranian Gradle dependency mirror

### Chinese Mirrors (Secondary)
- **Aliyun**: Fast Chinese mirror for Maven Central
- **Huawei Cloud**: Alternative Chinese mirror
- **Tencent Cloud**: Additional backup mirror

### Original Repositories (Fallback)
- **Google Maven**: Direct access to Android libraries
- **Gradle Plugin Portal**: Official Gradle plugins
- **JCenter**: Legacy Android libraries

## 🔧 Manual Configuration

### Gradle Wrapper Properties
```properties
# Add to gradle/wrapper/gradle-wrapper.properties
systemProp.http.proxyHost=127.0.0.1
systemProp.http.proxyPort=10809
systemProp.https.proxyHost=127.0.0.1
systemProp.https.proxyPort=10809

# Use Chinese mirror for faster downloads
distributionUrl=https\://mirrors.cloud.tencent.com/gradle/gradle-8.0.2-bin.zip
```

### Maven Settings
```xml
<!-- Add to ~/.m2/settings.xml -->
<settings>
  <proxies>
    <proxy>
      <id>v2ray-proxy</id>
      <active>true</active>
      <protocol>https</protocol>
      <host>127.0.0.1</host>
      <port>10809</port>
    </proxy>
  </proxies>
  <mirrors>
    <mirror>
      <id>aliyun-central</id>
      <mirrorOf>central</mirrorOf>
      <name>Aliyun Central</name>
      <url>https://maven.aliyun.com/repository/central</url>
    </mirror>
  </mirrors>
</settings>
```

### IDE Configuration
1. **Android Studio**:
   - File → Settings → Appearance & Behavior → System Settings → HTTP Proxy
   - Set Manual proxy configuration
   - Host: `127.0.0.1`, Port: `10809`

2. **IntelliJ IDEA**:
   - File → Settings → System Settings → HTTP Proxy
   - Same configuration as Android Studio

## 🌐 Domains Covered

### Essential Android Development
- `gradle.org` - Gradle distribution
- `maven.google.com` - Google Maven repository
- `dl.google.com` - Google downloads
- `developer.android.com` - Android documentation
- `repo.maven.apache.org` - Maven Central
- `central.sonatype.org` - Sonatype repository

### Iranian Mirrors (NEW!)
- `maven.myket.ir` - Myket Iranian Maven repository
- `en-mirror.ir` - EN Mirror for Gradle dependencies
- `maven.aliyun.com` - Aliyun Chinese mirror
- `mirrors.huaweicloud.com` - Huawei Cloud mirror

### Development Tools
- `github.com` - GitHub repositories
- `plugins.jetbrains.com` - IDE plugins
- `stackoverflow.com` - Developer community
- `gitlab.com` - GitLab repositories
- `bitbucket.org` - Bitbucket repositories

### CDN and Services
- `cdnjs.cloudflare.com` - CDN for libraries
- `fonts.googleapis.com` - Google Fonts
- `firebase.google.com` - Firebase services

## ⚡ Performance Optimization

### DNS Settings
- Primary DNS: Google DNS (fast for global domains)
- Backup DNS: Cloudflare (reliable for Google services)
- Local DNS: AliDNS (fast for Iran traffic)

### Routing Strategy
- Direct routing for Iran domains
- Proxy routing for international domains
- Block unwanted protocols (BitTorrent)

### Connection Settings
- Increased timeouts for large downloads
- Optimized for HTTPS connections
- Support for parallel downloads

## 🔍 Troubleshooting

### Common Issues

1. **Gradle Sync Fails**:
   - Check proxy settings in `gradle.properties`
   - Verify v2rayN is running and connected
   - Try different mirror URLs

2. **Slow Downloads**:
   - Switch to Aliyun mirror in Maven settings
   - Check DNS resolution in v2rayN logs
   - Verify proxy port configuration

3. **Connection Timeouts**:
   - Increase timeout values in `gradle.properties`
   - Check network connectivity
   - Try restarting v2rayN

4. **Certificate Issues**:
   - Ensure system certificates are up to date
   - Check proxy bypass settings for local domains

### Debug Steps
1. Check v2rayN logs for connection errors
2. Test proxy connection: `curl -x http://127.0.0.1:10809 https://google.com`
3. Verify DNS: `nslookup gradle.org`
4. Check firewall settings

## 🔍 Sanctions Bypass Troubleshooting

### DNS Switching Issues
1. **DNS Not Switching**: Check if sanctions are detected in the GUI
2. **403 Errors Persist**: Verify Iranian DNS servers are accessible
3. **Slow Switching**: Increase check interval or test DNS servers manually

### Automatic Detection Problems
1. **False Positives**: Temporarily disable auto-switching and use manual mode
2. **False Negatives**: Manually check status and force DNS switch if needed
3. **Network Errors**: Check internet connectivity and proxy settings

### DNS Server Issues
1. **Server Not Responding**: Use "Test All" to find working servers
2. **All Servers Down**: Check your internet connection and Iranian network access
3. **Inconsistent Results**: Try different DNS server combinations

### Configuration Problems
1. **Settings Not Saved**: Ensure you click "Save Settings" in the GUI
2. **Auto-Start Not Working**: Check if the application has proper permissions
3. **Log Not Updating**: Restart the application to refresh logging

## 🔄 Automatic Features

### Background Monitoring
- **Sanctions Detection**: Runs every 5 minutes by default
- **DNS Health Check**: Tests current DNS server health
- **Automatic Switching**: Switches to working Iranian DNS when needed
- **Logging**: All actions logged to the GUI activity log

### Smart Fallback System
- **Primary → Secondary**: Falls back to different Iranian DNS servers
- **Iranian → International**: Falls back to Google/Cloudflare if all Iranian DNS fail
- **Connection Recovery**: Attempts to restore optimal DNS configuration
- **User Notification**: Logs all switching activities for transparency

## 📱 Mobile Development Support

### React Native
- NPM registry access
- Metro bundler proxy configuration
- iOS/Android SDK downloads

### Flutter
- Pub.dev access
- Dart SDK downloads
- Google Fonts integration

### Ionic/Cordova
- NPM and Bower repositories
- Platform SDK downloads
- Cordova plugin repositories

## 🔄 Updates and Maintenance

### Keeping Configurations Current
1. Regularly update geo files through v2rayN
2. Check for new mirror URLs
3. Update Gradle wrapper versions
4. Monitor DNS server reliability

### Backup Configurations
- Export v2rayN settings regularly
- Keep copies of custom routing rules
- Backup Maven and Gradle configurations

## 🤝 Contributing

To improve this configuration:
1. Test with different Android projects
2. Report missing domains or slow connections
3. Suggest additional mirrors or optimizations
4. Share working configurations for other regions

## 📄 License

This configuration is provided as-is for educational and development purposes. Users are responsible for complying with local laws and regulations.

---

## 🎯 Key Benefits of New Implementation

### ✅ Problems Solved
- **No More 403 Errors**: Automatic detection and DNS switching prevents Gradle cache corruption
- **Transparent Mirroring**: Zero-configuration repository redirection to Iranian mirrors
- **Seamless Development**: No more interrupted downloads or manual DNS changes
- **Intelligent Fallback**: Always tries to use the best available DNS configuration
- **User-Friendly**: GUI interface makes configuration and monitoring easy
- **Zero App Changes**: Apps work normally without modifying build configurations

### 🔄 How It Works
1. **Transparent Mirroring**: DNS redirects repository domains to Iranian mirror IPs
2. **Detection**: Monitors Google domains for 403 responses
3. **Analysis**: Tests Iranian DNS servers and mirrors for availability
4. **Switching**: Automatically switches to working DNS configuration
5. **Monitoring**: Continuous health checking and optimization
6. **Recovery**: Falls back gracefully when needed

### 💡 Best Practices
- **Enable Auto-Switching**: Let the system handle DNS optimization automatically
- **Regular Testing**: Use "Test All" to verify DNS server health
- **Monitor Logs**: Check activity log for any issues or optimizations
- **Update Regularly**: Keep v2rayN updated for latest Iranian DNS servers

**Note**: This setup is specifically optimized for Android development in Iran. **For the best experience, use the transparent mirroring configuration** - it requires no changes to your projects and automatically handles all repository redirection. The manual configuration files are provided as alternatives for users who prefer explicit control.
