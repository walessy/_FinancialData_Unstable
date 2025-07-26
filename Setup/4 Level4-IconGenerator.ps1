# 4 Level4-IconGenerator.ps1
# Automated Icon Generator for Trading Platforms - IMPROVED VERSION
# Creates Demo/Live text overlays on platform icons using pure PowerShell/.NET
# No external dependencies required

param(
    [string]$ConfigFile = "instances-config.json",
    [ValidateSet("Generate", "Status", "Remove", "Help")]
    [string]$Action = "Generate",
    [switch]$Force = $false,
    [switch]$Quiet = $false,
    [switch]$Verbose = $false
)

# Load required .NET assemblies with error handling
try {
    Add-Type -AssemblyName System.Drawing
    Add-Type -AssemblyName System.Windows.Forms
}
catch {
    Write-Host "Failed to load required .NET assemblies: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Enhanced logging function
function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("Info", "Warning", "Error", "Success", "Verbose")]
        [string]$Level = "Info",
        [switch]$NoNewline
    )
    
    if ($Level -eq "Verbose" -and -not $Verbose) { return }
    if ($Quiet -and $Level -ne "Error") { return }
    
    $colors = @{
        "Info" = "White"
        "Warning" = "Yellow" 
        "Error" = "Red"
        "Success" = "Green"
        "Verbose" = "Gray"
    }
    
    $prefix = switch ($Level) {
        "Error" { "[ERROR] " }
        "Warning" { "[WARN] " }
        "Success" { "[OK] " }
        "Verbose" { "[DEBUG] " }
        default { "[INFO] " }
    }
    
    Write-Host "$prefix$Message" -ForegroundColor $colors[$Level] -NoNewline:$NoNewline
}

# Enhanced configuration validation
function Test-Configuration {
    param([object]$Config)
    
    $requiredProperties = @("tradingRoot", "instances")
    foreach ($prop in $requiredProperties) {
        if (-not $Config.PSObject.Properties[$prop]) {
            throw "Missing required property: $prop"
        }
    }
    
    if (-not (Test-Path $Config.tradingRoot)) {
        throw "Trading root path does not exist: $($Config.tradingRoot)"
    }
    
    # Check if PlatformInstallations directory exists
    $platformInstallationsPath = Join-Path $Config.tradingRoot "PlatformInstallations"
    if (-not (Test-Path $platformInstallationsPath)) {
        throw "PlatformInstallations directory not found: $platformInstallationsPath"
    }
    
    if (-not $Config.instances -or $Config.instances.Count -eq 0) {
        throw "No instances defined in configuration"
    }
    
    # Validate each instance
    foreach ($instance in $Config.instances) {
        if (-not $instance.name) {
            throw "Instance missing required 'name' property"
        }
        if (-not $instance.platform) {
            throw "Instance '$($instance.name)' missing required 'platform' property"
        }
    }
    
    Write-Log "Configuration validation passed" -Level Success
}

# Enhanced icon extraction with better error handling
function Get-IconFromExecutable {
    param(
        [string]$ExecutablePath,
        [string]$OutputPath,
        [int]$IconSize = 48
    )
    
    Write-Log "Extracting icon from: $ExecutablePath" -Level Verbose
    
    if (-not (Test-Path $ExecutablePath)) {
        Write-Log "Executable not found: $ExecutablePath" -Level Warning
        return $false
    }
    
    try {
        # Create output directory if it doesn't exist
        $outputDir = Split-Path $OutputPath -Parent
        if (-not (Test-Path $outputDir)) {
            New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
            Write-Log "Created directory: $outputDir" -Level Verbose
        }
        
        # Extract the icon from the executable
        $icon = [System.Drawing.Icon]::ExtractAssociatedIcon($ExecutablePath)
        
        if ($icon) {
            # Create a bitmap with specified size for better quality
            $bitmap = New-Object System.Drawing.Bitmap $IconSize, $IconSize
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            
            # Set high quality rendering
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            
            # Draw the icon at the specified size
            $graphics.DrawIcon($icon, 0, 0)
            
            # Save the icon properly
            # Create a proper ICO file by using the original icon
            $ms = New-Object System.IO.MemoryStream
            $icon.Save($ms)
            [System.IO.File]::WriteAllBytes($OutputPath, $ms.ToArray())
            $ms.Dispose()
            
            # Clean up
            $graphics.Dispose()
            $bitmap.Dispose()
            $icon.Dispose()
            
            Write-Log "Icon extracted successfully to: $OutputPath" -Level Success
            return $true
        }
    }
    catch {
        Write-Log "Failed to extract icon: $($_.Exception.Message)" -Level Error
    }
    
    return $false
}

