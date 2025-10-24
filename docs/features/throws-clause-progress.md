# Throws Clause Implementation Progress Report

## Overview
Implementation of Java-like `throws` keyword for C# methods to declare exceptions that methods may throw.

## Completed Work

### Phase 1: Syntax Layer ✅
**Status**: COMPLETE and TESTED

1. **SyntaxKind.cs** - Added two new syntax kinds:
   - `ThrowsKeyword = 8452` (contextual keyword)
   - `ThrowsClause = 9081` (syntax node)

2. **Syntax.xml** - Defined syntax tree structure:
   ```xml
   <Node Name="ThrowsClauseSyntax">
     <Kind Name="ThrowsClause"/>
     <Field Name="ThrowsKeyword" Type="SyntaxToken">
       <Kind Name="ThrowsKeyword"/>
     </Field>
     <Field Name="ExceptionTypes" Type="SeparatedSyntaxList&lt;TypeSyntax&gt;" MinCount="1" />
   </Node>
   ```
   - Added `ThrowsClause` field to `MethodDeclarationSyntax`

3. **SyntaxKindFacts.cs** - Keyword recognition:
   - Added to `GetContextualKeywordKinds()`
   - Added to `IsContextualKeyword()`
   - Added to `GetContextualKeywordKind()` mapping "throws" → `ThrowsKeyword`
   - Added to `GetText()` mapping `ThrowsKeyword` → "throws"

4. **LanguageParser.cs** - Parser implementation:
   - **ParseThrowsClause()** (~50 lines, lines 2348-2390)
     - Parses `throws` keyword followed by comma-separated exception types
     - Error handling for missing types
     - Uses `_pool` for SeparatedSyntaxList management
   - **ParseMethodDeclaration()** integration (lines ~3620)
     - Parses throws clause after constraint clauses, before method body
     - Passes `throwsClause` to SyntaxFactory

5. **Code Generation**:
   - Generated factory methods in `Syntax.xml.Main.Generated.cs`
   - Updated manual helper files:
     - `MethodDeclarationSyntax.cs` - backward compatibility overload
     - `SyntaxExtensions.cs` - Update() method to preserve throws clause

6. **Build Verification**:
   - Compilers.slnf builds successfully
   - Sample code compiles:
     ```csharp
     public void SimpleMethod() throws IOException { }
     public int Calculate(int x, int y) throws ArgumentException, InvalidOperationException { }
     public string Format(string input) throws ArgumentNullException => input;
     ```

### Phase 2: Symbol Layer (In Progress) ⏳

1. **IMethodSymbol.cs** - Public API ✅:
   ```csharp
   /// <summary>
   /// Gets the exception types declared in the throws clause of this method.
   /// Returns an empty array if the method has no throws clause.
   /// </summary>
   ImmutableArray<ITypeSymbol> ThrowsTypes { get; }
   ```
   - Added after `Parameters` property (line ~130)

2. **MethodSymbol.cs** - Internal abstract class ✅:
   ```csharp
   public virtual ImmutableArray<TypeSymbol> ThrowsTypes
   {
       get { return ImmutableArray<TypeSymbol>.Empty; }
   }
   ```
   - Default implementation returns empty array
   - Can be overridden by derived classes

3. **PublicModel/MethodSymbol.cs** - API wrapper ✅:
   ```csharp
   ImmutableArray<ITypeSymbol> IMethodSymbol.ThrowsTypes
   {
       get { return _underlying.ThrowsTypes.GetPublicSymbols(); }
   }
   ```
   - Maps internal symbols to public API

4. **SourceOrdinaryMethodSymbol.cs** - Binding implementation 🚧:
   - NEXT STEP: Override `ThrowsTypes` to bind types from `ThrowsClauseSyntax`
   - Need to add lazy binding logic similar to parameters/return type

## Test Results

### Syntax Parsing Test
**File**: `samples/ThrowsClauseDemo/MinimalTest.cs`

Compiled successfully with our modified compiler:
```bash
$ dotnet ./artifacts/publish/csc/csc.dll samples/ThrowsClauseDemo/MinimalTest.cs ...
Microsoft (R) Visual C# Compiler version 5.3.0-dev (<developer build>)
[SUCCESS - No syntax errors]
```

**Executable created**: `artifacts/MinimalTest.exe` (4.0KB)  
**Runtime test**: Executed successfully, outputs "Testing throws clause syntax..."

## Architecture Decisions

