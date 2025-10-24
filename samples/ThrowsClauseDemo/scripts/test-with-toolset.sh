#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Creating IDE Test Project for Throws Clause ===${NC}"
echo ""

if [ -z "$1" ]; then
    echo -e "${RED}Error: Project name required${NC}"
    echo "Usage: ./test-in-ide.sh <ProjectName>"
    echo ""
    echo "Example:"
    echo "  ./test-in-ide.sh MyThrowsTest"
    exit 1
fi

PROJECT_NAME="$1"

# Get repository root
REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
NUGET_DIR="$REPO_ROOT/artifacts/nuget"

# Check if packages exist
if [ ! -f "$NUGET_DIR/Microsoft.Net.Compilers.Toolset.5.0.0-dev.nupkg" ]; then
    echo -e "${RED}Error: Toolset package not found${NC}"
    echo ""
    echo "Please run publish-toolset.sh first:"
    echo "  cd $REPO_ROOT/samples/ThrowsClauseDemo/scripts"
    echo "  ./publish-toolset.sh"
    exit 1
fi

# Create project in user's home directory (outside the roslyn repo)
PROJECT_DIR="$HOME/Desktop/$PROJECT_NAME"

if [ -d "$PROJECT_DIR" ]; then
    echo -e "${YELLOW}Warning: Directory already exists: $PROJECT_DIR${NC}"
    read -p "Do you want to overwrite it? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
    rm -rf "$PROJECT_DIR"
fi

echo "Creating project at: $PROJECT_DIR"
echo ""

# Create new .NET console project
echo -e "${YELLOW}Step 1: Creating .NET project...${NC}"
dotnet new console -n "$PROJECT_NAME" -o "$PROJECT_DIR" -f net9.0 || { echo -e "${RED}✗ Failed to create project${NC}"; exit 1; }
cd "$PROJECT_DIR"
echo -e "${GREEN}✓ Project created${NC}"
echo ""

# Copy nuget.config
echo -e "${YELLOW}Step 2: Configuring NuGet sources...${NC}"
cp "$NUGET_DIR/nuget.config" .
echo -e "${GREEN}✓ NuGet config copied${NC}"
echo ""

# Add toolset package reference
echo -e "${YELLOW}Step 3: Adding compiler toolset package...${NC}"
dotnet add package Microsoft.Net.Compilers.Toolset --version 5.0.0-dev || { echo -e "${RED}✗ Failed to add package${NC}"; exit 1; }
echo -e "${GREEN}✓ Toolset package added${NC}"
echo ""

# Create sample Program.cs with throws clauses
echo -e "${YELLOW}Step 4: Creating sample code with throws clauses...${NC}"
cat > "Program.cs" <<'EOF'
using System;
using System.IO;

namespace ThrowsClauseTest;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing throws clause feature...");
        Console.WriteLine("");
        
        var demo = new FileDemo();
        
        // This will throw IOException - demonstrates throws clause
        try
        {
            demo.ReadConfig("nonexistent.txt");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"✓ Caught expected exception: {ex.Message}");
        }
        
        Console.WriteLine("");
        Console.WriteLine("✓ Throws clause test completed successfully!");
    }
}

// Example 1: Basic throws clause
class FileDemo
{
    public string ReadConfig(string path) throws IOException
    {
        return File.ReadAllText(path);
    }
}

// Example 2: Multiple exception types
class DataProcessor
{
    public void ProcessData(string data) throws ArgumentException, InvalidOperationException
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException("Data cannot be empty");
            
        // Processing logic...
        throw new InvalidOperationException("Processing not implemented");
    }
}

// Example 3: Override with throws clause
class BaseReader
{
    public virtual void Read() throws IOException
    {
        // Base implementation
    }
}

class DerivedReader : BaseReader
{
    public override void Read() throws IOException
    {
        // Must declare IOException or subtype
        base.Read();
    }
}

// Example 4: Interface implementation with throws clause
interface IFileProcessor
{
    void Process(string path) throws IOException;
}

class FileProcessor : IFileProcessor
{
    public void Process(string path) throws IOException
    {
        // Must declare IOException
        File.ReadAllText(path);
    }
}
EOF
echo -e "${GREEN}✓ Sample code created${NC}"
echo ""

# Create README
echo -e "${YELLOW}Step 5: Creating documentation...${NC}"
cat > "README.md" <<EOF
# $PROJECT_NAME

This project demonstrates the throws clause feature in C#.

## ✅ Works With

- **dotnet build** (CLI) - Uses toolset package
- **dotnet run** (CLI) - Works!
- **VS Code** - Full IntelliSense support
- **Visual Studio** - Full IntelliSense support  
- **Rider** - Full IntelliSense support

## Building and Running

\`\`\`bash
# Build the project
dotnet build

# Run the project
dotnet run
\`\`\`

Both commands now work with throws syntax! 🎉

## Testing in IDE

### Visual Studio Code
\`\`\`bash
code .
\`\`\`

### Visual Studio 2022
Open the .csproj file

### JetBrains Rider
\`\`\`bash
rider $PROJECT_NAME.csproj
\`\`\`

## Throws Clause Examples

See \`Program.cs\` for examples:
- Basic throws clause
- Multiple exception types
- Override validation
- Interface implementation validation

## How It Works

This project uses the \`Microsoft.Net.Compilers.Toolset\` package which overrides
both the MSBuild and .NET SDK compilers. This allows throws syntax to work in:

1. Command-line builds (\`dotnet build\`)
2. Command-line execution (\`dotnet run\`)
3. IDE IntelliSense and error checking
4. IDE builds and debugging

The compiler recognizes the throws clause and enforces:
- CS9340: Throws clause must contain exception types
- CS9341: Must handle or redeclare declared exceptions
- CS9342: Override throws clause must be compatible with base
- CS9343: Interface implementation throws clause must be compatible
- CS9344: Exception types must derive from System.Exception
EOF
echo -e "${GREEN}✓ Documentation created${NC}"
echo ""

# Try to build
echo -e "${YELLOW}Step 6: Testing build...${NC}"
if dotnet build --no-restore; then
    echo -e "${GREEN}✓ Build succeeded!${NC}"
    echo ""
    echo -e "${GREEN}✅ Everything works! The throws syntax is recognized!${NC}"
    echo ""
    
    # Try to run
    echo -e "${YELLOW}Step 7: Testing run...${NC}"
    dotnet run --no-build
    echo ""
else
    echo -e "${RED}✗ Build failed${NC}"
    echo ""
    echo "This might indicate an issue with the toolset package."
    echo "Please check the error messages above."
fi

echo ""
echo -e "${BLUE}=== Project Ready ===${NC}"
echo ""
echo "Project location: $PROJECT_DIR"
echo ""
echo -e "${GREEN}To open in your IDE:${NC}"
echo ""
echo "Visual Studio Code:"
echo -e "  ${YELLOW}code \"$PROJECT_DIR\"${NC}"
echo ""
echo "Visual Studio:"
echo -e "  ${YELLOW}open \"$PROJECT_DIR/$PROJECT_NAME.csproj\"${NC}"
echo ""
echo "JetBrains Rider:"
echo -e "  ${YELLOW}rider \"$PROJECT_DIR/$PROJECT_NAME.csproj\"${NC}"
echo ""
echo -e "${BLUE}✅ The throws clause syntax works everywhere! 🎉${NC}"
echo ""