# Enhanced find custom base icon with better search patterns
function Find-CustomBaseIcon {
    param(
        [string]$PlatformPath,
        [string]$InstanceName
    )
    
    $shortcutImagePath = Join-Path $PlatformPath "ShortCutImage"
    
    if (Test-Path $shortcutImagePath) {
        # Check for custom icon patterns
        $iconPatterns = @(
            "$InstanceName*.ico",
            "custom*.ico",
            "base*.ico",
            "icon*.ico"
        )
        
        foreach ($pattern in $iconPatterns) {
            $iconFiles = Get-ChildItem $shortcutImagePath -Filter $pattern -ErrorAction SilentlyContinue
            if ($iconFiles) {
                $selectedIcon = $iconFiles | Select-Object -First 1
                Write-Log "Found custom base icon: $($selectedIcon.Name)" -Level Success
                return $selectedIcon.FullName
            }
        }
        
        # Look for any ico file as fallback
        $anyIcoFiles = Get-ChildItem $shortcutImagePath -Filter "*.ico" -ErrorAction SilentlyContinue
        if ($anyIcoFiles) {
            $selectedIcon = $anyIcoFiles | Select-Object -First 1
            Write-Log "Using first available ico file: $($selectedIcon.Name)" -Level Info
            return $selectedIcon.FullName
        }
    }
    
    return $null
}

# Enhanced icon extraction with custom icon support
function Get-BaseIcon {
    param(
        [string]$PlatformPath,
        [string]$InstanceName,
        [string]$OutputPath,
        [int]$IconSize = 48
    )
    
    Write-Log "Getting base icon for: $InstanceName" -Level Verbose
    
    # Create output directory if it doesn't exist
    $outputDir = Split-Path $OutputPath -Parent
    if (-not (Test-Path $outputDir)) {
        New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
        Write-Log "Created directory: $outputDir" -Level Verbose
    }
    
    # Priority 1: Check for custom icon in ShortCutImage folder
    $customIconPath = Find-CustomBaseIcon -PlatformPath $PlatformPath -InstanceName $InstanceName
    
    if ($customIconPath) {
        try {
            # Copy the custom icon as base icon
            Copy-Item $customIconPath $OutputPath -Force
            Write-Log "Using custom base icon from ShortCutImage: $(Split-Path $customIconPath -Leaf)" -Level Success
            return $true
        }
        catch {
            Write-Log "Failed to copy custom icon: $($_.Exception.Message)" -Level Warning
        }
    }
    
    # Priority 2: Check for .ico file in PlatformInstances folder (for MT4/MT5)
    $instancePath = Join-Path ([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetDirectoryName($PlatformPath))) "PlatformInstances\$InstanceName"
    if (Test-Path $instancePath) {
        $instanceIcons = Get-ChildItem $instancePath -Filter "*.ico" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($instanceIcons) {
            try {
                Copy-Item $instanceIcons.FullName $OutputPath -Force
                Write-Log "Using instance icon: $($instanceIcons.Name) from PlatformInstances" -Level Success
                return $true
            }
            catch {
                Write-Log "Failed to copy instance icon: $($_.Exception.Message)" -Level Warning
            }
        }
    }
    
    # Priority 3: Check for broker icon in [BrokerName]Ico folder
    $brokerName = ($InstanceName -split "_")[0]
    $brokerIcoPath = Join-Path ([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetDirectoryName($PlatformPath))) "${brokerName}Ico\image.ico"
    if (Test-Path $brokerIcoPath) {
        try {
            Copy-Item $brokerIcoPath $OutputPath -Force
            Write-Log "Using broker icon: image.ico from ${brokerName}Ico folder" -Level Success
            return $true
        }
        catch {
            Write-Log "Failed to copy broker icon: $($_.Exception.Message)" -Level Warning
        }
    }
    
    # Priority 4: Extract from executable as last resort
    Write-Log "No custom/instance/broker icon found, extracting from executable" -Level Verbose
    
    # Find the main executable based on platform type
    $executableName = switch ($PlatformPath -match "MT4|MT5|TraderEvolution") {
        {$_ -match "MT4"} { "terminal.exe" }
        {$_ -match "MT5"} { "terminal64.exe" }
        {$_ -match "TraderEvolution"} { "TradeTerminal.exe" }
        default { "*.exe" }
    }
    
    $executables = Get-ChildItem $PlatformPath -Filter $executableName -ErrorAction SilentlyContinue
    
    # If specific executable not found, look for any suitable executable
    if (-not $executables) {
        $executables = Get-ChildItem $PlatformPath -Filter "*.exe" | Where-Object { 
            $_.Name -notmatch "(uninstall|setup|installer|updater|metaeditor|evocode)" 
        }
    }
    
    if ($executables) {
        $executablePath = $executables[0].FullName
        return Get-IconFromExecutable -ExecutablePath $executablePath -OutputPath $OutputPath -IconSize $IconSize
    }
    
    Write-Log "No suitable executable found for icon extraction" -Level Warning
    return $false
}

