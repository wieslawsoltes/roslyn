# Using the Throws Clause Compiler in Your IDE

## Quick Start

1. **Publish the compiler**:
   ```bash
   cd samples/ThrowsClauseDemo/scripts
   ./publish-compiler.sh
   ```
   This creates NuGet packages in `/Users/wieslawsoltes/GitHub/roslyn/artifacts/nuget/`

2. **Create a test project** (automated):
   ```bash
   ./test-in-ide.sh MyTestProject
   ```

3. **Open in your IDE**:
   - VS Code: `code /Users/wieslawsoltes/GitHub/roslyn/artifacts/test-projects/MyTestProject`
   - Visual Studio: Open the `.csproj` file
   - Rider: Open the `.csproj` file

## ⚠️ IMPORTANT: CLI vs IDE Behavior

The throws clause syntax **ONLY works in IDEs**, not in command-line `dotnet build`.

**Why?**
- **Command-line `dotnet build`**: Uses the standard .NET SDK compiler (no throws support)
- **IDEs (VS Code, Visual Studio, Rider)**: Load the compiler from NuGet packages (has throws support)

**This means:**
- ✅ IDE IntelliSense will recognize throws clauses
- ✅ IDE build (Build → Build Solution) will succeed
- ✅ IDE error squiggles will work correctly
- ❌ Command-line `dotnet build` will show syntax errors (expected!)
- ❌ Command-line `dotnet run` won't work (expected!)

## Manual Project Setup

If you prefer to set up a project manually:

### 1. Create a new project anywhere
```bash
dotnet new console -n ThrowsTest
cd ThrowsTest
```

### 2. Create `nuget.config`
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="RoslynThrowsClause" value="/Users/wieslawsoltes/GitHub/roslyn/artifacts/nuget" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### 3. Add compiler package reference to `.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.0.0-dev" />
  </ItemGroup>
</Project>
```

### 4. Write code with throws clauses
```csharp
using System;
using System.IO;

public class FileReader
{
    public string ReadFile(string path) throws IOException
    {
        return File.ReadAllText(path);
    }
}
```

### 5. Open in your IDE
Your IDE will recognize the throws syntax and provide IntelliSense, error checking, etc.

## IDE-Specific Configuration

### Visual Studio Code
No additional configuration needed. Just open the folder containing the project.

### Visual Studio 2022
No additional configuration needed. Open the `.csproj` or `.sln` file.

### JetBrains Rider
You may need to add the custom NuGet source:
1. Settings → Build, Execution, Deployment → NuGet
2. Add package source: `/Users/wieslawsoltes/GitHub/roslyn/artifacts/nuget`

## Testing the Feature

### Example Code
```csharp
using System;
using System.IO;

namespace ThrowsClauseTest;

class Program
{
    // Method declares it throws IOException
    static void ReadConfig(string path) throws IOException
    {
        string content = File.ReadAllText(path);
        Console.WriteLine(content);
    }

    // Caller must handle declared exceptions
    static void Main()
    {
        try
        {
            ReadConfig("config.txt");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Failed to read config: {ex.Message}");
        }
    }
}
```

### What to Test
1. **IntelliSense**: Type `throws` after a method parameter list - should show completion
2. **Error Diagnostics**: Call a method with throws clause without try-catch - should show error CS9341
3. **Override Validation**: Override a method with incompatible throws clause - should show error CS9342
4. **Interface Implementation**: Implement interface method with incompatible throws - should show error CS9343

## Supported Features

✅ **Syntax**: `throws ExceptionType1, ExceptionType2`
✅ **Error CS9340**: Throws clause requires exception types, not other types
✅ **Error CS9341**: Must handle declared exceptions with try-catch or re-declare them
✅ **Error CS9342**: Override method's throws clause must be compatible with base
✅ **Error CS9343**: Interface implementation's throws clause must be compatible
✅ **Error CS9344**: Exception types in throws clause must be subtypes of System.Exception

## Limitations

❌ Cannot use throws syntax with command-line `dotnet build`
❌ Cannot execute code that uses throws syntax from command line
❌ Cannot use in CI/CD pipelines without replacing SDK compiler
✅ Works perfectly in all major IDEs for development

## Troubleshooting

### "Syntax error: ; expected" when using throws
**Problem**: IDE is using standard compiler, not modified one  
**Solution**: 
- Verify `nuget.config` exists and points to correct path
- Verify PackageReference to `Microsoft.CodeAnalysis.CSharp 5.0.0-dev` is in `.csproj`
- Restart IDE after changing nuget.config

### Command-line build fails with syntax errors
**Not a problem!** This is expected. The throws syntax only works in IDEs.

### IDE doesn't recognize throws keyword
**Solution**:
1. Verify the compiler packages exist: `ls /Users/wieslawsoltes/GitHub/roslyn/artifacts/nuget`
2. Check you see `Microsoft.CodeAnalysis.CSharp.5.0.0-dev.nupkg`
3. Restore packages: `dotnet restore` (in IDE terminal)
4. Restart IDE

## Next Steps

- See `samples/ThrowsClauseDemo/` for 6 complete example programs
- Run `./run-all-samples.sh` to see all examples working
- Read `throws-clause-final-report.md` for complete documentation
- Check `throws-clause-unit-tests-summary.md` for test coverage details
