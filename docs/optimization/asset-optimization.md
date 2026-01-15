# Asset Optimization Guide

This guide shows how to reduce Divine Ascension's mod size from 2.5MB to ~1.5MB (40% reduction).

## Current Size Breakdown

```
Component       Size    Percentage
────────────────────────────────────
PNG Textures:   2.1MB   52.5% ← BIGGEST OPPORTUNITY
DLL:            1.2MB   30.0%
Localization:   488KB   12.2%
OGG Sounds:     208KB   5.2%
────────────────────────────────────
TOTAL:          4.0MB   (compresses to 2.5MB zip)
```

## Optimization Strategies

### 1. PNG Image Optimization (Save ~1MB)

**Tool: optipng (lossless compression)**

```bash
# Install optipng (Fedora/RHEL)
sudo dnf install optipng

# Install optipng (Debian/Ubuntu)
sudo apt install optipng

# Optimize all PNG files
find DivineAscension/assets -name "*.png" -type f -exec optipng -o7 -strip all {} \;
```

**Expected Results:**

- 30-50% size reduction per image
- No quality loss (lossless)
- Typical reduction: 2.1MB → 1.0-1.4MB

**Alternative: pngquant (lossy but higher compression)**

```bash
# Install pngquant
sudo dnf install pngquant

# Optimize with slight quality loss (not noticeable in-game)
find DivineAscension/assets -name "*.png" -type f -exec pngquant --quality=85-95 --ext .png --force {} \;
```

Expected: 2.1MB → 600KB-800KB (60-70% reduction)

---

### 2. JSON Minification (Save ~150KB)

**Remove whitespace from localization files:**

```bash
# Using Python (built-in on most systems)
for file in DivineAscension/assets/divineascension/lang/*.json; do
    python3 -c "import json; json.dump(json.load(open('$file')), open('$file.tmp', 'w'), separators=(',',':'))"
    mv "$file.tmp" "$file"
done
```

**Expected Results:**

- 30-40% size reduction per JSON file
- Typical reduction: 488KB → 300KB

**Note:** Keep one human-readable version in source control, only minify in the release package.

---

### 3. OGG Audio Optimization (Save ~50KB)

**Lower bitrate slightly (already compressed):**

```bash
# Install ffmpeg if needed
sudo dnf install ffmpeg

# Re-encode at lower bitrate (96kbps is good for game sounds)
for file in DivineAscension/assets/divineascension/sounds/**/*.ogg; do
    ffmpeg -i "$file" -c:a libvorbis -b:a 96k "${file%.ogg}_optimized.ogg"
    mv "${file%.ogg}_optimized.ogg" "$file"
done
```

**Expected Results:**

- 20-30% size reduction
- Typical reduction: 208KB → 150KB
- Quality: Still excellent for game audio

---

### 4. DLL Optimization (Save ~100-200KB)

**Already optimized in Release build, but can enable:**

**Option A: IL Trimming (Safe)**

Add to `DivineAscension.csproj`:

```xml
<PropertyGroup>
    <PublishTrimmed>false</PublishTrimmed>
    <TrimMode>link</TrimMode>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
</PropertyGroup>
```

**Option B: Assembly Compression (Requires ILRepack or similar)**
Not recommended - adds complexity with minimal gain.

---

## Automated Optimization Script

Create `scripts/optimize-assets.sh`:

```bash
#!/bin/bash
set -e

echo "=== Divine Ascension Asset Optimization ==="
echo ""

# Check dependencies
if ! command -v optipng &> /dev/null; then
    echo "ERROR: optipng not installed. Install with: sudo dnf install optipng"
    exit 1
fi

if ! command -v python3 &> /dev/null; then
    echo "ERROR: python3 not installed"
    exit 1
fi

# Navigate to project root
cd "$(dirname "$0")/.."

# 1. Optimize PNGs
echo "1. Optimizing PNG images..."
original_png_size=$(find DivineAscension/assets -name "*.png" -type f -exec du -ch {} + | tail -1 | awk '{print $1}')
find DivineAscension/assets -name "*.png" -type f -exec optipng -o7 -strip all -quiet {} \;
optimized_png_size=$(find DivineAscension/assets -name "*.png" -type f -exec du -ch {} + | tail -1 | awk '{print $1}')
echo "   PNG: $original_png_size → $optimized_png_size"

# 2. Minify JSON (only in Releases folder, keep source readable)
echo "2. Minifying JSON localization files in Releases..."
if [ -d "Releases/divineascension/assets" ]; then
    find Releases/divineascension/assets -name "*.json" -type f | while read -r file; do
        python3 -c "import json; json.dump(json.load(open('$file')), open('$file.tmp', 'w'), separators=(',',':'))"
        mv "$file.tmp" "$file"
    done
    echo "   JSON minified in release package"
else
    echo "   Skipping (Releases folder not found - run after build)"
fi

# 3. Report savings
echo ""
echo "=== Optimization Complete ==="
echo "Run './build.sh' to create optimized release package"
```