# Enhanced account type detection with config support
function Get-AccountTypeInfo {
    param(
        [string]$InstanceName,
        [object]$InstanceConfig = $null
    )
    
    # Priority 1: Check for custom iconSettings in config
    if ($InstanceConfig -and $InstanceConfig.PSObject.Properties["iconSettings"] -and $InstanceConfig.iconSettings.text) {
        Write-Log "Using custom icon settings for $InstanceName" -Level Verbose
        return @{
            text = $InstanceConfig.iconSettings.text
            color = $InstanceConfig.iconSettings.color
            description = if ($InstanceConfig.iconSettings.description) { $InstanceConfig.iconSettings.description } else { "Custom Icon" }
        }
    }
    
    # Priority 2: Check for accountType field in config
    if ($InstanceConfig -and $InstanceConfig.PSObject.Properties["accountType"]) {
        $accountType = $InstanceConfig.accountType.ToLower()
        Write-Log "Using config accountType for ${InstanceName}: $accountType" -Level Verbose
        
        switch ($accountType) {
            "demo" { return @{ text = "D"; color = "Blue"; description = "Demo Account" } }
            "live" { return @{ text = "L"; color = "Red"; description = "Live Account" } }
            "paper" { return @{ text = "S"; color = "Green"; description = "Paper Trading" } }
            "sim" { return @{ text = "S"; color = "Green"; description = "Simulation" } }
            "simulation" { return @{ text = "S"; color = "Green"; description = "Simulation" } }
            "premium" { return @{ text = "P"; color = "Gold"; description = "Premium Account" } }
            "pro" { return @{ text = "P"; color = "Gold"; description = "Pro Account" } }
            "vip" { return @{ text = "V"; color = "Gold"; description = "VIP Account" } }
            "test" { return @{ text = "T"; color = "Gray"; description = "Test Account" } }
            "staging" { return @{ text = "T"; color = "Gray"; description = "Staging Account" } }
            "dev" { return @{ text = "T"; color = "Gray"; description = "Development Account" } }
            "development" { return @{ text = "T"; color = "Gray"; description = "Development Account" } }
            "production" { return @{ text = "L"; color = "Red"; description = "Production Account" } }
            "real" { return @{ text = "L"; color = "Red"; description = "Real Account" } }
            default { return @{ text = "?"; color = "Purple"; description = "Unknown Type ($accountType)" } }
        }
    }
    
    # Priority 3: Fallback to name detection
    $nameLower = $InstanceName.ToLower()
    Write-Log "Using name-based detection for $InstanceName" -Level Verbose
    
    # Check for disabled state
    if ($nameLower -match "disabled") {
        return @{ text = "X"; color = "Gray"; description = "Disabled Account" }
    }
    
    # Check for account type patterns in name
    if ($nameLower -match "demo|practice|training") {
        return @{ text = "D"; color = "Blue"; description = "Demo Account (from name)" }
    }
    elseif ($nameLower -match "live|real|production") {
        return @{ text = "L"; color = "Red"; description = "Live Account (from name)" }
    }
    elseif ($nameLower -match "paper|sim|simulation") {
        return @{ text = "S"; color = "Green"; description = "Simulation Account (from name)" }
    }
    elseif ($nameLower -match "premium|pro|vip|gold") {
        return @{ text = "P"; color = "Gold"; description = "Premium Account (from name)" }
    }
    elseif ($nameLower -match "test|staging|dev") {
        return @{ text = "T"; color = "Gray"; description = "Test Account (from name)" }
    }
    
    # Default for unknown types
    return @{
        text = "?"
        color = "Purple"
        description = "Unknown Account Type"
    }
}

