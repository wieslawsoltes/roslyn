# Throws Clause - Phase 4 Complete: Override Validation

## Overview
Phase 4 has been successfully completed, adding override validation to the throws clause feature. The compiler now validates that overriding methods don't throw exceptions not declared by the base method, enforcing the Liskov Substitution Principle for exception specifications.

## Completed Work

### 1. Override Validation Logic (SourceMemberContainerSymbol_ImplementationChecks.cs)

#### New Method: `CheckThrowsClauseOverride`
Added a new private static method to validate throws clause compatibility between base and override methods:

```csharp
/// <summary>
/// Checks that an overriding method's throws clause is compatible with the base method.
/// An override can only throw exception types that are declared by the base method or are subtypes thereof.
/// </summary>
private static void CheckThrowsClauseOverride(
    MethodSymbol baseMethod,
    MethodSymbol overrideMethod,
    BindingDiagnosticBag diagnostics,
    Location location)
{
    var overrideThrowsTypes = overrideMethod.ThrowsTypes;
    if (overrideThrowsTypes.IsEmpty)
    {
        // Override throws no exceptions - always valid
        return;
    }

    var baseThrowsTypes = baseMethod.ThrowsTypes;

    // Check each exception type in the override's throws clause
    foreach (var overrideExceptionType in overrideThrowsTypes)
    {
        bool isCompatible = false;

        // An override exception type is compatible if:
        // 1. It's declared in the base throws clause (identity conversion), or
        // 2. It's a subtype of an exception declared in the base throws clause (reference conversion)
        foreach (var baseExceptionType in baseThrowsTypes)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, baseMethod.ContainingAssembly);
            var conversion = overrideMethod.DeclaringCompilation.Conversions.ClassifyBuiltInConversion(
                overrideExceptionType,
                baseExceptionType,
                isChecked: false,
                ref useSiteInfo);
            diagnostics.Add(location, useSiteInfo);

            // Allow identity conversion (same type) or implicit reference conversion (subtype)
            if (conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference))
            {
                // Override exception type is same as or derives from base exception type
                isCompatible = true;
                break;
            }
        }

        if (!isCompatible)
        {
            // ERR_OverrideThrowsExceptionNotDeclaredByBase (CS9342)
            diagnostics.Add(ErrorCode.ERR_OverrideThrowsExceptionNotDeclaredByBase,
                location,
                overrideMethod,
                overrideExceptionType);
        }
    }
}
```

#### Integration into Override Checking
Integrated the validation into the existing `checkValidMethodOverride` local function within `CheckOverrideMember`:

```csharp
static void checkValidMethodOverride(
    Location overridingMemberLocation,
    MethodSymbol overriddenMethod,
    MethodSymbol overridingMethod,
    BindingDiagnosticBag diagnostics)
{
    // ... existing validation code ...

    // Check throws clause compatibility
    CheckThrowsClauseOverride(overriddenMethod, overridingMethod, diagnostics, overridingMemberLocation);
}
```

### 2. Fixed Identity Conversion Bug

#### Problem
The initial implementation didn't recognize `System.Exception` itself as valid, because it only checked for implicit reference conversions, not identity conversions.

#### Solution
Updated both validation points to allow identity conversions:

**In `BindThrowsTypes()` (SourceOrdinaryMethodSymbol.cs):**
```csharp
// OLD: if (!conversion.IsImplicit || !conversion.IsReference)
// NEW:
if (!(conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference)))
{
    diagnostics.Add(ErrorCode.ERR_ThrowsClauseTypeMustDeriveFromException,
        exceptionTypeSyntax.Location,
        boundType);
}
```

**In `CheckThrowsClauseOverride():`**
```csharp
// OLD: if (conversion.IsImplicit && conversion.IsReference)
// NEW:
if (conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference))
{
    isCompatible = true;
    break;
}
```

## Validation Rules

### Override Compatibility Rules
An overriding method's throws clause is compatible with the base method if:

1. **Empty Override**: The override throws no exceptions (empty or absent throws clause)
   - ✅ Always valid regardless of base throws clause

2. **Exact Match**: The override throws the same exception type as declared in base
   - Uses **identity conversion** check
   - Example: Base throws `IOException`, override throws `IOException` ✅

3. **More Specific**: The override throws a subtype of an exception in the base throws clause
   - Uses **implicit reference conversion** check  
   - Example: Base throws `Exception`, override throws `IOException` ✅
   - Example: Base throws `CustomException`, override throws `SpecificCustomException` ✅

4. **Subset**: The override throws fewer exception types than base
   - Example: Base throws `IOException, ArgumentException`, override throws only `IOException` ✅

### Invalid Override Scenarios (CS9342)
- Override throws exception type not in base's throws clause
- Override throws exception type broader than any in base's throws clause
- Override throws exception when base has no throws clause

## Testing

### Test File: OverrideValidationTest.cs
Created comprehensive test with multiple scenarios:

#### Valid Override Cases (Compiles Successfully)
```csharp
class BaseClass
{
    public virtual void Method1() throws IOException { }
    public virtual void Method2() throws IOException, ArgumentException { }
    public virtual void Method3() { } // No throws
    public virtual void Method4() throws Exception { }
}

class DerivedValid : BaseClass
{
    // ✅ Same exception
    public override void Method1() throws IOException { }
    
    // ✅ Subset of base exceptions
    public override void Method2() throws IOException { }
    
    // ✅ No exceptions (always valid)
    public override void Method3() { }
    
    // ✅ More specific exception (IOException < Exception)
    public override void Method4() throws IOException { }
}
```

