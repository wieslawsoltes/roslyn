#!/bin/bash
# Script to publish the modified Roslyn compiler to NuGet package format
# This creates packages that can be used in your IDE for testing
# Usage: ./publish-compiler.sh [output-directory]

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROSLYN_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Default output directory
OUTPUT_DIR="${1:-$ROSLYN_ROOT/artifacts/nuget}"

echo -e "${BLUE}=== Publishing Roslyn Compiler with Throws Clause ===${NC}"
echo "Roslyn root: $ROSLYN_ROOT"
echo "Output directory: $OUTPUT_DIR"
echo ""

# Step 1: Build the compiler
echo -e "${YELLOW}Step 1: Building compiler...${NC}"
cd "$ROSLYN_ROOT"

if ! dotnet publish src/Compilers/CSharp/csc/AnyCpu/csc.csproj \
    -c Release \
    -f net9.0 \
    -o artifacts/publish/csc \
    --self-contained false; then
    echo -e "${RED}✗ Compiler build failed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Compiler built successfully${NC}"
echo ""

# Step 2: Build Microsoft.CodeAnalysis packages
echo -e "${YELLOW}Step 2: Building Microsoft.CodeAnalysis packages...${NC}"

# Build Core package
if ! dotnet pack src/Compilers/Core/Portable/Microsoft.CodeAnalysis.csproj \
    -c Release \
    -o "$OUTPUT_DIR" \
    /p:PackageVersion=5.0.0-dev \
    /p:RepositoryCommit=dev; then
    echo -e "${RED}✗ Core package build failed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Core package built${NC}"

# Build CSharp package
if ! dotnet pack src/Compilers/CSharp/Portable/Microsoft.CodeAnalysis.CSharp.csproj \
    -c Release \
    -o "$OUTPUT_DIR" \
    /p:PackageVersion=5.0.0-dev \
    /p:RepositoryCommit=dev; then
    echo -e "${RED}✗ CSharp package build failed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ CSharp package built${NC}"
echo ""

# Step 3: Create local NuGet feed
echo -e "${YELLOW}Step 3: Setting up local NuGet feed...${NC}"

NUGET_CONFIG="$OUTPUT_DIR/nuget.config"

cat > "$NUGET_CONFIG" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="RoslynThrowsClause" value="$OUTPUT_DIR" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

echo -e "${GREEN}✓ NuGet config created at: $NUGET_CONFIG${NC}"
echo ""

# Step 4: Summary
echo -e "${BLUE}=== Publication Complete ===${NC}"
echo ""
echo -e "${GREEN}Packages created in: $OUTPUT_DIR${NC}"
echo ""
echo "Available packages:"
ls -lh "$OUTPUT_DIR"/*.nupkg 2>/dev/null || echo "  (no packages found)"
echo ""

echo -e "${BLUE}To use in your IDE:${NC}"
echo ""
echo "1. Copy nuget.config to your project directory:"
echo -e "   ${YELLOW}cp $NUGET_CONFIG /path/to/your/project/${NC}"
echo ""
echo "2. Or add the package source globally:"
echo -e "   ${YELLOW}dotnet nuget add source \"$OUTPUT_DIR\" --name RoslynThrowsClause${NC}"
echo ""
echo "3. Reference the packages in your project:"
echo -e "   ${YELLOW}<PackageReference Include=\"Microsoft.CodeAnalysis.CSharp\" Version=\"5.0.0-dev\" />${NC}"
echo ""
echo "4. If using Directory.Build.props, you can also add:"
cat <<'EOF'
   <PropertyGroup>
     <RoslynVersion>5.0.0-dev</RoslynVersion>
   </PropertyGroup>
   <ItemGroup>
     <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="$(RoslynVersion)" />
   </ItemGroup>
EOF
echo ""

echo -e "${BLUE}For Visual Studio:${NC}"
echo "1. Go to Tools → NuGet Package Manager → Package Manager Settings"
echo "2. Select 'Package Sources'"
echo "3. Click '+' to add a new source"
echo "4. Name: RoslynThrowsClause"
echo "5. Source: $OUTPUT_DIR"
echo "6. Click 'Update' then 'OK'"
echo ""

echo -e "${GREEN}✓ Compiler ready for IDE testing!${NC}"