# Enhanced icon generation with better error handling and custom text support
function Create-IconWithOverlay {
    param(
        [string]$BaseIconPath,
        [string]$OutputPath,
        [string]$Text,
        [string]$Color = "Red",
        [bool]$IsDisabled = $false
    )
    
    try {
        # Check if the base icon file exists and is valid
        if (-not (Test-Path $BaseIconPath)) {
            Write-Log "Base icon not found: $BaseIconPath" -Level Error
            return $false
        }
        
        # Load the base icon as an image
        $baseImage = [System.Drawing.Image]::FromFile($BaseIconPath)
        
        # Create a new 48x48 bitmap
        $finalBitmap = New-Object System.Drawing.Bitmap(48, 48)
        $graphics = [System.Drawing.Graphics]::FromImage($finalBitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        
        # Draw the base icon scaled to 48x48
        $graphics.DrawImage($baseImage, 0, 0, 48, 48)
        
        # Apply disabled effect if needed
        if ($IsDisabled) {
            # Create a semi-transparent gray overlay
            $grayBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(100, 128, 128, 128))
            $graphics.FillRectangle($grayBrush, 0, 0, 48, 48)
            $grayBrush.Dispose()
        }
        
        # Create colored circle background for text
        $circleSize = 20
        $circleX = 48 - $circleSize - 2
        $circleY = 48 - $circleSize - 2
        
        # Map color name to Color object
        $bgColor = switch ($Color) {
            "Red" { [System.Drawing.Color]::FromArgb(220, 220, 20, 60) }      # Crimson
            "Blue" { [System.Drawing.Color]::FromArgb(220, 30, 144, 255) }   # DodgerBlue
            "Green" { [System.Drawing.Color]::FromArgb(220, 50, 205, 50) }   # LimeGreen
            "Gold" { [System.Drawing.Color]::FromArgb(220, 255, 215, 0) }    # Gold
            "Gray" { [System.Drawing.Color]::FromArgb(220, 105, 105, 105) }  # DimGray
            "Purple" { [System.Drawing.Color]::FromArgb(220, 138, 43, 226) } # BlueViolet
            "Orange" { [System.Drawing.Color]::FromArgb(220, 255, 140, 0) }  # DarkOrange
            default { [System.Drawing.Color]::FromArgb(220, 220, 20, 60) }
        }
        
        # Draw circle background
        $bgBrush = New-Object System.Drawing.SolidBrush($bgColor)
        $graphics.FillEllipse($bgBrush, $circleX, $circleY, $circleSize, $circleSize)
        
        # Draw circle border
        $borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 2)
        $graphics.DrawEllipse($borderPen, $circleX, $circleY, $circleSize, $circleSize)
        
        # Draw text
        $fontSize = if ($Text.Length -eq 1) { 12 } elseif ($Text.Length -eq 2) { 10 } else { 8 }
        $font = New-Object System.Drawing.Font("Arial", $fontSize, [System.Drawing.FontStyle]::Bold)
        $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        
        # Measure and center text
        $textSize = $graphics.MeasureString($Text, $font)
        $textX = $circleX + ($circleSize - $textSize.Width) / 2
        $textY = $circleY + ($circleSize - $textSize.Height) / 2
        
        # Draw the text
        $graphics.DrawString($Text, $font, $textBrush, $textX, $textY)
        
        # Save as PNG first (which .NET handles well)
        $tempPngPath = [System.IO.Path]::GetTempFileName() + ".png"
        $finalBitmap.Save($tempPngPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        # Load PNG and save as ICO
        $pngImage = [System.Drawing.Image]::FromFile($tempPngPath)
        $icon = [System.Drawing.Icon]::FromHandle((New-Object System.Drawing.Bitmap $pngImage).GetHicon())
        
        # Save the icon using a FileStream
        $fs = New-Object System.IO.FileStream($OutputPath, [System.IO.FileMode]::Create)
        $icon.Save($fs)
        $fs.Close()
        
        # Clean up temp file
        Remove-Item $tempPngPath -Force -ErrorAction SilentlyContinue
        
        # Clean up objects
        $baseImage.Dispose()
        $graphics.Dispose()
        $finalBitmap.Dispose()
        $font.Dispose()
        $textBrush.Dispose()
        $bgBrush.Dispose()
        $borderPen.Dispose()
        $pngImage.Dispose()
        
        # Force garbage collection
        [System.GC]::Collect()
        [System.GC]::WaitForPendingFinalizers()
        
        return $true
    }
    catch {
        Write-Log "Error creating icon overlay: $($_.Exception.Message)" -Level Error
        return $false
    }
}

