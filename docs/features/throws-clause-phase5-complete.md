# Throws Clause - Phase 5 Complete: Interface Implementation Validation

## Overview
Phase 5 has been successfully completed, adding interface implementation validation to the throws clause feature. The compiler now validates that interface implementations (both implicit and explicit) don't throw exceptions not declared by the interface method, ensuring type safety and contract adherence.

## Completed Work

### 1. Interface Implementation Validation Logic (SourceMemberContainerSymbol_ImplementationChecks.cs)

#### New Method: `CheckThrowsClauseInterfaceImplementation`
Added a new private static method to validate throws clause compatibility between interface and implementation methods:

```csharp
/// <summary>
/// Checks that an implementing method's throws clause is compatible with the interface method.
/// An implementation can only throw exception types that are declared by the interface method or are subtypes thereof.
/// </summary>
private static void CheckThrowsClauseInterfaceImplementation(
    MethodSymbol interfaceMethod,
    MethodSymbol implementingMethod,
    BindingDiagnosticBag diagnostics,
    Location location)
{
    var implementationThrowsTypes = implementingMethod.ThrowsTypes;
    if (implementationThrowsTypes.IsEmpty)
    {
        // Implementation throws no exceptions - always valid
        return;
    }

    var interfaceThrowsTypes = interfaceMethod.ThrowsTypes;

    // Check each exception type in the implementation's throws clause
    foreach (var implementationExceptionType in implementationThrowsTypes)
    {
        bool isCompatible = false;

        // An implementation exception type is compatible if:
        // 1. It's declared in the interface throws clause (identity conversion), or
        // 2. It's a subtype of an exception declared in the interface throws clause (reference conversion)
        foreach (var interfaceExceptionType in interfaceThrowsTypes)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, interfaceMethod.ContainingAssembly);
            var conversion = implementingMethod.DeclaringCompilation.Conversions.ClassifyBuiltInConversion(
                implementationExceptionType,
                interfaceExceptionType,
                isChecked: false,
                ref useSiteInfo);
            diagnostics.Add(location, useSiteInfo);

            // Allow identity conversion (same type) or implicit reference conversion (subtype)
            if (conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference))
            {
                // Implementation exception type is same as or derives from interface exception type
                isCompatible = true;
                break;
            }
        }

        if (!isCompatible)
        {
            // ERR_InterfaceImplementationThrowsExceptionNotDeclaredByInterface (CS9343)
            diagnostics.Add(ErrorCode.ERR_InterfaceImplementationThrowsExceptionNotDeclaredByInterface,
                location,
                implementingMethod,
                implementationExceptionType);
        }
    }
}
```

#### Integration into Interface Implementation Checking
Integrated the validation into `ComputeInterfaceImplementations` where interface members are validated:

```csharp
if (wasImplementingMemberFound && interfaceMemberKind == SymbolKind.Method)
{
    // ... existing use site error checking ...

    if (synthesizedImplementation.ForwardingMethod is not null || TypeSymbol.Equals(implementingMember.ContainingType, this, TypeCompareKind.ConsiderEverything2))
    {
        // ... existing diagnostics ...

        // Check throws clause compatibility for interface implementations
        CheckThrowsClauseInterfaceImplementation(
            (MethodSymbol)interfaceMember,
            (MethodSymbol)implementingMember,
            diagnostics,
            location);
    }
}
```

## Validation Rules

### Interface Implementation Compatibility Rules
An implementing method's throws clause is compatible with the interface method if:

1. **Empty Implementation**: The implementation throws no exceptions (empty or absent throws clause)
   - ✅ Always valid regardless of interface throws clause

2. **Exact Match**: The implementation throws the same exception type as declared in interface
   - Uses **identity conversion** check
   - Example: Interface throws `IOException`, implementation throws `IOException` ✅

3. **More Specific**: The implementation throws a subtype of an exception in the interface throws clause
   - Uses **implicit reference conversion** check
   - Example: Interface throws `Exception`, implementation throws `IOException` ✅
   - Example: Interface throws `CustomException`, implementation throws `SpecificCustomException` ✅

4. **Subset**: The implementation throws fewer exception types than interface
   - Example: Interface throws `IOException, ArgumentException`, implementation throws only `IOException` ✅