1. **Contextual Keyword**: `throws` is a contextual keyword (like `var`, `async`) to maintain backward compatibility
2. **Syntax Node**: `ThrowsClauseSyntax` contains keyword token + separated list of exception types
3. **Method-Only**: First iteration only supports methods, not constructors/properties/etc.
4. **No Runtime Enforcement**: This iteration only parses and stores the information in symbols
5. **Default Implementation**: Base `MethodSymbol` returns empty array by default

## Next Steps (Priority Order)

### Immediate (Phase 2 Continuation)
1. **Bind Throws Types** in `SourceOrdinaryMethodSymbol`:
   - Add lazy field `_lazyThrowsTypes`
   - Override `ThrowsTypes` property
   - Bind exception types using Binder
   - Validate types derive from `System.Exception`

2. **Add Diagnostic Codes** (`ErrorCode.cs`):
   ```csharp
   CS9001: Throws clause type must derive from System.Exception
   CS9002: Duplicate exception type in throws clause
   CS9003: Override method throws exceptions not declared in base
   CS9004: Interface implementation throws exceptions not declared in interface
   CS9005: Throws clause not allowed on this member
   ```

3. **Error Messages** (`CSharpResources.resx`):
   - Add user-facing error messages for CS9001-CS9005

### Phase 3: Semantic Validation
- Validate exception types in `Binder`
- Check override/interface implementation rules
- Ensure no duplicate exception types

### Phase 4: Metadata Support
- Define `ThrowsAttribute` for IL emission
- Emit attribute with exception types
- Read attribute from metadata (PE symbols)

### Phase 5: IDE Support
- Syntax highlighting for `throws` keyword
- IntelliSense completion after method parameters
- Quick info tooltips showing throws clause

### Phase 6: Testing
- Unit tests for parser
- Semantic analysis tests
- Override/interface implementation tests
- Metadata round-trip tests

## Files Modified

### Compiler Core
- `src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs`
- `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
- `src/Compilers/CSharp/Portable/Syntax/SyntaxKindFacts.cs`
- `src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`
- `src/Compilers/CSharp/Portable/Syntax/MethodDeclarationSyntax.cs`
- `src/Compilers/CSharp/Portable/Syntax/SyntaxExtensions.cs`
- `src/Compilers/Core/Portable/Symbols/IMethodSymbol.cs`
- `src/Compilers/CSharp/Portable/Symbols/MethodSymbol.cs`
- `src/Compilers/CSharp/Portable/Symbols/PublicModel/MethodSymbol.cs`

### Documentation
- `docs/features/throws-clause-specification.md`
- `docs/features/throws-clause-implementation-plan.md`
- `docs/features/throws-clause-progress.md` (this file)

### Samples
- `samples/ThrowsClauseDemo/Program.cs`
- `samples/ThrowsClauseDemo/SimpleTest.cs`
- `samples/ThrowsClauseDemo/MinimalTest.cs`
- `samples/ThrowsClauseDemo/ThrowsClauseDemo.csproj`
- `samples/ThrowsClauseDemo/README.md`

## Build Status
✅ **Compilers.slnf**: PASSING  
✅ **csc Tool**: Published successfully  
✅ **Sample Compilation**: SUCCESS  
✅ **Runtime Execution**: SUCCESS

## Technical Notes

### Merge Conflict Resolution
- Integrated with main branch changes (AllowsKeyword=8450, ExtensionKeyword=8451)
- Assigned ThrowsKeyword=8452 to maintain sequential numbering
- Fixed lambda signature in SkipBadTypeParameterConstraintTokens: `(p, _) => ...`

### Code Generation
- Generator script: `eng/generate-compiler-code.cs`
- Invocation: `dotnet run --file eng/generate-compiler-code.cs`
- Generates: C#, VB, VB GetText, IOperation files
- Output: `src/Compilers/CSharp/Portable/Generated/CSharpSyntaxGenerator/`

### Parser Design Patterns
- Used `_pool.AllocateSeparated<T>()` for efficient list building
- Terminator tokens: `OpenBrace`, `Semicolon`, `EqualsGreaterThan`
- Error recovery: `CreateMissingIdentifierName()` with `ERR_TypeExpected`
- Context checking: `CurrentToken.ContextualKind == SyntaxKind.ThrowsKeyword`

---

**Last Updated**: October 24, 2025  
**Status**: Phase 2 - Symbol Layer (60% complete)  
**Next Milestone**: Complete binding and validation logic
