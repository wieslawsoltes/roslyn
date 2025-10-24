# Throws Clause Demo Scripts

This directory contains helper scripts for testing and using the modified Roslyn compiler with throws clause support.

## ⚠️ CRITICAL: IDE vs Command Line

**The throws syntax ONLY works in IDEs (VS Code, Visual Studio, Rider), NOT in command-line builds!**

- ✅ **run-all-samples.sh**: Works (uses direct compiler invocation)
- ✅ **IDE development**: Works (uses NuGet packages)
- ❌ **`dotnet build`**: Fails (uses standard .NET SDK)

See `IDE-USAGE-COMPLETE-GUIDE.md` in the repo root for full explanation.

## Scripts Overview

### 1. run-all-samples.sh

**Purpose:** Compile and run all throws clause sample programs in one go.

**Usage:**
```bash
cd samples/ThrowsClauseDemo/scripts
./run-all-samples.sh
```

**What it does:**
- Compiles all 6 sample programs using the modified compiler
- Runs the successfully compiled programs
- Shows which samples should produce errors (CS9340-CS9343)
- Provides a summary of pass/fail results

**Sample programs tested:**
1. `MinimalTest.cs` - Basic syntax demonstration
2. `ThrowsTypeTest.cs` - Symbol binding and API usage
3. `ValidationSuccessTest.cs` - Valid throws clause scenarios
4. `ValidationTest.cs` - CS9340/CS9341 error validation
5. `OverrideValidationTest.cs` - CS9342 override error validation
6. `InterfaceImplementationTest.cs` - CS9343 interface error validation

**Expected output:**
```
=== Throws Clause Sample Programs ===
...
Summary: 6/6 samples passed
✓ All samples completed successfully!
```

---

### 2. publish-compiler.sh

**Purpose:** Package the modified Roslyn compiler as NuGet packages for use in your IDE.

**Usage:**
```bash
cd samples/ThrowsClauseDemo/scripts
./publish-compiler.sh [output-directory]
```

**Arguments:**
- `output-directory` (optional): Directory to store NuGet packages (default: `artifacts/nuget`)

**What it does:**
1. Builds the compiler in Release mode
2. Creates NuGet packages for:
   - `Microsoft.CodeAnalysis.Core` (5.0.0-dev)
   - `Microsoft.CodeAnalysis.CSharp` (5.0.0-dev)
3. Creates a `nuget.config` file for easy project configuration
4. Provides instructions for using the packages in various IDEs

**Output:**
- NuGet packages: `artifacts/nuget/*.nupkg`
- NuGet config: `artifacts/nuget/nuget.config`

**After running, you can:**

#### Option 1: Use in a specific project
```bash
cp artifacts/nuget/nuget.config /path/to/your/project/
```

#### Option 2: Add as global NuGet source
```bash
dotnet nuget add source "$(pwd)/artifacts/nuget" --name RoslynThrowsClause
```

#### Option 3: Use in Visual Studio
1. Tools → NuGet Package Manager → Package Manager Settings
2. Package Sources → Add new source
3. Name: `RoslynThrowsClause`
4. Source: `/path/to/roslyn/artifacts/nuget`

---

### 3. test-in-ide.sh

**Purpose:** Create a ready-to-use test project configured with the modified compiler for IDE testing.

**Usage:**
```bash
cd samples/ThrowsClauseDemo/scripts
./test-in-ide.sh [project-name]
```

**Arguments:**
- `project-name` (optional): Name for the test project (default: `ThrowsClauseTest`)

**What it does:**
1. Checks if NuGet packages exist (runs `publish-compiler.sh` if needed)
2. Creates a new .NET 9.0 console project
3. Configures the project to use the modified compiler
4. Adds sample code demonstrating throws clause features
5. Builds and runs the project to verify it works

**Output:**
- Project directory: `artifacts/test-projects/[project-name]/`
- Project files:
  - `[project-name].csproj` - Project file referencing local Roslyn
  - `nuget.config` - NuGet configuration
  - `Program.cs` - Sample code with throws clauses
  - `README.md` - Project documentation

**Opening in IDEs:**

```bash
# Visual Studio Code
code artifacts/test-projects/ThrowsClauseTest

# Visual Studio
open artifacts/test-projects/ThrowsClauseTest/ThrowsClauseTest.csproj

# JetBrains Rider
rider artifacts/test-projects/ThrowsClauseTest/ThrowsClauseTest.csproj
```

---

## Prerequisites

### Required
- .NET 9.0 SDK installed
- Bash shell (macOS/Linux) or Git Bash (Windows)
- Modified Roslyn compiler built at `artifacts/publish/csc/csc.dll`

### Building the compiler first
If you haven't built the compiler yet:

```bash
cd /path/to/roslyn
dotnet publish src/Compilers/CSharp/csc/AnyCpu/csc.csproj \
  -c Debug \
  -f net9.0 \
  -o artifacts/publish/csc \
  --self-contained false
```

---

## Quick Start Guide

### 1. Test all samples locally
```bash
cd samples/ThrowsClauseDemo/scripts
chmod +x *.sh  # Make scripts executable (first time only)
./run-all-samples.sh
```

### 2. Create NuGet packages for IDE testing
```bash
./publish-compiler.sh
```

### 3. Create and test a project in your IDE
```bash
./test-in-ide.sh MyThrowsTest
code ../../../artifacts/test-projects/MyThrowsTest
```

---

## Troubleshooting

### "Compiler not found" error
**Solution:** Build the compiler first:
```bash
cd ../..  # Go to roslyn root
dotnet publish src/Compilers/CSharp/csc/AnyCpu/csc.csproj -c Debug -f net9.0 -o artifacts/publish/csc
```