#### Invalid Override Cases (CS9342 Errors)
```csharp
class DerivedInvalid : BaseClass
{
    // ❌ CS9342: ArgumentException not in base
    public override void Method1() throws ArgumentException { }
    
    // ❌ CS9342: InvalidOperationException not in base
    public override void Method2() throws InvalidOperationException { }
    
    // ❌ CS9342: IOException not in base (base has no throws clause)
    public override void Method3() throws IOException { }
}

class DerivedWithBroaderException : BaseWithCustomException
{
    // ❌ CS9342: Exception is broader than CustomException
    public override void CustomMethod() throws Exception { }
}
```

### Compilation Results
```bash
OverrideValidationTest.cs(119,30): error CS9342: 'DerivedWithBroaderException.CustomMethod()': override method throws exception type 'Exception' not declared in base method
OverrideValidationTest.cs(70,30): error CS9342: 'DerivedInvalid.Method2()': override method throws exception type 'InvalidOperationException' not declared in base method
OverrideValidationTest.cs(76,30): error CS9342: 'DerivedInvalid.Method3()': override method throws exception type 'IOException' not declared in base method
OverrideValidationTest.cs(64,30): error CS9342: 'DerivedInvalid.Method1()': override method throws exception type 'ArgumentException' not declared in base method
```

✅ **All expected errors reported, no false positives!**

## Design Decisions

### Why Allow Identity + Implicit Reference Conversions?
- **Identity**: Allows exact type match (base: `IOException`, override: `IOException`)
- **Implicit Reference**: Allows more specific types (base: `Exception`, override: `IOException`)
- **Rejected**: Explicit conversions, boxing conversions, user-defined conversions

### Why Use Conversion System?
- Consistent with Roslyn's type compatibility checking patterns
- Properly handles generic types and inheritance hierarchies
- Respects compiler semantics
- Handles use-site diagnostics correctly

### Liskov Substitution Principle
The validation enforces LSP: an overriding method can be more restrictive (fewer/more specific exceptions) but not less restrictive (additional/broader exceptions) than the base method. This ensures that code expecting the base type's behavior won't be surprised by unexpected exceptions.

### Integration Point
Override checking happens in `CheckOverrideMember()` → `checkValidMethodOverride()`, which is called during type member checking phase. This is the same place where:
- Return type compatibility is checked
- Parameter type compatibility is checked (`CheckValidNullableMethodOverride`)
- Scoped ref safety is checked (`CheckValidScopedOverride`)
- Ref/readonly mismatches are checked (`CheckRefReadonlyInMismatch`)

## Build Status

### Compilation
- **Compilers.slnf**: ✅ Builds successfully with zero errors and warnings
- **Compiler Published**: ✅ `artifacts/publish/csc/csc.dll` updated

### Test Results
- Valid overrides compile without errors ✅
- Invalid overrides report CS9342 with correct member and exception type ✅
- No false positives (System.Exception identity bug fixed) ✅

## What Works Now

1. ✅ **Syntax Parsing**: Methods can declare throws clauses
2. ✅ **Symbol Binding**: Exception types accessible via `IMethodSymbol.ThrowsTypes`
3. ✅ **Type Validation**: Types must derive from `System.Exception` (CS9340)
4. ✅ **Duplicate Detection**: Duplicate exception types reported (CS9341)
5. ✅ **Override Validation**: Overrides validated against base method throws clause (CS9342)
6. ✅ **Build Integration**: Modified compiler successfully builds programs
7. ✅ **Runtime Execution**: Compiled programs execute correctly

## Remaining Work

### High Priority
1. **Interface Implementation Validation (CS9343)**: Check that interface implementations don't throw exceptions not declared by interface method
   - Similar to override validation but for implicit/explicit interface implementations
   - Integration point: Interface implementation checking code

### Medium Priority
2. **CS9344 Implementation**: Restrict throws clause to allowed member kinds
   - Currently only methods are tested, but syntax allows it on other members
   - Should restrict or extend to constructors, property accessors, etc.

### Low Priority
3. **Unit Tests**: Create proper unit tests in Roslyn test projects following established patterns
4. **Additional Features**: Consider warnings for uncaught exceptions, unused exception declarations

## Files Modified

1. `/src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberContainerSymbol_ImplementationChecks.cs`
   - Added `CheckThrowsClauseOverride()` method
   - Integrated into `checkValidMethodOverride()` local function

2. `/src/Compilers/CSharp/Portable/Symbols/Source/SourceOrdinaryMethodSymbol.cs`
   - Fixed identity conversion bug in `BindThrowsTypes()`

## Test Files Created

1. `/samples/ThrowsClauseDemo/OverrideValidationTest.cs` - Comprehensive override validation test (136 lines)
   - Valid override scenarios (4 test classes)
   - Invalid override scenarios (4 test classes with CS9342 errors)
   - Exception hierarchy testing (custom exception types)

## Next Steps

The immediate next step is to implement interface implementation validation (CS9343):
1. Find interface implementation checking code in Roslyn
2. Add similar validation logic to `CheckThrowsClauseOverride`
3. Report CS9343 when implementation throws exceptions not in interface
4. Test with explicit and implicit interface implementations

This will require finding the interface implementation checking code, likely in:
- `SourceMemberContainerSymbol_ImplementationChecks.cs` (interface member checking)
- Similar pattern to override checking but with different error code

## Summary

Phase 4 successfully implements **override validation** for the throws clause feature. The compiler now:
- Validates override methods against base method throws clauses
- Enforces Liskov Substitution Principle for exception specifications
- Reports CS9342 with clear, actionable messages
- Allows exact matches and more specific exception types
- Prevents broader or unrelated exception types in overrides
- Maintains zero build warnings/errors
- Passes all override validation tests

The foundation is now in place for interface implementation validation (Phase 5), which will use similar logic but for interface implementations instead of overrides.
