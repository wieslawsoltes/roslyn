# Throws Clause - IDE Quick Start

## TL;DR

```bash
# 1. Publish compiler (already done!)
cd samples/ThrowsClauseDemo/scripts && ./publish-compiler.sh

# 2. Create test project
./test-in-ide.sh MyProject

# 3. Open in VS Code
code /Users/wieslawsoltes/GitHub/roslyn/artifacts/test-projects/MyProject
```

## ⚠️ THE MOST IMPORTANT THING TO KNOW

**CLI builds WILL FAIL - this is NORMAL and EXPECTED!**

```bash
dotnet build  # ❌ Shows syntax errors - uses standard compiler
```

**IDEs WILL WORK - this is where you use throws syntax!**

```
VS Code / Visual Studio / Rider  # ✅ Uses your modified compiler
```

## Why?

| Tool | Compiler | Throws Support |
|------|----------|----------------|
| `dotnet build` | .NET SDK | ❌ No |
| IDE | NuGet Package | ✅ Yes |

**You CANNOT use throws syntax in CLI builds. Only in IDEs.**

## Example

```csharp
// This works in IDEs, fails in CLI
public void ReadFile(string path) throws IOException
{
    File.ReadAllText(path);
}
```

## Files

- **Packages**: `/Users/wieslawsoltes/GitHub/roslyn/artifacts/nuget/*.nupkg`
- **Test Project**: `/Users/wieslawsoltes/GitHub/roslyn/artifacts/test-projects/IDETestProject/`
- **Full Guide**: `IDE-USAGE-COMPLETE-GUIDE.md`
- **Scripts**: `samples/ThrowsClauseDemo/scripts/README.md`

## Testing

1. Open test project in your IDE
2. Edit `Program.cs`
3. Use throws syntax
4. See IntelliSense work ✅
5. Build in IDE (not CLI) ✅
6. CLI build fails (expected) ❌

That's it!
