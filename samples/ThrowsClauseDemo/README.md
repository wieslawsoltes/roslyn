# Throws Clause Demo

This directory contains sample applications demonstrating the new `throws` clause feature for C# method declarations.

## 🚀 Quick Start

### Run All Samples
```bash
cd scripts
./run-all-samples.sh
```

### Package for IDE Use
```bash
cd scripts
./publish-compiler.sh
```

### Create Test Project
```bash
cd scripts
./test-in-ide.sh MyTestProject
```

See [scripts/README.md](scripts/README.md) for detailed documentation.

---

## Overview

The `throws` clause allows developers to explicitly declare which exception types a method may throw, similar to Java's checked exception mechanism.

## Sample Programs

| File | Description | Expected Result |
|------|-------------|-----------------|
| **Program.cs** | Comprehensive demo (241 lines) | Full feature showcase |
| **MinimalTest.cs** | Basic syntax demonstration | ✅ Compiles and runs |
| **ThrowsTypeTest.cs** | Symbol binding and API | ✅ Compiles and runs |
| **ValidationSuccessTest.cs** | Valid throws clauses | ✅ Compiles and runs |
| **ValidationTest.cs** | CS9340/CS9341 errors | ⚠️ Expected errors |
| **OverrideValidationTest.cs** | CS9342 override errors | ⚠️ Expected errors |
| **InterfaceImplementationTest.cs** | CS9343 interface errors | ⚠️ Expected errors |

## Features Demonstrated

1. ✅ **Basic Throws Clause** - Single exception type declaration
2. ✅ **Multiple Exception Types** - Method declaring multiple exceptions
3. ✅ **Expression-Bodied Methods** - Throws clause with => syntax
4. ✅ **Inheritance** - Override methods with subset of base exceptions
5. ✅ **Interface Implementation** - Interface methods with throws clause
6. ✅ **Error Validation** - CS9340-CS9343 error codes

## Error Codes

| Code | Description |
|------|-------------|
| **CS9340** | Throws clause type must derive from System.Exception |
| **CS9341** | Duplicate exception type in throws clause |
| **CS9342** | Override throws exception not declared by base method |
| **CS9343** | Interface implementation throws exception not declared by interface |

---

## Building and Running

### Prerequisites

Build the modified Roslyn compiler first:

```bash
cd /Users/wieslawsoltes/GitHub/roslyn
dotnet publish src/Compilers/CSharp/csc/AnyCpu/csc.csproj \
  -c Debug -f net9.0 -o artifacts/publish/csc
```

### Manual Compilation

Compile individual samples:

```bash
cd samples/ThrowsClauseDemo

# Compile
dotnet ../../artifacts/publish/csc/csc.dll \
  /nologo /t:exe \
  /out:../../artifacts/MinimalTest.exe \
  /r:/usr/local/share/dotnet/shared/Microsoft.NETCore.App/9.0.6/System.Runtime.dll \
  /r:/usr/local/share/dotnet/shared/Microsoft.NETCore.App/9.0.6/System.Console.dll \
  MinimalTest.cs

# Run
dotnet ../../artifacts/MinimalTest.exe
```

### Using Scripts (Recommended)

The easiest way to test all samples:

```bash
cd scripts
./run-all-samples.sh
```

## Example Code

### Basic Usage

```csharp
public string ReadFile(string path) throws IOException
{
    if (!File.Exists(path))
    {
        throw new IOException($"File not found: {path}");
    }
    return File.ReadAllText(path);
}
```

### Multiple Exceptions

```csharp
public void ProcessFile(string path) throws ArgumentException, IOException
{
    if (string.IsNullOrEmpty(path))
    {
        throw new ArgumentException("Path cannot be null or empty");
    }
    
    string content = ReadFile(path);
    // Process content
}
```

### Expression-Bodied Method

```csharp
public void ValidateEmail(string email) throws ArgumentException =>
    throw new ArgumentException(
        string.IsNullOrEmpty(email) 
            ? "Email cannot be empty" 
            : "Invalid email format");
```

### Inheritance

```csharp
public abstract class BaseReader
{
    public abstract string Read() throws IOException, InvalidOperationException;
}

public class StreamReader : BaseReader
{
    // Override can declare subset of base exceptions
    public override string Read() throws IOException
    {
        // Implementation
    }
}
```

## Expected Output

When you run the demo, you should see:

```
=== C# Throws Clause Demo ===

Example 1: Basic file operation with throws clause
  Attempting to read: test.txt
Caught IOException: File not found: test.txt

Example 2: Method with multiple exception types
  Processing file: 
Caught ArgumentException: Path cannot be null or empty

Example 3: Safe operation with no exceptions
  Computing: 5 + 10
5 + 10 = 15

Example 4: Expression-bodied method
Caught ArgumentException: Email cannot be empty

=== Demo Complete ===
```

## Implementation Notes

This is a first iteration implementation with the following characteristics:

- ✅ Syntax recognition and parsing
- ✅ Support for methods only (not properties, constructors, etc.)
- ✅ Multiple exception types separated by commas
- ✅ Expression-bodied methods supported
- ⚠️ No runtime enforcement in this iteration
- ⚠️ Information is available in syntax tree but not yet in metadata

## Related Files

- Language Specification: `/docs/features/throws-clause-specification.md`
- Implementation Plan: `/docs/features/throws-clause-implementation-plan.md`

## License

This sample is part of the Roslyn project and follows the same MIT license.
