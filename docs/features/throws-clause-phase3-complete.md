# Throws Clause - Phase 3 Complete

## Overview
Phase 3 has been successfully completed, adding semantic validation to the throws clause feature. The compiler now validates that exception types in throws clauses derive from System.Exception and detects duplicate exception types.

## Completed Work

### 1. Error Code Definitions (ErrorCode.cs)
Added 5 new error codes after `ERR_AmbigExtension = 9339`:

```csharp
ERR_ThrowsClauseTypeMustDeriveFromException = 9340,
ERR_DuplicateExceptionTypeInThrowsClause = 9341,
ERR_OverrideThrowsExceptionNotDeclaredByBase = 9342,
ERR_InterfaceImplementationThrowsExceptionNotDeclaredByInterface = 9343,
ERR_ThrowsClauseNotAllowedOnMemberKind = 9344,
```

### 2. Error Messages (CSharpResources.resx)
Added user-facing error messages for all 5 error codes:

- **CS9340**: "Throws clause type '{0}' does not derive from 'System.Exception'"
- **CS9341**: "Duplicate exception type '{0}' in throws clause"
- **CS9342**: "'{0}': override method throws exception type '{1}' not declared in base method"
- **CS9343**: "'{0}': interface implementation throws exception type '{1}' not declared in interface method"
- **CS9344**: "Throws clause is not allowed on {0}"

### 3. Localization Files (.xlf)
Successfully updated all localization files using:
```bash
dotnet msbuild src/Compilers/CSharp/Portable/Microsoft.CodeAnalysis.CSharp.csproj /t:UpdateXlf
```

### 4. Validation Logic (SourceOrdinaryMethodSymbol.cs)
Enhanced the `BindThrowsTypes()` method to perform validation:

#### Validation Rules Implemented:
1. **Type Derivation Check (CS9340)**:
   - Validates each exception type derives from `System.Exception`
   - Uses `ClassifyBuiltInConversion` to check for implicit reference conversion
   - Skips validation for error types (already reported)

2. **Duplicate Detection (CS9341)**:
   - Uses `PooledHashSet<TypeSymbol>` to track seen types
   - Reports duplicate exception types with precise location

#### Implementation Details:
```csharp
private ImmutableArray<TypeSymbol> BindThrowsTypes(BindingDiagnosticBag diagnostics)
{
    // Get System.Exception type from well-known types
    var exceptionType = this.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Exception);
    
    // Track seen types for duplicate detection
    var seenTypes = PooledHashSet<TypeSymbol>.GetInstance();
    
    // Validate each exception type
    foreach (var exceptionTypeSyntax in exceptionTypesSyntax)
    {
        var boundType = signatureBinder.BindType(exceptionTypeSyntax, diagnostics).Type;
        
        // Check derivation from Exception
        var conversion = this.DeclaringCompilation.Conversions.ClassifyBuiltInConversion(
            boundType, exceptionType, isChecked: false, ref useSiteInfo);
        
        if (!conversion.IsImplicit || !conversion.IsReference)
        {
            diagnostics.Add(ErrorCode.ERR_ThrowsClauseTypeMustDeriveFromException,
                exceptionTypeSyntax.Location, boundType);
        }
        
        // Check for duplicates
        if (!seenTypes.Add(boundType))
        {
            diagnostics.Add(ErrorCode.ERR_DuplicateExceptionTypeInThrowsClause,
                exceptionTypeSyntax.Location, boundType);
        }
    }
    
    seenTypes.Free();
    return throwsTypesBuilder.ToImmutableAndFree();
}
```

### 5. ErrorFacts.cs Update
Added the new error codes to the `IsBuildOnlyDiagnostic` switch expression, marking them as `false` (not build-only). This ensures they can be reported from `SemanticModel.GetDiagnostics()` API.

## Testing

### Validation Error Tests (ValidationTest.cs)
Created comprehensive test file with invalid throws clauses:

```csharp
// CS9340: string doesn't derive from Exception
void InvalidType1() throws string { }

// CS9340: int doesn't derive from Exception
void InvalidType2() throws int { }

// CS9341: Duplicate IOException
void DuplicateTypes() throws IOException, ArgumentException, IOException { }

// CS9341: Multiple duplicates
void MultipleDuplicates() throws IOException, IOException, ArgumentException, ArgumentException { }
```

#### Compilation Output:
```
ValidationTest.cs(15,36): error CS9340: Throws clause type 'string' does not derive from 'System.Exception'
ValidationTest.cs(20,36): error CS9340: Throws clause type 'int' does not derive from 'System.Exception'
ValidationTest.cs(25,70): error CS9341: Duplicate exception type 'IOException' in throws clause
ValidationTest.cs(30,55): error CS9341: Duplicate exception type 'IOException' in throws clause
ValidationTest.cs(30,87): error CS9341: Duplicate exception type 'ArgumentException' in throws clause
```

