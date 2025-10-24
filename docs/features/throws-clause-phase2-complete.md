# Throws Clause Implementation - Phase 2 Complete

## What We Just Completed

### Symbol Binding Implementation ✅

We successfully implemented the binding of throws clause exception types into the Roslyn symbol model. Here's what was done:

#### 1. Added Lazy Field for Throws Types
**File**: `SourceOrdinaryMethodOrUserDefinedOperatorSymbol.cs`
- Added `_lazyThrowsTypes` field to store bound exception types
- Follows the same lazy initialization pattern as parameters and return type

#### 2. Updated MethodChecks
**Files**: `SourceOrdinaryMethodOrUserDefinedOperatorSymbol.cs`
- Created overload: `MethodChecks(returnType, parameters, throwsTypes, diagnostics)`
- Original overload now calls new version with empty array
- Stores throws types in `_lazyThrowsTypes` field

#### 3. Overrode ThrowsTypes Property
**File**: `SourceOrdinaryMethodOrUserDefinedOperatorSymbol.cs`
```csharp
public sealed override ImmutableArray<TypeSymbol> ThrowsTypes
{
    get
    {
        LazyMethodChecks();
        return _lazyThrowsTypes;
    }
}
```
- Returns the lazily-initialized throws types
- Ensures method checks run first (binds types on-demand)

#### 4. Implemented BindThrowsTypes Method
**File**: `SourceOrdinaryMethodSymbol.cs`
```csharp
private ImmutableArray<TypeSymbol> BindThrowsTypes(BindingDiagnosticBag diagnostics)
{
    var syntax = GetSyntax();
    if (syntax.ThrowsClause == null)
    {
        return ImmutableArray<TypeSymbol>.Empty;
    }

    var withTypeParamsBinder = this.DeclaringCompilation
        .GetBinderFactory(syntax.SyntaxTree)
        .GetBinder(syntax.ReturnType, syntax, this);
    var signatureBinder = withTypeParamsBinder
        .WithAdditionalFlagsAndContainingMemberOrLambda(
            BinderFlags.SuppressConstraintChecks, this);

    var exceptionTypesSyntax = syntax.ThrowsClause.ExceptionTypes;
    var throwsTypesBuilder = ArrayBuilder<TypeSymbol>.GetInstance(exceptionTypesSyntax.Count);

    foreach (var exceptionTypeSyntax in exceptionTypesSyntax)
    {
        var exceptionType = signatureBinder.BindType(exceptionTypeSyntax, diagnostics).Type;
        throwsTypesBuilder.Add(exceptionType);
    }

    return throwsTypesBuilder.ToImmutableAndFree();
}
```

#### 5. Integrated into Method Checks
**File**: `SourceOrdinaryMethodSymbol.cs`
```csharp
protected override void MethodChecks(BindingDiagnosticBag diagnostics)
{
    var (returnType, parameters, declaredConstraints) = MakeParametersAndBindReturnType(diagnostics);
    var throwsTypes = BindThrowsTypes(diagnostics);  // NEW
    
    MethodSymbol? overriddenOrExplicitlyImplementedMethod = 
        MethodChecks(returnType, parameters, throwsTypes, diagnostics);  // UPDATED
    // ... rest of method
}
```

## Test Results

### Build Status
✅ **Compilers.slnf**: PASSING  
✅ **csc Publish**: SUCCESS  
✅ **Sample Compilation**: SUCCESS  

### Compilation Test
Created `ThrowsTypeTest.cs` with:
- Method with no throws clause
- Method with single exception type
- Method with multiple exception types
- Expression-bodied method with throws

**Result**: All compile successfully without errors!

### Runtime Test
```
$ dotnet artifacts/ThrowsTypeTest.exe
Testing throws clause binding...
Caught expected IOException: Test
All tests passed!
```

## Technical Details

### Binding Process
1. **Syntax Retrieval**: Get `MethodDeclarationSyntax` containing `ThrowsClause`
2. **Binder Creation**: Use declaration's compilation to get appropriate binder with type parameters in scope
3. **Constraint Suppression**: Use `BinderFlags.SuppressConstraintChecks` (constraints checked later in `AfterAddingTypeMembersChecks`)
4. **Type Binding**: For each exception type syntax, bind to TypeSymbol
5. **Storage**: Store in immutable array, returned via lazy property

### Design Decisions
- **Lazy Initialization**: Throws types bound on first access to ThrowsTypes property
- **Binder Scope**: Uses same binder as return type/parameters (has type parameters in scope)
- **Constraint Checking**: Deferred like parameters/return type (checked after adding to type member list)
- **Empty Array**: Methods without throws clause return `ImmutableArray<TypeSymbol>.Empty`

### Integration Points
- Works seamlessly with existing lazy initialization infrastructure
- Uses same diagnostic collection pattern as parameters/return type
- Respects method symbol lifecycle (binding happens during method checks)

## Files Modified in Phase 2

1. `src/Compilers/Core/Portable/Symbols/IMethodSymbol.cs` - Added ThrowsTypes property
2. `src/Compilers/CSharp/Portable/Symbols/MethodSymbol.cs` - Added virtual ThrowsTypes property
3. `src/Compilers/CSharp/Portable/Symbols/PublicModel/MethodSymbol.cs` - Added IMethodSymbol.ThrowsTypes implementation
4. `src/Compilers/CSharp/Portable/Symbols/Source/SourceOrdinaryMethodOrUserDefinedOperatorSymbol.cs`:
   - Added `_lazyThrowsTypes` field
   - Added overloaded `MethodChecks` with throwsTypes parameter
   - Overrode `ThrowsTypes` property
5. `src/Compilers/CSharp/Portable/Symbols/Source/SourceOrdinaryMethodSymbol.cs`:
   - Added `BindThrowsTypes()` method
   - Updated `MethodChecks()` to bind and pass throws types

## What's Next (Phase 3: Semantic Validation)

### 1. Add Error Codes
**File**: `ErrorCode.cs`
```csharp
CS9001 // Throws clause type must derive from System.Exception
CS9002 // Duplicate exception type in throws clause  
CS9003 // Override method throws exceptions not declared in base
CS9004 // Interface implementation throws exceptions not declared in interface
CS9005 // Throws clause not allowed on this member
```

### 2. Add Error Messages
**File**: `CSharpResources.resx`
- User-facing error messages for CS9001-CS9005

### 3. Implement Validation Logic
- Validate exception types derive from `System.Exception`
- Check for duplicate exception types
- Validate override/interface implementation rules

### 4. Add Validation to BindThrowsTypes
Enhance the binding method to perform validation:
- Check each type derives from Exception
- Detect duplicates
- Report diagnostics

---

**Status**: Phase 2 Symbol Binding - COMPLETE ✅  
**Next Milestone**: Phase 3 Semantic Validation  
**Last Updated**: October 24, 2025