### ".NET version not found" error
**Solution:** The script will try to find any .NET 9.x version. If none found, install .NET 9 SDK:
```bash
# macOS
brew install --cask dotnet-sdk

# Windows
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0

# Linux
# Follow instructions at: https://learn.microsoft.com/dotnet/core/install/linux
```

### "Permission denied" when running scripts
**Solution:** Make scripts executable:
```bash
chmod +x *.sh
```

### NuGet restore fails in test-in-ide.sh
**Solution:** Clear NuGet cache and try again:
```bash
dotnet nuget locals all --clear
./test-in-ide.sh
```

### IDE doesn't recognize throws keyword
**Possible causes:**
1. Project isn't using the local NuGet packages
   - Check that `nuget.config` exists in project directory
   - Verify package source points to correct directory
2. IDE cache needs refresh
   - Visual Studio: Tools → Options → Projects and Solutions → Unload and reload project
   - VS Code: Reload window (Cmd/Ctrl+Shift+P → "Reload Window")
   - Rider: File → Invalidate Caches → Invalidate and Restart

---

## Examples

### Run all samples and see results
```bash
$ ./run-all-samples.sh

=== Throws Clause Sample Programs ===
Compiler: /path/to/roslyn/artifacts/publish/csc/csc.dll
Runtime: /usr/local/share/dotnet/shared/Microsoft.NETCore.App/9.0.6

-----------------------------------
Sample: MinimalTest
Description: Basic throws clause syntax
Expected: SUCCESS

Compiling...
✓ Compilation succeeded
Running...

Testing throws clause syntax...

✓ Execution succeeded

-----------------------------------
Sample: ValidationTest
Description: CS9340 and CS9341 validation
Expected: ERRORS

Compiling...
ValidationTest.cs(15,36): error CS9340: Throws clause type 'string' does not derive from 'System.Exception'
ValidationTest.cs(20,36): error CS9340: Throws clause type 'int' does not derive from 'System.Exception'
ValidationTest.cs(25,70): error CS9341: Duplicate exception type 'IOException' in throws clause
...
✓ Expected errors reported

==================================
Summary: 6/6 samples passed

✓ All samples completed successfully!
```

### Package compiler for IDE use
```bash
$ ./publish-compiler.sh

=== Publishing Roslyn Compiler with Throws Clause ===
Roslyn root: /path/to/roslyn
Output directory: /path/to/roslyn/artifacts/nuget

Step 1: Building compiler...
✓ Compiler built successfully

Step 2: Building Microsoft.CodeAnalysis packages...
✓ Core package built
✓ CSharp package built

Step 3: Setting up local NuGet feed...
✓ NuGet config created at: /path/to/roslyn/artifacts/nuget/nuget.config

=== Publication Complete ===

Packages created in: /path/to/roslyn/artifacts/nuget

Available packages:
Microsoft.CodeAnalysis.5.0.0-dev.nupkg
Microsoft.CodeAnalysis.CSharp.5.0.0-dev.nupkg

To use in your IDE:
...
✓ Compiler ready for IDE testing!
```

### Create test project for IDE
```bash
$ ./test-in-ide.sh MyAwesomeTest

=== Creating Test Project for IDE ===
Project name: MyAwesomeTest
Project directory: /path/to/roslyn/artifacts/test-projects/MyAwesomeTest

Creating project files...
✓ Project created

Restoring NuGet packages...
✓ Restore succeeded

Building project...
✓ Build succeeded

Running project...
Testing throws clause in IDE...
Caught expected IOException: File not found
✓ Test completed!

=== Project Ready ===

Project location: /path/to/roslyn/artifacts/test-projects/MyAwesomeTest

To open in your IDE:

Visual Studio Code:
  code /path/to/roslyn/artifacts/test-projects/MyAwesomeTest
...
```

---

## Script Features

### Error Handling
All scripts use `set -e` to exit on errors and provide colored output for easy reading:
- 🟢 Green: Success messages
- 🔵 Blue: Section headers
- 🟡 Yellow: Warnings and progress
- 🔴 Red: Errors

### Automatic Detection
Scripts automatically detect:
- Available .NET versions
- Roslyn root directory
- Missing dependencies
- Compiler build status

### Cross-Platform Support
Scripts work on:
- ✅ macOS
- ✅ Linux
- ✅ Windows (with Git Bash or WSL)

---

## Advanced Usage

### Custom NuGet package version
Edit `publish-compiler.sh` and change:
```bash
/p:PackageVersion=5.0.0-dev
```
to your desired version.

### Custom runtime references
Edit `run-all-samples.sh` to use different .NET version:
```bash
DOTNET_VERSION="9.0.6"  # Change to your version
```

### Running specific samples
You can run individual samples manually:
```bash
cd samples/ThrowsClauseDemo
dotnet ../../artifacts/publish/csc/csc.dll \
  /nologo /t:exe /out:../../artifacts/Test.exe \
  /r:/path/to/System.Runtime.dll \
  /r:/path/to/System.Console.dll \
  MinimalTest.cs
dotnet ../../artifacts/Test.exe
```

---

## See Also

- [Throws Clause Specification](../../docs/features/throws-clause-specification.md)
- [Implementation Plan](../../docs/features/throws-clause-implementation-plan.md)
- [Complete Summary](../../docs/throws-clause-complete-summary.md)
- [Final Report](../../docs/throws-clause-final-report.md)

---

**Last Updated:** October 24, 2025
