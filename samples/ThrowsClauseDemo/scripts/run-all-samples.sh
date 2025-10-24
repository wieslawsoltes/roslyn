#!/bin/bash
# Script to compile and run all throws clause sample programs
# Usage: ./run-all-samples.sh

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROSLYN_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
SAMPLES_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS_DIR="$ROSLYN_ROOT/artifacts"

# Compiler paths
CSC_DLL="$ARTIFACTS_DIR/publish/csc/csc.dll"

# Runtime references (adjust version if needed)
DOTNET_VERSION="9.0.6"
NETCORE_PATH="/usr/local/share/dotnet/shared/Microsoft.NETCore.App/$DOTNET_VERSION"
REF_RUNTIME="$NETCORE_PATH/System.Runtime.dll"
REF_CONSOLE="$NETCORE_PATH/System.Console.dll"
REF_CORELIB="$NETCORE_PATH/System.Private.CoreLib.dll"

# Check if compiler exists
if [ ! -f "$CSC_DLL" ]; then
    echo -e "${RED}Error: Compiler not found at $CSC_DLL${NC}"
    echo "Please build the compiler first:"
    echo "  cd $ROSLYN_ROOT"
    echo "  dotnet publish src/Compilers/CSharp/csc/AnyCpu/csc.csproj -c Debug -f net9.0 -o artifacts/publish/csc"
    exit 1
fi

# Check if .NET runtime exists
if [ ! -d "$NETCORE_PATH" ]; then
    echo -e "${YELLOW}Warning: .NET $DOTNET_VERSION not found${NC}"
    echo "Trying to find available versions..."
    NETCORE_BASE="/usr/local/share/dotnet/shared/Microsoft.NETCore.App"
    if [ -d "$NETCORE_BASE" ]; then
        LATEST_VERSION=$(ls "$NETCORE_BASE" | grep "^9\." | sort -V | tail -1)
        if [ -n "$LATEST_VERSION" ]; then
            echo -e "${GREEN}Found version: $LATEST_VERSION${NC}"
            NETCORE_PATH="$NETCORE_BASE/$LATEST_VERSION"
            REF_RUNTIME="$NETCORE_PATH/System.Runtime.dll"
            REF_CONSOLE="$NETCORE_PATH/System.Console.dll"
            REF_CORELIB="$NETCORE_PATH/System.Private.CoreLib.dll"
        else
            echo -e "${RED}No .NET 9 version found. Please install .NET 9 SDK.${NC}"
            exit 1
        fi
    fi
fi

echo -e "${BLUE}=== Throws Clause Sample Programs ===${NC}"
echo "Compiler: $CSC_DLL"
echo "Runtime: $NETCORE_PATH"
echo ""

# Create runtime config for executables
create_runtimeconfig() {
    local exe_path="$1"
    local config_path="${exe_path%.exe}.runtimeconfig.json"
    cat > "$config_path" <<EOF
{
  "runtimeOptions": {
    "tfm": "net9.0",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "9.0.0"
    }
  }
}
EOF
}

# Sample definitions: name, description, expected_result
declare -a SAMPLES=(
    "MinimalTest:Basic throws clause syntax:SUCCESS"
    "ThrowsTypeTest:Symbol binding and API:SUCCESS"
    "ValidationSuccessTest:Valid throws clauses:SUCCESS"
    "ValidationTest:CS9340 and CS9341 validation:ERRORS"
    "OverrideValidationTest:CS9342 override validation:ERRORS"
    "InterfaceImplementationTest:CS9343 interface validation:ERRORS"
)

SUCCESS_COUNT=0
TOTAL_COUNT=${#SAMPLES[@]}

for sample_info in "${SAMPLES[@]}"; do
    IFS=':' read -r sample_name sample_desc expected_result <<< "$sample_info"
    
    echo -e "${BLUE}-----------------------------------${NC}"
    echo -e "${BLUE}Sample: ${GREEN}$sample_name${NC}"
    echo -e "Description: $sample_desc"
    echo -e "Expected: $expected_result"
    echo ""
    
    # Compile
    echo -e "${YELLOW}Compiling...${NC}"
    OUTPUT_EXE="$ARTIFACTS_DIR/${sample_name}.exe"
    
    if dotnet "$CSC_DLL" \
        /nologo \
        /t:exe \
        "/out:$OUTPUT_EXE" \
        "/r:$REF_CORELIB" \
        "/r:$REF_RUNTIME" \
        "/r:$REF_CONSOLE" \
        "$SAMPLES_DIR/${sample_name}.cs" 2>&1; then
        
        if [ "$expected_result" == "ERRORS" ]; then
            echo -e "${RED}✗ UNEXPECTED: Compilation succeeded but errors were expected${NC}"
            continue
        fi
        
        echo -e "${GREEN}✓ Compilation succeeded${NC}"
        
        # Create runtime config
        create_runtimeconfig "$OUTPUT_EXE"
        
        # Run
        echo -e "${YELLOW}Running...${NC}"
        echo ""
        if dotnet "$OUTPUT_EXE"; then
            echo ""
            echo -e "${GREEN}✓ Execution succeeded${NC}"
            ((SUCCESS_COUNT++))
        else
            echo ""
            echo -e "${RED}✗ Execution failed${NC}"
        fi
    else
        if [ "$expected_result" == "ERRORS" ]; then
            echo -e "${GREEN}✓ Expected errors reported${NC}"
            ((SUCCESS_COUNT++))
        else
            echo -e "${RED}✗ Compilation failed${NC}"
        fi
    fi
    
    echo ""
done

echo -e "${BLUE}===================================${NC}"
echo -e "${BLUE}Summary: ${GREEN}$SUCCESS_COUNT${NC}/${TOTAL_COUNT} samples passed"
echo ""

if [ $SUCCESS_COUNT -eq $TOTAL_COUNT ]; then
    echo -e "${GREEN}✓ All samples completed successfully!${NC}"
    exit 0
else
    echo -e "${RED}✗ Some samples failed${NC}"
    exit 1
fi