# Enhanced icon generation with progress tracking
function Invoke-IconGeneration {
    param(
        [array]$Instances,
        [string]$TradingRoot
    )
    
    Write-Log "Starting icon generation for $($Instances.Count) instances..." -Level Info
    
    $desktopTradingFolder = Join-Path ([Environment]::GetFolderPath("Desktop")) "Trading Platforms"
    $totalInstances = $Instances.Count
    $processedCount = 0
    $successCount = 0
    $errorCount = 0
    
    foreach ($instance in $Instances) {
        $processedCount++
        $progress = [Math]::Round(($processedCount / $totalInstances) * 100)
        
        Write-Log "[$processedCount/$totalInstances] Processing: $($instance.name) ($progress%)" -Level Info
        
        try {
            # Get platform path using the source field from config
            $platformFolder = $instance.source
            if (-not $platformFolder) {
                # Fallback: Parse instance name to get broker and platform
                $parts = $instance.name -split "_"
                if ($parts.Length -ge 2) {
                    $broker = $parts[0]
                    $platform = $parts[1]
                    $platformFolder = "${broker}_${platform}"
                } else {
                    # Last resort: use platform property
                    $platformFolder = $instance.platform
                }
            }
            
            # Get platform path and executable
            $platformPath = Join-Path (Join-Path $TradingRoot "PlatformInstallations") $platformFolder
            $shortcutImagePath = Join-Path $platformPath "ShortCutImage"
            
            if (-not (Test-Path $platformPath)) {
                Write-Log "Platform path not found: $platformPath" -Level Warning
                $errorCount++
                continue
            }
            
            # Find platform executable based on platform type
            $executableName = switch ($instance.platform) {
                "MT4" { "terminal.exe" }
                "MT5" { "terminal64.exe" }
                "TraderEvolution" { "TradeTerminal.exe" }
                default { "*.exe" }
            }
            
            $executables = Get-ChildItem $platformPath -Filter $executableName -ErrorAction SilentlyContinue
            
            # If specific executable not found, look for any suitable executable
            if (-not $executables) {
                $executables = Get-ChildItem $platformPath -Filter "*.exe" | Where-Object { 
                    $_.Name -notmatch "(uninstall|setup|installer|metaeditor|evocode)" 
                }
            }
            
            if (-not $executables) {
                Write-Log "No suitable executable found in: $platformPath" -Level Warning
                $errorCount++
                continue
            }
            
            $executablePath = $executables[0].FullName
            Write-Log "Using executable: $executablePath" -Level Verbose
            
            # Create ShortCutImage directory if needed
            if (-not (Test-Path $shortcutImagePath)) {
                New-Item -Path $shortcutImagePath -ItemType Directory -Force | Out-Null
                Write-Log "Created ShortCutImage directory" -Level Verbose
            }
            
            # Extract base icon
            $baseIconPath = Join-Path $shortcutImagePath "base_icon.ico"
            
            # If file exists and Force is set, try to delete it first
            if ((Test-Path $baseIconPath) -and $Force) {
                try {
                    Remove-Item $baseIconPath -Force -ErrorAction Stop
                    Write-Log "Removed existing base icon for regeneration" -Level Verbose
                }
                catch {
                    Write-Log "Could not remove existing icon: $($_.Exception.Message)" -Level Warning
                }
            }
            
            if ($Force -or -not (Test-Path $baseIconPath)) {
                if (-not (Get-BaseIcon -PlatformPath $platformPath -InstanceName $instance.name -OutputPath $baseIconPath)) {
                    Write-Log "Failed to get base icon for $($instance.name)" -Level Warning
                    $errorCount++
                    continue
                }
            }
            
            # Get account type info
            $accountInfo = Get-AccountTypeInfo -InstanceName $instance.name -InstanceConfig $instance
            Write-Log "Account type: $($accountInfo.description) [$($accountInfo.text) - $($accountInfo.color)]" -Level Verbose
            
            # Generate icon based on enabled/disabled state
            if ($instance.enabled) {
                $iconOutputPath = Join-Path $shortcutImagePath "$($instance.name).ico"
                
                if ($Force -or -not (Test-Path $iconOutputPath)) {
                    if (Create-IconWithOverlay -BaseIconPath $baseIconPath -OutputPath $iconOutputPath `
                            -Text $accountInfo.text -Color $accountInfo.color -IsDisabled $false) {
                        Write-Log "Created enabled icon: $iconOutputPath" -Level Success
                        $successCount++
                    }
                    else {
                        $errorCount++
                    }
                }
                else {
                    Write-Log "Enabled icon already exists (use -Force to regenerate)" -Level Verbose
                    $successCount++
                }
            }
            else {
                $iconOutputPath = Join-Path $shortcutImagePath "$($instance.name)_DISABLED.ico"
                
                if ($Force -or -not (Test-Path $iconOutputPath)) {
                    if (Create-IconWithOverlay -BaseIconPath $baseIconPath -OutputPath $iconOutputPath `
                            -Text "X" -Color "Gray" -IsDisabled $true) {
                        Write-Log "Created disabled icon: $iconOutputPath" -Level Success
                        $successCount++
                    }
                    else {
                        $errorCount++
                    }
                }
                else {
                    Write-Log "Disabled icon already exists (use -Force to regenerate)" -Level Verbose
                    $successCount++
                }
            }
            
            # Copy icon to desktop Trading Platforms folder
            if (Test-Path $iconOutputPath) {
                if (-not (Test-Path $desktopTradingFolder)) {
                    New-Item -Path $desktopTradingFolder -ItemType Directory -Force | Out-Null
                    Write-Log "Created desktop Trading Platforms folder" -Level Verbose
                }
                
                $desktopIconName = if ($instance.enabled) { "$($instance.name).ico" } else { "$($instance.name)_DISABLED.ico" }
                $desktopIconPath = Join-Path $desktopTradingFolder $desktopIconName
                
                Copy-Item -Path $iconOutputPath -Destination $desktopIconPath -Force
                Write-Log "Copied icon to desktop: $desktopIconName" -Level Verbose
            }
        }
        catch {
            Write-Log "Error processing $($instance.name): $($_.Exception.Message)" -Level Error
            $errorCount++
        }
    }
    
    Write-Log "`nIcon generation complete:" -Level Success
    Write-Log "  Total instances: $totalInstances" -Level Info
    Write-Log "  Successful: $successCount" -Level Success
    Write-Log "  Errors: $errorCount" -Level $(if ($errorCount -eq 0) { "Success" } else { "Warning" })
}