### Invalid Implementation Scenarios (CS9343)
- Implementation throws exception type not in interface's throws clause
- Implementation throws exception type broader than any in interface's throws clause
- Implementation throws exception when interface has no throws clause

### Works for Both Implementation Types
- ✅ **Implicit implementations** (public methods)
- ✅ **Explicit implementations** (qualified with interface name)

## Testing

### Test File: InterfaceImplementationTest.cs
Created comprehensive test with multiple scenarios (188 lines):

#### Valid Implementation Cases (Compiles Successfully)
```csharp
interface IBasicOperations
{
    void Method1() throws IOException;
    void Method2() throws IOException, ArgumentException;
    void Method3(); // No throws
    void Method4() throws Exception;
}

class ValidImplementation : IBasicOperations
{
    // ✅ Same exception
    public void Method1() throws IOException { }
    
    // ✅ Subset of interface exceptions
    public void Method2() throws IOException { }
    
    // ✅ No exceptions (always valid)
    public void Method3() { }
    
    // ✅ More specific exception (FileNotFoundException < Exception)
    public void Method4() throws FileNotFoundException { }
}

class ExplicitImplementation : IBasicOperations
{
    // ✅ Explicit implementation with compatible throws clause
    void IBasicOperations.Method1() throws IOException { }
    
    // ... more explicit implementations ...
}

class NoThrowsImplementation : IBasicOperations
{
    // ✅ No throws clause is always compatible
    public void Method1() { }
    public void Method2() { }
    public void Method3() { }
    public void Method4() { }
}
```

#### Invalid Implementation Cases (CS9343 Errors)
```csharp
class InvalidImplementation : IBasicOperations
{
    // ❌ CS9343: ArgumentException not in interface
    public void Method1() throws ArgumentException { }
    
    // ❌ CS9343: InvalidOperationException not in interface
    public void Method2() throws InvalidOperationException { }
    
    // ❌ CS9343: IOException not in interface (interface has no throws clause)
    public void Method3() throws IOException { }
}

class InvalidCustomImplementation : ICustomExceptions
{
    // ❌ CS9343: Exception is broader than CustomException
    public void CustomMethod() throws Exception { }
}

class ExplicitImplementation : IBasicOperations
{
    // ❌ CS9343: InvalidOperationException not in interface
    void IBasicOperations.Method2() throws InvalidOperationException { }
}
```

### Compilation Results
```bash
InterfaceImplementationTest.cs(130,21): error CS9343: 'InvalidCustomImplementation.CustomMethod()': interface implementation throws exception type 'Exception' not declared in interface method
InterfaceImplementationTest.cs(86,31): error CS9343: 'ExplicitImplementation.IBasicOperations.Method2()': interface implementation throws exception type 'InvalidOperationException' not declared in interface method
InterfaceImplementationTest.cs(52,21): error CS9343: 'InvalidImplementation.Method1()': interface implementation throws exception type 'ArgumentException' not declared in interface method
InterfaceImplementationTest.cs(58,21): error CS9343: 'InvalidImplementation.Method2()': interface implementation throws exception type 'InvalidOperationException' not declared in interface method
InterfaceImplementationTest.cs(64,21): error CS9343: 'InvalidImplementation.Method3()': interface implementation throws exception type 'IOException' not declared in interface method
```

✅ **All expected errors reported, no false positives!**

## Design Decisions

### Why Check in `ComputeInterfaceImplementations`?
This is where Roslyn validates all aspects of interface implementations:
- Checks that all interface members are implemented
- Validates signature compatibility
- Reports use-site diagnostics
- Creates forwarding methods when needed

Adding throws clause validation here ensures:
- Consistent error reporting location
- Integration with existing interface validation logic
- Proper handling of both implicit and explicit implementations
- Coverage of all interface implementation scenarios

### Why Use Same Logic as Override Validation?
The validation logic for interface implementations is nearly identical to override validation:
- Both enforce Liskov Substitution Principle
- Both allow exact match (identity) or more specific types (implicit reference conversion)
- Both report incompatible exception types

This consistency makes the feature intuitive and maintainable.