Make executable: `chmod +x scripts/optimize-assets.sh`

---

## Integration with Cake Build

**Option 1: Pre-build optimization (Recommended)**

Run before packaging:

```bash
./scripts/optimize-assets.sh  # Optimize source assets
./build.sh                     # Build and package
```

**Option 2: Automated in Cake build**

Add to `CakeBuild/Program.cs` before `PackageTask`:

```csharp
[TaskName("OptimizeAssets")]
[IsDependentOn(typeof(BuildTask))]
public sealed class OptimizeAssetsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Optimizing PNG images...");

        // Optimize PNGs in build output
        var pngFiles = context.GetFiles($"./{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/assets/**/*.png");
        foreach (var file in pngFiles)
        {
            context.StartProcess("optipng", new ProcessSettings
            {
                Arguments = $"-o7 -strip all -quiet {file.FullPath}"
            });
        }

        context.Information("Minifying JSON files...");

        // Minify JSON in build output
        var jsonFiles = context.GetFiles($"./{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/assets/**/*.json");
        foreach (var file in jsonFiles)
        {
            var content = System.IO.File.ReadAllText(file.FullPath);
            var minified = Newtonsoft.Json.JsonConvert.SerializeObject(
                Newtonsoft.Json.JsonConvert.DeserializeObject(content),
                Newtonsoft.Json.Formatting.None
            );
            System.IO.File.WriteAllText(file.FullPath, minified);
        }
    }
}

// Update PackageTask dependency
[TaskName("Package")]
[IsDependentOn(typeof(OptimizeAssetsTask))] // Changed from BuildTask
public sealed class PackageTask : FrostingTask<BuildContext>
{
    // ... existing code ...
}
```

---

## Expected Results

**Before Optimization:**

- Uncompressed: 4.0MB
- Zipped: 2.5MB

**After Optimization:**

- PNG optimization: -1.0MB
- JSON minification: -150KB
- Audio optimization: -50KB
- **Total savings: ~1.2MB (30%)**

**Final Size:**

- Uncompressed: 2.8MB
- Zipped: **~1.5-1.8MB** (40% smaller)

---

## Best Practices

1. **Keep source assets unoptimized** - Only optimize in build/release
2. **Optimize before every release** - Run script before packaging
3. **Test after optimization** - Verify textures and sounds in-game
4. **Version control** - Don't commit optimized assets to git

---

## Troubleshooting

**Problem: Images look worse after optimization**

- Solution: Use optipng (lossless) instead of pngquant (lossy)

**Problem: JSON parse errors after minification**

- Solution: Validate JSON before minifying: `jq . file.json > /dev/null`

**Problem: Sounds are distorted**

- Solution: Use higher bitrate (128kbps instead of 96kbps)

---

## Additional Optimizations (Advanced)

### Remove Unused Assets

```bash
# Find assets that aren't referenced in code
find DivineAscension/assets -type f -name "*.png" | while read file; do
    basename=$(basename "$file" .png)
    if ! grep -r "$basename" DivineAscension --include="*.cs" --include="*.json" -q; then
        echo "Potentially unused: $file"
    fi
done
```

### Use Asset Atlases (Future)

- Combine multiple small icons into sprite sheets
- Reduces file count and improves compression
- Requires code changes to use texture coordinates

---

## Maintenance

Run optimization check regularly:

```bash
# Check current package size
./build.sh
ls -lh Releases/*.zip

# Optimize and rebuild
./scripts/optimize-assets.sh
./build.sh
ls -lh Releases/*.zip  # Compare sizes
```