# Enhanced status display with icon validation
function Show-IconStatus {
    param(
        [array]$Instances,
        [string]$TradingRoot
    )
    
    Write-Log "`n=== Icon Status Report ===" -Level Success
    
    $desktopTradingFolder = Join-Path ([Environment]::GetFolderPath("Desktop")) "Trading Platforms"
    $totalIcons = 0
    $enabledIcons = 0
    $disabledIcons = 0
    
    foreach ($instance in $Instances) {
        # Get platform path using the source field from config
        $platformFolder = $instance.source
        if (-not $platformFolder) {
            # Fallback: Parse instance name to get broker and platform
            $parts = $instance.name -split "_"
            if ($parts.Length -ge 2) {
                $broker = $parts[0]
                $platform = $parts[1]
                $platformFolder = "${broker}_${platform}"
            } else {
                # Last resort: use platform property
                $platformFolder = $instance.platform
            }
        }
        
        $platformPath = Join-Path (Join-Path $TradingRoot "PlatformInstallations") $platformFolder
        $shortcutImagePath = Join-Path $platformPath "ShortCutImage"
        
        Write-Log "`nInstance: $($instance.name)" -Level Info
        Write-Log "  Platform: $($instance.platform)" -Level Verbose
        Write-Log "  Status: $(if ($instance.enabled) { 'Enabled' } else { 'Disabled' })" -Level $(if ($instance.enabled) { "Success" } else { "Warning" })
        
        $accountInfo = Get-AccountTypeInfo -InstanceName $instance.name -InstanceConfig $instance
        Write-Log "  Account Type: $($accountInfo.description) [$($accountInfo.text) - $($accountInfo.color)]" -Level Verbose
        
        # Show account type source if present
        if ($instance.PSObject.Properties["accountType"]) {
            Write-Log "  Config accountType: '$($instance.accountType)'" -Level Info
        }
        if ($instance.PSObject.Properties["iconSettings"] -and $instance.iconSettings.text) {
            Write-Log "  Custom Icon Settings: Text='$($instance.iconSettings.text)', Color='$($instance.iconSettings.color)'" -Level Info
        }
        
        # Check for icons
        $baseIcon = Join-Path $shortcutImagePath "base_icon.ico"
        $customIcon = Find-CustomBaseIcon -PlatformPath $platformPath -InstanceName $instance.name
        $enabledIcon = Join-Path $shortcutImagePath "$($instance.name).ico"
        $disabledIcon = Join-Path $shortcutImagePath "$($instance.name)_DISABLED.ico"
        $desktopIcon = Join-Path $desktopTradingFolder "$(if ($instance.enabled) { $instance.name } else { "$($instance.name)_DISABLED" }).ico"
        
        Write-Log "  Icons:" -Level Verbose
        if ($customIcon) {
            Write-Log "    Custom Base Icon: [OK] $(Split-Path $customIcon -Leaf)" -Level Success
        }
        Write-Log "    Generated Base Icon: $(if (Test-Path $baseIcon) { '[OK] Present' } else { '[MISSING] Not Found' })" -Level Verbose
        
        if ($instance.enabled) {
            Write-Log "    Enabled Icon: $(if (Test-Path $enabledIcon) { '[OK] Present' } else { '[MISSING] Not Found' })" -Level $(if (Test-Path $enabledIcon) { "Success" } else { "Warning" })
            if (Test-Path $enabledIcon) { $enabledIcons++; $totalIcons++ }
        }
        else {
            Write-Log "    Disabled Icon: $(if (Test-Path $disabledIcon) { '[OK] Present' } else { '[MISSING] Not Found' })" -Level $(if (Test-Path $disabledIcon) { "Success" } else { "Warning" })
            if (Test-Path $disabledIcon) { $disabledIcons++; $totalIcons++ }
        }
        
        Write-Log "    Desktop Copy: $(if (Test-Path $desktopIcon) { '[OK] Present' } else { '[MISSING] Not Found' })" -Level $(if (Test-Path $desktopIcon) { "Success" } else { "Warning" })
    }
    
    Write-Log "`n=== Summary ===" -Level Success
    Write-Log "Total Icons: $totalIcons" -Level Info
    Write-Log "Enabled Icons: $enabledIcons" -Level Success
    Write-Log "Disabled Icons: $disabledIcons" -Level Warning
    Write-Log "Desktop Folder: $(if (Test-Path $desktopTradingFolder) { '[OK] Present' } else { '[MISSING] Not Found' })" -Level $(if (Test-Path $desktopTradingFolder) { "Success" } else { "Warning" })
}