### Integration Point Details
The validation is called when:
- `wasImplementingMemberFound` is true (we found an implementing method)
- `interfaceMemberKind == SymbolKind.Method` (we're checking a method, not property/event)
- Implementation is in current type OR a forwarding method is needed

This ensures we:
- Only validate actual implementations (not missing members)
- Only validate methods (properties/events validated via their accessors)
- Check both direct implementations and base class implementations that need forwarding

## Build Status

### Compilation
- **Compilers.slnf**: ✅ Builds successfully with zero errors and warnings
- **Compiler Published**: ✅ `artifacts/publish/csc/csc.dll` updated

### Test Results
- Valid implementations (implicit & explicit) compile without errors ✅
- Invalid implementations report CS9343 with correct member and exception type ✅
- No false positives ✅
- Works for both implicit and explicit interface implementations ✅

## What Works Now

1. ✅ **Syntax Parsing**: Methods can declare throws clauses
2. ✅ **Symbol Binding**: Exception types accessible via `IMethodSymbol.ThrowsTypes`
3. ✅ **Type Validation**: Types must derive from `System.Exception` (CS9340)
4. ✅ **Duplicate Detection**: Duplicate exception types reported (CS9341)
5. ✅ **Override Validation**: Overrides validated against base method (CS9342)
6. ✅ **Interface Implementation Validation**: Implementations validated against interface (CS9343)
7. ✅ **Explicit Implementation Support**: Works for explicit interface implementations
8. ✅ **Build Integration**: Modified compiler successfully builds programs
9. ✅ **Runtime Execution**: Compiled programs execute correctly

## Remaining Work

### Medium Priority
1. **CS9344 Implementation**: Restrict throws clause to allowed member kinds
   - Currently only methods are tested, but syntax allows it on other members
   - Decide whether to extend support (constructors, property accessors) or restrict

### Low Priority
2. **Unit Tests**: Create proper unit tests in Roslyn test projects
   - Follow patterns from existing override/interface tests
   - Test edge cases, generic methods, async methods, etc.

3. **Additional Features**: Consider warnings for:
   - Uncaught exceptions (like Java's checked exceptions)
   - Unused exception declarations
   - Throws clause on methods that don't actually throw

## Files Modified

1. `/src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberContainerSymbol_ImplementationChecks.cs`
   - Added `CheckThrowsClauseInterfaceImplementation()` method (65 lines)
   - Integrated into `ComputeInterfaceImplementations()` method

## Test Files Created

1. `/samples/ThrowsClauseDemo/InterfaceImplementationTest.cs` - Comprehensive interface validation test (188 lines)
   - Valid implicit implementation scenarios (4 test classes)
   - Invalid implicit implementation scenarios (2 test classes)
   - Valid explicit implementation scenarios (1 test class)
   - Invalid explicit implementation scenarios (1 test class)
   - Custom exception hierarchy testing (2 test classes)
   - Mixed implicit/explicit implementations (1 test class)
   - No throws clause implementations (1 test class)

## Comparison: Override vs Interface Implementation

| Aspect | Override Validation | Interface Implementation |
|--------|-------------------|-------------------------|
| Error Code | CS9342 | CS9343 |
| Integration Point | `CheckOverrideMember` | `ComputeInterfaceImplementations` |
| Method Name | `CheckThrowsClauseOverride` | `CheckThrowsClauseInterfaceImplementation` |
| Validation Logic | Identical | Identical |
| Base Method | Virtual/abstract method | Interface method |
| Implementation Type | Override keyword | Implicit or explicit |
| Location | `checkValidMethodOverride` local function | After use-site diagnostics check |

## Summary

Phase 5 successfully implements **interface implementation validation** for the throws clause feature. The compiler now:
- Validates interface implementations (implicit and explicit) against interface method throws clauses
- Enforces Liskov Substitution Principle for exception specifications in interfaces
- Reports CS9343 with clear, actionable messages
- Allows exact matches and more specific exception types
- Prevents broader or unrelated exception types in implementations
- Works consistently with override validation (CS9342)
- Maintains zero build warnings/errors
- Passes all interface implementation validation tests

The throws clause feature is now functionally complete for its core validation scenarios:
- ✅ Basic type validation (CS9340, CS9341)
- ✅ Override validation (CS9342)
- ✅ Interface implementation validation (CS9343)

The remaining work is primarily around polish (unit tests, edge cases) and optional features (CS9344, warnings for uncaught exceptions).
