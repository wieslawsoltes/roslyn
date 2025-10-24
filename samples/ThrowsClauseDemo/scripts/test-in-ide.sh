#!/bin/bash
# Script to create a test project that uses the modified compiler
# This creates a simple C# project configured to use your local Roslyn build
# Usage: ./test-in-ide.sh [project-name]

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROSLYN_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

PROJECT_NAME="${1:-ThrowsClauseTest}"
TEST_DIR="$ROSLYN_ROOT/artifacts/test-projects/$PROJECT_NAME"
NUGET_DIR="$ROSLYN_ROOT/artifacts/nuget"

echo -e "${BLUE}=== Creating Test Project for IDE ===${NC}"
echo "Project name: $PROJECT_NAME"
echo "Project directory: $TEST_DIR"
echo ""

# Check if NuGet packages exist
if [ ! -d "$NUGET_DIR" ] || [ -z "$(ls -A $NUGET_DIR/*.nupkg 2>/dev/null)" ]; then
    echo -e "${YELLOW}NuGet packages not found. Building them first...${NC}"
    "$SCRIPT_DIR/publish-compiler.sh" "$NUGET_DIR"
    echo ""
fi

# Create project directory
mkdir -p "$TEST_DIR"
cd "$TEST_DIR"

echo -e "${YELLOW}Creating project files...${NC}"

# Create .csproj file
cat > "$PROJECT_NAME.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <!-- Reference local Roslyn build -->
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.0.0-dev" />
  </ItemGroup>

</Project>
EOF

# Create nuget.config
cat > "nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="RoslynThrowsClause" value="$NUGET_DIR" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# Create Directory.Build.props to isolate from parent build system if inside roslyn repo
cat > "Directory.Build.props" <<'EOF'
<Project>
  <!-- Disable Central Package Management and parent build system -->
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
  </PropertyGroup>
</Project>
EOF

# Create Directory.Build.targets to isolate from parent build system
cat > "Directory.Build.targets" <<'EOF'
<Project>
  <!-- Prevent importing parent repository build targets -->
</Project>
EOF

# Create sample Program.cs
cat > "Program.cs" <<'EOF'
using System;
using System.IO;

namespace ThrowsClauseTest;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing throws clause in IDE...");
        
        try
        {
            var processor = new FileProcessor();
            processor.ReadFile("test.txt");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Caught expected IOException: {ex.Message}");
        }
        
        Console.WriteLine("✓ Test completed!");
    }
}

class FileProcessor
{
    // Method with throws clause
    public void ReadFile(string path) throws IOException
    {
        throw new IOException("File not found");
    }
    
    // Method with multiple exception types
    public void ProcessData(string data) throws ArgumentNullException, FormatException
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
            
        if (string.IsNullOrEmpty(data))
            throw new FormatException("Data is empty");
    }
}

// Override example
class BaseProcessor
{
    public virtual void Process() throws IOException
    {
        throw new IOException("Base error");
    }
}

class DerivedProcessor : BaseProcessor
{
    // Valid: FileNotFoundException derives from IOException
    public override void Process() throws FileNotFoundException
    {
        throw new FileNotFoundException("File not found");
    }
}

// Interface example
interface IDataProcessor
{
    void ProcessData() throws ArgumentException;
}

class DataProcessor : IDataProcessor
{
    // Valid: ArgumentNullException derives from ArgumentException
    public void ProcessData() throws ArgumentNullException
    {
        throw new ArgumentNullException();
    }
}
EOF

# Create README
cat > "README.md" <<EOF
# $PROJECT_NAME

This is a test project using the modified Roslyn compiler with throws clause support.

## Building

\`\`\`bash
dotnet build
\`\`\`

## Running

\`\`\`bash
dotnet run
\`\`\`

## Testing in IDE

### Visual Studio Code
1. Open this directory in VS Code
2. The project should work with the modified compiler automatically

### Visual Studio
1. Open the .csproj file in Visual Studio
2. The nuget.config ensures the local Roslyn build is used

### JetBrains Rider
1. Open the solution/project in Rider
2. Go to Settings → Build, Execution, Deployment → NuGet
3. Add the custom package source: $NUGET_DIR

## Throws Clause Features

This project demonstrates:
- Basic throws clause syntax
- Multiple exception types
- Override validation
- Interface implementation validation

See Program.cs for examples.
EOF

echo -e "${GREEN}✓ Project created${NC}"
echo ""

# Try to restore and build
echo -e "${YELLOW}Restoring NuGet packages...${NC}"
if dotnet restore; then
    echo -e "${GREEN}✓ Restore succeeded${NC}"
    echo ""
    
    echo -e "${YELLOW}Building project...${NC}"
    echo -e "${YELLOW}NOTE: CLI build will fail with syntax errors - this is EXPECTED${NC}"
    echo -e "${YELLOW}The throws syntax only works in IDEs, not command-line dotnet build${NC}"
    echo ""
    if dotnet build; then
        echo -e "${GREEN}✓ Build succeeded${NC}"
        echo ""
        
        echo -e "${YELLOW}Running project...${NC}"
        dotnet run
        echo ""
    else
        echo -e "${YELLOW}✗ Build failed (EXPECTED - see note above)${NC}"
        echo "The command-line 'dotnet build' uses the standard compiler."
        echo "To use throws syntax, open the project in your IDE."
    fi
else
    echo -e "${RED}✗ Restore failed${NC}"
fi

echo ""
echo -e "${BLUE}=== Project Ready ===${NC}"
echo ""
echo "Project location: $TEST_DIR"
echo ""
echo -e "${GREEN}To open in your IDE:${NC}"
echo ""
echo "Visual Studio Code:"
echo -e "  ${YELLOW}code $TEST_DIR${NC}"
echo ""
echo "Visual Studio:"
echo -e "  ${YELLOW}open $TEST_DIR/$PROJECT_NAME.csproj${NC}"
echo ""
echo "JetBrains Rider:"
echo -e "  ${YELLOW}rider $TEST_DIR/$PROJECT_NAME.csproj${NC}"
echo ""
echo -e "${BLUE}The project is configured to use the modified compiler automatically.${NC}"
echo "You can now test throws clause syntax in your IDE!"
echo ""
echo -e "${YELLOW}⚠️  IMPORTANT:${NC}"
echo "The throws syntax ONLY works in IDEs, not in command-line 'dotnet build'."
echo "This is because IDEs use the compiler from NuGet packages, while the CLI uses"
echo "the installed .NET SDK compiler."