# Remove all generated icons
function Remove-GeneratedIcons {
    param(
        [array]$Instances,
        [string]$TradingRoot
    )
    
    Write-Log "Removing all generated icons..." -Level Warning
    
    $desktopTradingFolder = Join-Path ([Environment]::GetFolderPath("Desktop")) "Trading Platforms"
    $removedCount = 0
    
    foreach ($instance in $Instances) {
        # Get platform path using the source field from config
        $platformFolder = $instance.source
        if (-not $platformFolder) {
            # Fallback: Parse instance name to get broker and platform
            $parts = $instance.name -split "_"
            if ($parts.Length -ge 2) {
                $broker = $parts[0]
                $platform = $parts[1]
                $platformFolder = "${broker}_${platform}"
            } else {
                # Last resort: use platform property
                $platformFolder = $instance.platform
            }
        }
        
        $platformPath = Join-Path (Join-Path $TradingRoot "PlatformInstallations") $platformFolder
        $shortcutImagePath = Join-Path $platformPath "ShortCutImage"
        
        if (Test-Path $shortcutImagePath) {
            # Remove generated icons (keep custom ones)
            $iconFiles = @(
                "base_icon.ico",
                "$($instance.name).ico",
                "$($instance.name)_DISABLED.ico"
            )
            
            foreach ($iconFile in $iconFiles) {
                $iconPath = Join-Path $shortcutImagePath $iconFile
                if (Test-Path $iconPath) {
                    Remove-Item $iconPath -Force
                    Write-Log "Removed: $iconFile" -Level Verbose
                    $removedCount++
                }
            }
        }
        
        # Remove desktop copies
        if (Test-Path $desktopTradingFolder) {
            $desktopIcons = @(
                "$($instance.name).ico",
                "$($instance.name)_DISABLED.ico"
            )
            
            foreach ($iconName in $desktopIcons) {
                $desktopIconPath = Join-Path $desktopTradingFolder $iconName
                if (Test-Path $desktopIconPath) {
                    Remove-Item $desktopIconPath -Force
                    Write-Log "Removed from desktop: $iconName" -Level Verbose
                    $removedCount++
                }
            }
        }
    }
    
    # Remove Trading Platforms folder if empty
    if ((Test-Path $desktopTradingFolder) -and (Get-ChildItem $desktopTradingFolder).Count -eq 0) {
        Remove-Item $desktopTradingFolder -Force
        Write-Log "Removed empty Trading Platforms folder from desktop" -Level Success
    }
    
    Write-Log "Removed $removedCount icon files total" -Level Success
}

# Main execution starts here
if (-not $Quiet) {
    Write-Log "=== Level 4 Dynamic Icon Generator (Enhanced) ===" -Level Success
    Write-Log "Action: $Action" -Level Info
    if ($Verbose) { Write-Log "Verbose logging enabled" -Level Verbose }
}

# Load and validate configuration
$configPath = Join-Path (Get-Location) $ConfigFile
if (-not (Test-Path $configPath)) {
    Write-Log "Configuration file not found: $configPath" -Level Error
    Write-Log "Please ensure instances-config.json exists in the current directory" -Level Warning
    exit 1
}

try {
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    Test-Configuration -Config $config
    $TradingRoot = $config.tradingRoot
    Write-Log "Configuration loaded successfully from: $configPath" -Level Success
}
catch {
    Write-Log "Configuration error: $($_.Exception.Message)" -Level Error
    exit 1
}

# Convert instances from config file (include disabled instances for dynamic management)
$Instances = @()
foreach ($instance in $config.instances) {
    $Instances += $instance
}

Write-Log "Loaded $($Instances.Count) instances from configuration" -Level Info