✅ **All expected errors reported correctly!**

### Validation Success Tests (ValidationSuccessTest.cs)
Created test file with valid throws clauses:

```csharp
void TestSingleException() throws IOException { }
void TestMultipleExceptions() throws IOException, ArgumentException, InvalidOperationException { }
void TestNoThrowsClause() { }
void TestExceptionSubclass() throws FileNotFoundException { }
```

#### Compilation Result:
✅ **Compiled successfully with no errors**

#### Runtime Execution:
```
✅ All validation tests passed!
The compiler correctly accepts valid throws clauses.
Caught IOException: Test
Caught ArgumentException: Test
No exceptions
Caught FileNotFoundException: File not found
✅ Runtime execution successful!
```

## Build Status

### Compilation
- **Compilers.slnf**: ✅ Builds successfully with zero errors and warnings
- **Compiler Published**: ✅ `artifacts/publish/csc/csc.dll` updated

### Code Quality
- All error codes properly defined
- All error messages localized
- ErrorFacts.cs properly updated (no warnings)
- Validation logic follows Roslyn patterns (pooled collections, use-site info, etc.)

## What Works Now

1. ✅ **Syntax Parsing**: Methods can declare throws clauses with comma-separated exception types
2. ✅ **Symbol Binding**: Exception types bound to `TypeSymbol` instances accessible via `IMethodSymbol.ThrowsTypes`
3. ✅ **Type Validation**: Compiler enforces that all types in throws clauses derive from `System.Exception`
4. ✅ **Duplicate Detection**: Compiler detects and reports duplicate exception types in throws clauses
5. ✅ **Build Integration**: Modified compiler successfully builds programs with throws clauses
6. ✅ **Runtime Execution**: Compiled programs execute correctly
7. ✅ **Error Reporting**: Clear, actionable error messages with precise locations

## Remaining Work

### High Priority (Not Yet Implemented)
1. **Override Validation (CS9342)**: Check that override methods don't throw exceptions not declared by base method
2. **Interface Implementation Validation (CS9343)**: Check that interface implementations don't throw exceptions not declared by interface method

### Medium Priority
3. **CS9344 Implementation**: Restrict throws clause to allowed member kinds (currently only methods supported)

### Low Priority (Future Enhancements)
4. Consider adding throws clause support to:
   - Constructors
   - Property accessors
   - Delegates
   - Lambda expressions
5. Consider warnings for:
   - Uncaught exceptions (like Java's checked exceptions)
   - Unused exception declarations

## Technical Design Decisions

### Why Use `ClassifyBuiltInConversion`?
We use the conversion system rather than directly checking `Type.DerivesFrom()` because:
- It's the standard Roslyn pattern for type compatibility checks
- It properly handles use-site diagnostics
- It respects compiler semantics for conversions
- It's consistent with how other language features validate types

### Why Not Build-Only?
Throws clause errors (CS9340, CS9341) are marked as non-build-only diagnostics because:
- They are semantic errors detectable from `SemanticModel`
- IDE features can report them during editing
- They don't require lowering or emit phases

### Pooled Collections
We use `PooledHashSet` for duplicate detection to minimize allocations, following Roslyn's performance-conscious patterns.

## Files Modified

1. `/src/Compilers/CSharp/Portable/Errors/ErrorCode.cs` - Added error codes 9340-9344
2. `/src/Compilers/CSharp/Portable/CSharpResources.resx` - Added error messages
3. `/src/Compilers/CSharp/Portable/xlf/*.xlf` - Localization files updated via UpdateXlf
4. `/src/Compilers/CSharp/Portable/Symbols/Source/SourceOrdinaryMethodSymbol.cs` - Enhanced BindThrowsTypes() with validation
5. `/src/Compilers/CSharp/Portable/Errors/ErrorFacts.cs` - Added error codes to IsBuildOnlyDiagnostic switch

## Test Files Created

1. `/samples/ThrowsClauseDemo/ValidationTest.cs` - Tests error cases (CS9340, CS9341)
2. `/samples/ThrowsClauseDemo/ValidationSuccessTest.cs` - Tests valid throws clauses

## Next Steps

The immediate next step is to implement override and interface implementation validation:
1. Add validation in override checking to ensure CS9342 is reported
2. Add validation in interface implementation checking to ensure CS9343 is reported

These checks will need to be integrated into the method override and interface implementation validation logic, likely in:
- `SourceMemberContainerTypeSymbol.cs` (override checking)
- Interface implementation checking code

## Summary

Phase 3 successfully adds **semantic validation** to the throws clause feature. The compiler now:
- Validates exception types derive from System.Exception
- Detects duplicate exception types
- Reports clear, actionable errors with precise source locations
- Maintains zero build warnings/errors
- Passes all end-to-end tests (compilation and execution)

The foundation for override/interface validation is in place via error codes CS9342/CS9343, which can be implemented in Phase 4.
