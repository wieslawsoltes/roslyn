#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Publishing Modified Roslyn Compiler (Toolset) ===${NC}"
echo ""
echo "This script will:"
echo "1. Build the modified Roslyn compiler in Release mode"
echo "2. Pack the compiler as Microsoft.Net.Compilers.Toolset package"
echo "3. Create a local NuGet configuration"
echo ""
echo "✅ The toolset package works with BOTH dotnet CLI and IDEs!"
echo ""

# Get the repository root (3 levels up from scripts directory)
REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
SAMPLES_DIR="$REPO_ROOT/samples/ThrowsClauseDemo"

# Default output directory
if [ -z "$1" ]; then
    NUGET_DIR="$REPO_ROOT/artifacts/nuget"
else
    NUGET_DIR="$1"
fi

echo "Repository root: $REPO_ROOT"
echo "Output directory: $NUGET_DIR"
echo ""

# Create output directory
mkdir -p "$NUGET_DIR"

# Step 1: Build compiler in Release mode
echo -e "${YELLOW}Step 1: Building compiler in Release mode...${NC}"
echo ""
cd "$REPO_ROOT"
dotnet build Compilers.slnf -c Release || { echo -e "${RED}✗ Build failed${NC}"; exit 1; }
echo -e "${GREEN}✓ Compiler built successfully${NC}"
echo ""

# Step 2: Build and pack the toolset package
echo -e "${YELLOW}Step 2: Building Microsoft.Net.Compilers.Toolset package...${NC}"
echo ""
echo "This package overrides both MSBuild and .NET SDK compilers."
echo "It works with: dotnet build, dotnet run, and all IDEs."
echo ""

# Build the toolset package
dotnet pack "$REPO_ROOT/src/NuGet/Microsoft.Net.Compilers.Toolset/AnyCpu/Microsoft.Net.Compilers.Toolset.Package.csproj" \
    -c Release \
    -o "$NUGET_DIR" \
    -p:PackageVersion=5.0.0-dev \
    || { echo -e "${RED}✗ Failed to pack Toolset${NC}"; exit 1; }
echo -e "${GREEN}✓ Toolset package built${NC}"
echo ""

# Step 3: Create nuget.config
echo -e "${YELLOW}Step 3: Creating nuget.config...${NC}"
cat > "$NUGET_DIR/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="RoslynThrowsClause" value="$NUGET_DIR" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
echo -e "${GREEN}✓ NuGet config created at: $NUGET_DIR/nuget.config${NC}"
echo ""

echo ""
echo -e "${GREEN}=== Publish Complete ===${NC}"
echo ""
echo "✓ Compiler built successfully"
echo "✓ Toolset package built"
echo "✓ NuGet config created"
echo ""
echo "Packages created in: $NUGET_DIR"
ls -lh "$NUGET_DIR"/*.nupkg 2>/dev/null | awk '{print "  - " $9 " (" $5 ")"}'
echo ""

echo -e "${BLUE}=== How to Use the Toolset Package ===${NC}"
echo ""
echo -e "${GREEN}✅ This package works with BOTH CLI and IDEs!${NC}"
echo ""
echo -e "${YELLOW}Option 1: Use the test-in-ide.sh script (Recommended)${NC}"
echo "  cd $SAMPLES_DIR/scripts"
echo "  ./test-in-ide.sh MyProjectName"
echo ""
echo -e "${YELLOW}Option 2: Manual Project Setup${NC}"
echo ""
echo "1. Create a new project:"
echo "   dotnet new console -n MyProject"
echo "   cd MyProject"
echo ""
echo "2. Copy the nuget.config:"
echo "   cp $NUGET_DIR/nuget.config ."
echo ""
echo "3. Add PackageReference to your .csproj:"
echo "   <ItemGroup>"
echo "     <PackageReference Include=\"Microsoft.Net.Compilers.Toolset\" Version=\"5.0.0-dev\" />"
echo "   </ItemGroup>"
echo ""
echo "4. Build and run:"
echo "   dotnet restore"
echo "   dotnet build   # ✅ Now works with throws syntax!"
echo "   dotnet run     # ✅ Now works!"
echo ""
echo "5. Or open in IDE:"
echo "   code .                    # VS Code"
echo "   open *.csproj             # Visual Studio"
echo "   rider *.csproj            # JetBrains Rider"
echo ""
echo -e "${GREEN}The throws clause syntax will work everywhere! 🎉${NC}"
echo ""