# Main action processing
switch ($Action) {
    "Generate" {
        Invoke-IconGeneration -Instances $Instances -TradingRoot $TradingRoot
    }
    
    "Status" {
        Show-IconStatus -Instances $Instances -TradingRoot $TradingRoot
    }
    
    "Remove" {
        Remove-GeneratedIcons -Instances $Instances -TradingRoot $TradingRoot
    }
    
    "Help" {
        Write-Host "`n=== Level 4 Icon Generator Help (Enhanced) ===" -ForegroundColor Green
        Write-Host ""
        Write-Host "USAGE:" -ForegroundColor Cyan
        Write-Host "  .\Setup\'4 Level4-IconGenerator.ps1' [-Action] <Action> [Options]"
        Write-Host ""
        Write-Host "ACTIONS:" -ForegroundColor Cyan
        Write-Host "  Generate  Create dynamic icons reflecting current enabled/disabled states" -ForegroundColor White
        Write-Host "  Status    Show detailed status for all instances with comprehensive info" -ForegroundColor White
        Write-Host "  Remove    Remove all generated icons (enabled & disabled)" -ForegroundColor White
        Write-Host "  Help      Show this help message" -ForegroundColor White
        Write-Host ""
        Write-Host "OPTIONS:" -ForegroundColor Cyan
        Write-Host "  -Force    Regenerate existing icons (useful after config changes)" -ForegroundColor White
        Write-Host "  -Quiet    Suppress non-essential output" -ForegroundColor White
        Write-Host "  -Verbose  Show detailed processing information" -ForegroundColor White
        Write-Host ""
        Write-Host "ENHANCED FEATURES:" -ForegroundColor Cyan
        Write-Host "  • High-quality icon rendering with anti-aliasing" -ForegroundColor White
        Write-Host "  • Improved error handling and logging" -ForegroundColor White
        Write-Host "  • Progress tracking for batch operations" -ForegroundColor White
        Write-Host "  • Enhanced account type detection" -ForegroundColor White
        Write-Host "  • Better text positioning and shadows" -ForegroundColor White
        Write-Host "  • Comprehensive status reporting" -ForegroundColor White
        Write-Host "  • Automatic cleanup of old icons" -ForegroundColor White
        Write-Host ""
        Write-Host "CUSTOMIZATION OPTIONS:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  Method 1 - Custom Base Icons (automatic detection):" -ForegroundColor White
        Write-Host "    1. Save a custom .ico file in platform's ShortCutImage folder" -ForegroundColor Gray
        Write-Host "    2. Example: PlatformInstallations\\Broker_TraderEvolution\\ShortCutImage\\custom.ico" -ForegroundColor Gray
        Write-Host "    3. Script automatically uses it as base for overlays" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  Method 2 - Config accountType Field (recommended):" -ForegroundColor White
        Write-Host "    Use existing 'accountType' field in instances:" -ForegroundColor Gray
        Write-Host "    \"accountType\": \"demo\"   → Blue 'D'" -ForegroundColor Gray
        Write-Host "    \"accountType\": \"live\"   → Red 'L'" -ForegroundColor Gray
        Write-Host "    \"accountType\": \"paper\"  → Green 'S'" -ForegroundColor Gray
        Write-Host "    Works with your existing config structure!" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  Method 3 - Custom Text/Color Overlays (maximum control):" -ForegroundColor White
        Write-Host "    Add 'iconSettings' to any instance for full customization:" -ForegroundColor Gray
        Write-Host "    {" -ForegroundColor Gray
        Write-Host "      \"name\": \"MyBroker_Instance\"," -ForegroundColor Gray
        Write-Host "      \"iconSettings\": {" -ForegroundColor Gray
        Write-Host "        \"text\": \"LIVE\"," -ForegroundColor Gray
        Write-Host "        \"color\": \"Red\"," -ForegroundColor Gray
        Write-Host "        \"description\": \"Live Trading Account\"" -ForegroundColor Gray
        Write-Host "      }" -ForegroundColor Gray
        Write-Host "    }" -ForegroundColor Gray
        Write-Host "    • Supports any single character or short text" -ForegroundColor Gray
        Write-Host "    • Available colors: Red, Blue, Green, Gold, Purple, Orange, etc." -ForegroundColor Gray
        Write-Host "    • Takes priority over automatic name-based detection" -ForegroundColor Gray
        Write-Host ""
        Write-Host "ACCOUNT TYPE DETECTION PRIORITY:" -ForegroundColor Cyan
        Write-Host "  1. Custom iconSettings (highest priority)" -ForegroundColor White
        Write-Host "  2. Config accountType field (recommended - works with your config!)" -ForegroundColor White
        Write-Host "  3. Automatic name detection (fallback)" -ForegroundColor White
        Write-Host "  4. Unknown type → Purple '?'" -ForegroundColor Gray
        Write-Host ""
        Write-Host "ACCOUNT TYPE DETECTION:" -ForegroundColor Cyan
        Write-Host "  • demo, practice, training → Blue 'D'" -ForegroundColor Blue
        Write-Host "  • live, real, production → Red 'L'" -ForegroundColor Red
        Write-Host "  • paper, sim, simulation → Green 'S'" -ForegroundColor Green
        Write-Host "  • premium, pro, vip, gold → Gold 'P'" -ForegroundColor Yellow
        Write-Host "  • test, staging, dev → Gray 'T'" -ForegroundColor Gray
        Write-Host ""
        Write-Host "EXAMPLES:" -ForegroundColor Cyan
        Write-Host "  .\Setup\'4 Level4-IconGenerator.ps1' -Action Generate -Verbose" -ForegroundColor Gray
        Write-Host "  .\Setup\'4 Level4-IconGenerator.ps1' -Action Status" -ForegroundColor Gray
        Write-Host "  .\Setup\'4 Level4-IconGenerator.ps1' -Action Generate -Force -Quiet" -ForegroundColor Gray
        return
    }
}

# Show enhanced quick usage (unless Help was called or Quiet mode)
if ($Action -ne "Help" -and -not $Quiet) {
    Write-Log "`n=== Quick Usage ===" -Level Info
    Write-Host ".\Setup\'4 Level4-IconGenerator.ps1' -Action Generate -Verbose  # Generate with detailed output"
    Write-Host ".\Setup\'4 Level4-IconGenerator.ps1' -Action Status           # Comprehensive status check"
    Write-Host ".\Setup\'4 Level4-IconGenerator.ps1' -Action Generate -Force   # Force regeneration"
    Write-Host ".\Setup\'3 trading_manager.ps1' -Action Install             # Apply icons to shortcuts"
    Write-Host "`nEnhanced Features:" -ForegroundColor Gray
    Write-Host "  • Use -Verbose for detailed processing information" -ForegroundColor Gray
    Write-Host "  • Automatic cleanup of icons when state changes" -ForegroundColor Gray
    Write-Host "  • High-quality rendering with anti-aliasing" -ForegroundColor Gray
    Write-Host "  • Progress tracking for large configurations" -ForegroundColor Gray
    Write-Host "  • Custom icon support: Place .ico files in ShortCutImage folders" -ForegroundColor Gray
    Write-Host "  • Config integration: Uses existing 'accountType' fields" -ForegroundColor Gray
    Write-Host "  • Custom overlays: Add 'iconSettings' for full control" -ForegroundColor Gray
}