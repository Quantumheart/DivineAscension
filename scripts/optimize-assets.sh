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
original_png_size=$(find DivineAscension/assets -name "*.png" -type f -exec du -ch {} + 2>/dev/null | tail -1 | awk '{print $1}')
echo "   Original PNG size: $original_png_size"

png_count=0
find DivineAscension/assets -name "*.png" -type f | while read -r file; do
    optipng -o7 -strip all -quiet "$file"
    png_count=$((png_count + 1))
done

optimized_png_size=$(find DivineAscension/assets -name "*.png" -type f -exec du -ch {} + 2>/dev/null | tail -1 | awk '{print $1}')
echo "   Optimized PNG size: $optimized_png_size"
echo "   Optimized $(find DivineAscension/assets -name "*.png" -type f | wc -l) PNG files"

# 2. Minify JSON (only in Releases folder if it exists, keep source readable)
echo ""
echo "2. Checking for Releases folder to minify JSON..."
if [ -d "Releases/divineascension/assets" ]; then
    echo "   Minifying JSON localization files in Releases..."
    json_count=0
    find Releases/divineascension/assets -name "*.json" -type f | while read -r file; do
        original_size=$(stat -c%s "$file")
        python3 -c "import json; json.dump(json.load(open('$file')), open('$file.tmp', 'w'), separators=(',',':'))"
        if [ $? -eq 0 ]; then
            mv "$file.tmp" "$file"
            optimized_size=$(stat -c%s "$file")
            saved=$((original_size - optimized_size))
            echo "     $(basename "$file"): saved ${saved} bytes"
            json_count=$((json_count + 1))
        else
            rm -f "$file.tmp"
            echo "     $(basename "$file"): FAILED"
        fi
    done
    echo "   Minified $json_count JSON files"
else
    echo "   Skipping JSON minification (run after './build.sh' to optimize release package)"
fi

# 3. Report final results
echo ""
echo "=== Optimization Complete ==="
echo ""
echo "Next steps:"
echo "  1. Run './build.sh' to create release package"
echo "  2. Check Releases/*.zip for final optimized size"
echo ""
echo "Expected savings: 30-40% reduction in final zip size"
