# Throws Clause - Extended Features Implementation Plan

**Status:** 📋 Planning Phase  
**Current Phase:** Phase 6 Complete ✅  
**Next Phase:** Phase 7 (IDE IntelliSense)

---

## Overview

This document outlines the implementation plan for extended throws clause features, building on the complete core implementation (Phases 1-6). These features add IDE support, warnings, code fixes, and advanced analysis capabilities.

---

## Phase 7: IDE IntelliSense Features

**Status:** ⏳ Not Started  
**Priority:** High  
**Estimated Time:** 7-11 hours  
**Dependencies:** Phases 1-6 complete

### Objectives
- Provide IntelliSense completion for exception types after `throws` keyword
- Show throws information in signature help
- Display throws clause in hover tooltips

### Task Breakdown

#### Task 7.1: Completion Provider (4-6 hours)

**Files to Create:**
```
src/Features/Core/Portable/Completion/Providers/ThrowsClauseCompletionProvider.cs
```

**Implementation Steps:**
1. Create `ThrowsClauseCompletionProvider` inheriting from `CompletionProvider`
2. Override `ProvideCompletionsAsync`:
   - Detect cursor position after `throws` keyword
   - Get semantic model and collect all exception types
   - Filter to types deriving from `System.Exception`
   - Sort by common usage then alphabetically
3. Add completion item metadata:
   - Display text: exception type name
   - Description: XML documentation summary
   - Icon: exception glyph
4. Register provider in MEF

**Code Snippet:**
```csharp
[ExportCompletionProvider(nameof(ThrowsClauseCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(TypeImportCompletionProvider))]
[Shared]
internal class ThrowsClauseCompletionProvider : CompletionProvider
{
    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var position = context.Position;
        var semanticModel = await context.Document.GetSemanticModelAsync();
        
        // Check if we're after 'throws' keyword
        if (!IsInThrowsContext(position, semanticModel))
            return;
            
        // Get all exception types
        var exceptionTypes = GetExceptionTypes(semanticModel.Compilation);
        
        foreach (var type in exceptionTypes)
        {
            context.AddItem(CreateCompletionItem(type));
        }
    }
}
```

**Testing:**
- [ ] Completion triggers after `throws` keyword
- [ ] Only exception types appear in list
- [ ] Common exceptions (IOException, ArgumentException) appear first
- [ ] Works with partial type names
- [ ] XML docs appear in tooltip

#### Task 7.2: Signature Help (2-3 hours)

**Files to Modify:**
```
src/Features/Core/Portable/SignatureHelp/AbstractSignatureHelpProvider.cs
src/EditorFeatures/Core/IntelliSense/SignatureHelp/SignatureHelpItem.cs
```

**Implementation Steps:**
1. Extend `SignatureHelpItem` to include throws information
2. Update `AbstractSignatureHelpProvider` to extract throws from `IMethodSymbol`
3. Format throws clause in signature: `ReturnType MethodName(...) throws ExceptionType`
4. Add documentation section for each exception type

**Code Snippet:**
```csharp
protected SignatureHelpItem CreateSignatureHelpItem(IMethodSymbol method, ...)
{
    var parameters = GetParameters(method);
    var documentation = GetDocumentation(method);
    
    // Add throws clause to signature
    var throwsClause = method.ThrowsTypes.Any()
        ? $" throws {string.Join(", ", method.ThrowsTypes.Select(t => t.Name))}"
        : "";
        
    var displayText = $"{method.ReturnType} {method.Name}({parameters}){throwsClause}";
    
    return new SignatureHelpItem(...);
}
```

**Testing:**
- [ ] Signature help shows throws clause
- [ ] Multiple exceptions display correctly
- [ ] Works with inherited methods
- [ ] Works with interface implementations

#### Task 7.3: Quick Info (1-2 hours)

**Files to Modify:**
```
src/Features/Core/Portable/QuickInfo/CommonQuickInfoProvider.cs
```

**Implementation Steps:**
1. Extract throws types from `IMethodSymbol`
2. Add throws section to quick info display
3. Make exception types clickable (F12 navigation)
4. Show inherited/interface throws information

**Testing:**
- [ ] Hover shows throws clause
- [ ] Click exception type navigates to definition
- [ ] Shows inherited throws clauses
- [ ] Works in all IDEs

### Dependencies
- Core compiler implementation (Phases 1-6)
- Roslyn IDE services infrastructure
- MEF composition system

### Success Criteria
- ✅ IntelliSense suggests exception types after `throws`
- ✅ Signature help displays throws clause
- ✅ Hover tooltips show throws information
- ✅ Works in VS Code, Visual Studio, and Rider

---

## Phase 8: Quick Fixes and Code Actions

**Status:** ⏳ Not Started  
**Priority:** High  
**Estimated Time:** 12-16 hours  
**Dependencies:** Phase 7

### Objectives
- Automated fixes for CS9341 (missing throws declaration)
- Alternative fix: wrap with try-catch
- Remove unused throws declarations
- Propagate throws to calling methods

### Task Breakdown

#### Task 8.1: Add Missing Throws Declaration (4-5 hours)

**Files to Create:**
```
src/Features/CSharp/Portable/CodeFixes/AddThrowsClause/AddThrowsClauseCodeFixProvider.cs
src/Features/CSharp/Portable/CodeFixes/AddThrowsClause/AddThrowsClauseCodeAction.cs
```

**Implementation Steps:**
1. Create code fix provider for CS9341
2. Detect which exception needs to be added
3. Generate syntax for throws clause:
   - If no throws clause exists: create new one
   - If throws clause exists: append to list
4. Handle multiple exceptions at once
5. Preserve code formatting

**Code Snippet:**
```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddThrowsClause)]
[Shared]
internal class AddThrowsClauseCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds 
        => ImmutableArray.Create("CS9341");

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var exceptionType = GetExceptionTypeFromDiagnostic(diagnostic);
        
        context.RegisterCodeFix(
            CodeAction.Create(
                $"Add 'throws {exceptionType}'",
                c => AddThrowsClauseAsync(context.Document, diagnostic, c),
                equivalenceKey: nameof(AddThrowsClauseCodeFixProvider)),
            diagnostic);
    }
}
```

**Testing:**
- [ ] Adds throws to method without existing clause
- [ ] Appends to existing throws clause
- [ ] Handles multiple exceptions
- [ ] Preserves formatting and comments
- [ ] Works with generic exception types

#### Task 8.2: Wrap with Try-Catch (3-4 hours)

**Files to Create:**
```
src/Features/CSharp/Portable/CodeFixes/WrapWithTryCatch/WrapWithTryCatchCodeFixProvider.cs
```

**Implementation Steps:**
1. Create alternative fix for CS9341
2. Identify the invocation causing the error
3. Generate try-catch block:
   - Wrap invocation in try
   - Generate appropriate catch block
   - Preserve existing code structure
4. Handle multiple exceptions with multiple catches

**Testing:**
- [ ] Generates correct try-catch structure
- [ ] Handles multiple exceptions
- [ ] Preserves indentation
- [ ] Works with nested invocations

#### Task 8.3: Remove Unused Throws (2-3 hours)

**Files to Create:**
```
src/Features/CSharp/Portable/CodeFixes/RemoveUnusedThrows/RemoveUnusedThrowsCodeFixProvider.cs
```

**Implementation Steps:**
1. Create code fix for CS9346 (unused throws)
2. Identify which exception type to remove
3. Update syntax:
   - Remove type from list
   - Remove entire clause if last type
   - Preserve comma formatting
4. Handle batch removal

**Testing:**
- [ ] Removes single exception type
- [ ] Removes entire clause when empty
- [ ] Handles batch fix across multiple methods
- [ ] Preserves other exception types

#### Task 8.4: Propagate Throws (3-4 hours)

**Files to Create:**
```
src/Features/CSharp/Portable/CodeActions/PropagateThrows/PropagateThrowsCodeActionProvider.cs
```

**Implementation Steps:**
1. Create code action (not just fix)
2. Find calling method
3. Add throws clause to caller
4. Recurse up call chain with user approval
5. Show preview of all changes

**Testing:**
- [ ] Propagates throws to direct caller
- [ ] Handles call chains
- [ ] Shows preview before applying
- [ ] Works with async methods

### Success Criteria
- ✅ Code fixes appear in lightbulb menu
- ✅ Fixes produce syntactically correct code
- ✅ Batch apply works for multiple instances
- ✅ Preview shows before applying changes

---

## Phase 9: Warning Analyzers

**Status:** ⏳ Not Started  
**Priority:** Highest  
**Estimated Time:** 14-19 hours  
**Dependencies:** Phases 1-6

### Objectives
- Warn when throwing undeclared exceptions (CS9345)
- Warn when declared exceptions are never thrown (CS9346)
- Integrate with control flow analysis
- Configurable warning levels

### Task Breakdown

#### Task 9.1: Undeclared Exception Warning (4-5 hours)

**Files to Create:**
```
src/Analyzers/CSharp/Analyzers/ThrowsClause/UndeclaredExceptionAnalyzer.cs
src/Analyzers/CSharp/Analyzers/ThrowsClause/UndeclaredExceptionAnalyzer.Visitor.cs
```

**Files to Modify:**
```
src/Compilers/CSharp/Portable/Errors/ErrorCode.cs
src/Compilers/CSharp/Portable/CSharpResources.resx
```

**Implementation Steps:**
1. Add CS9345 error code
2. Create diagnostic analyzer inheriting from `DiagnosticAnalyzer`
3. Register syntax node action for `ThrowStatementSyntax`
4. For each throw statement:
   - Get thrown exception type
   - Check if it's in method's throws clause
   - Check if it's derived from declared type
   - Check if it's caught by surrounding try-catch
5. Report diagnostic if undeclared

**Code Snippet:**
```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UndeclaredExceptionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: "CS9345",
        title: "Undeclared exception thrown",
        messageFormat: "Exception '{0}' is thrown but not declared in throws clause",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
    }

    private void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
    {
        var throwStatement = (ThrowStatementSyntax)context.Node;
        var method = GetContainingMethod(throwStatement);
        var thrownType = GetThrownExceptionType(throwStatement, context.SemanticModel);
        
        if (!IsDecla redOrCaught(thrownType, method, throwStatement))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, throwStatement.GetLocation(), thrownType.Name));
        }
    }
}
```

**Testing:**
- [ ] Detects throw statements without throws declaration
- [ ] Allows throws if exception is declared
- [ ] Allows throws if exception is derived type
- [ ] Allows throws if caught by try-catch
- [ ] Handles rethrow (bare `throw;`)

#### Task 9.2: Unused Throws Warning (3-4 hours)

**Files to Create:**
```
src/Analyzers/CSharp/Analyzers/ThrowsClause/UnusedThrowsDeclarationAnalyzer.cs
```

**Files to Modify:**
```
src/Compilers/CSharp/Portable/Errors/ErrorCode.cs
src/Compilers/CSharp/Portable/CSharpResources.resx
```

**Implementation Steps:**
1. Add CS9346 error code
2. Create analyzer for method declarations with throws
3. Scan method body for all throw statements
4. Scan method calls and their throws clauses
5. Compare declared vs. actually thrown
6. Report unused declarations

**Testing:**
- [ ] Detects unused throws declarations
- [ ] Accounts for method calls with throws
- [ ] Handles exception inheritance
- [ ] Works with async methods
- [ ] Handles try-catch-rethrow

#### Task 9.3: Flow Analysis Integration (6-8 hours)

**Files to Modify:**
```
src/Compilers/CSharp/Portable/FlowAnalysis/AbstractFlowPass.cs
src/Compilers/CSharp/Portable/FlowAnalysis/PreciseAbstractFlowPass.cs
```

**Implementation Steps:**
1. Extend flow analysis to track exception flow
2. Add exception state to flow analysis data structures
3. Track exceptions through control flow:
   - Throw statements add to exception set
   - Try-catch removes caught exceptions
   - Method calls add their throws types
   - Finally blocks don't affect exception set
4. Use for more accurate warning analysis

**Testing:**
- [ ] Tracks exceptions through branches
- [ ] Handles try-catch-finally correctly
- [ ] Accounts for method calls
- [ ] Works with nested try-catch
- [ ] Handles complex control flow (loops, switches)

#### Task 9.4: Configuration (1-2 hours)

**Files to Modify:**
```
src/Analyzers/Core/Analyzers/AnalyzerConfigOptions.cs
```

**Implementation Steps:**
1. Add .editorconfig options:
   - `dotnet_diagnostic.CS9345.severity = warning|info|none`
   - `dotnet_diagnostic.CS9346.severity = warning|info|none`
2. Read configuration in analyzers
3. Respect user severity settings

**Testing:**
- [ ] .editorconfig settings work
- [ ] Can disable warnings
- [ ] Can change severity levels
- [ ] Works in all IDEs

### Success Criteria
- ✅ CS9345 warns on undeclared exceptions
- ✅ CS9346 warns on unused declarations
- ✅ Flow analysis is accurate
- ✅ Configurable via .editorconfig
- ✅ <1% false positive rate

---

## Phase 10: CodeLens Integration

**Status:** ⏳ Not Started  
**Priority:** Medium  
**Estimated Time:** 7-10 hours  
**Dependencies:** Phases 1-6

### Task Breakdown

#### Task 10.1: CodeLens Provider (3-4 hours)

**Files to Create:**
```
src/Features/CSharp/Portable/CodeLens/ThrowsClauseCodeLensProvider.cs
```

**Implementation:** Display throws information above method declarations

#### Task 10.2: Reference Finder (2-3 hours)

**Files to Create:**
```
src/Features/Core/Portable/FindUsages/ThrowsClauseReferenceFinder.cs
```

**Implementation:** Find all references to exceptions in throws clauses

#### Task 10.3: Visual Indicators (2-3 hours)

**Implementation:** Gutter icons and highlighting for methods with throws

---

## Phase 11: Refactoring Tools

**Status:** ⏳ Not Started  
**Priority:** Medium  
**Estimated Time:** 12-16 hours  
**Dependencies:** Phases 7-9

### Task Breakdown

#### Task 11.1: Extract Method (4-5 hours)

**Files to Modify:**
```
src/Features/CSharp/Portable/ExtractMethod/CSharpExtractMethodService.cs
```

**Implementation:** Preserve throws clause when extracting code containing throw statements

#### Task 11.2: Inline Method (3-4 hours)

**Files to Modify:**
```
src/Features/CSharp/Portable/InlineMethod/CSharpInlineMethodService.cs
```

**Implementation:** Handle throws clause when inlining methods

#### Task 11.3: Change Signature (3-4 hours)

**Files to Modify:**
```
src/Features/CSharp/Portable/ChangeSignature/CSharpChangeSignatureService.cs
```

**Implementation:** Support adding/removing exception types in Change Signature dialog

#### Task 11.4: Convert to Async (2-3 hours)

**Files to Modify:**
```
src/Features/CSharp/Portable/MakeAsync/AbstractMakeAsyncCodeFixProvider.cs
```

**Implementation:** Handle throws clause when converting to async

---

## Phase 12: Advanced Analysis

**Status:** ⏳ Not Started  
**Priority:** Low  
**Estimated Time:** 21-30 hours  
**Dependencies:** Phases 7-11

### Task Breakdown

#### Task 12.1: Exception Flow Analysis (8-10 hours)

**Files to Create:**
```
src/Compilers/CSharp/Portable/FlowAnalysis/ExceptionFlowAnalysis.cs
```

**Implementation:** Build complete control flow graph with exception edges

#### Task 12.2: Interprocedural Analysis (6-8 hours)

**Files to Create:**
```
src/Analyzers/CSharp/Analyzers/ThrowsClause/InterproceduralExceptionAnalyzer.cs
```

**Implementation:** Analyze exception flow across method boundaries

#### Task 12.3: Generic Exceptions (4-6 hours)

**Files to Modify:**
```
src/Compilers/CSharp/Portable/Symbols/Source/SourceOrdinaryMethodSymbol.cs
```

**Implementation:** Support generic exception types with constraints

#### Task 12.4: Performance Optimization (3-4 hours)

**Implementation:** Cache results, parallelize analysis, optimize hot paths

---

## Testing Strategy

### Unit Tests
- **Minimum per phase:** 20 tests
- **Coverage target:** >90% for new code
- **Test categories:**
  - Happy path scenarios
  - Edge cases
  - Error handling
  - Performance regression

### Integration Tests
- End-to-end IDE scenarios
- Multi-file analysis
- Large codebase testing

### Manual Testing
- Test in VS Code
- Test in Visual Studio
- Test in Rider
- User experience validation

---

## Deployment Strategy

### Phase-by-Phase Rollout
1. Deploy Phase 7-8 together (IDE + Quick Fixes)
2. Deploy Phase 9 separately (Warnings - needs monitoring)
3. Deploy Phase 10-11 together (CodeLens + Refactoring)
4. Deploy Phase 12 incrementally (Advanced features)

### Feature Flags
- Each phase should have feature flag
- Enable for beta testers first
- Gradual rollout to all users
- Easy rollback if issues found

### Monitoring
- Track analyzer performance
- Monitor false positive rates
- Collect user feedback
- Track fix acceptance rates

---

## Risk Assessment

### High Risk
- **Flow analysis accuracy:** Complex control flow may cause false positives
- **Performance impact:** Analysis on large codebases may be slow
- **Mitigation:** Extensive testing, performance benchmarks, caching

### Medium Risk
- **IDE compatibility:** Different IDEs may behave differently
- **Refactoring correctness:** Complex refactorings may not preserve semantics
- **Mitigation:** Test in all IDEs, comprehensive refactoring tests

### Low Risk
- **Code fix quality:** Fixes may produce non-idiomatic code
- **CodeLens overhead:** May impact editor responsiveness
- **Mitigation:** Follow style guidelines, performance testing

---

## Success Metrics

### Phase 7 (IntelliSense)
- IntelliSense completion response time: <100ms
- User acceptance rate: >70%

### Phase 8 (Quick Fixes)
- Fix acceptance rate: >80%
- Fix application time: <500ms

### Phase 9 (Warnings)
- False positive rate: <1%
- Analysis time: <200ms per method
- User opt-out rate: <10%

### Phase 10 (CodeLens)
- CodeLens update time: <100ms
- Memory overhead: <5MB per solution

### Phase 11 (Refactoring)
- Refactoring correctness: 100%
- User satisfaction: >85%

### Phase 12 (Advanced)
- Analysis scalability: Up to 500k LOC
- Cache hit rate: >90%
- Generic exception support: 100% correct

---

## Resources Required

### Engineering
- **1 Senior Compiler Engineer** - Flow analysis, advanced features
- **2 IDE Engineers** - IntelliSense, code fixes, refactoring
- **1 QA Engineer** - Testing, performance validation

### Infrastructure
- Performance testing environment
- Large codebase test corpus
- CI/CD pipeline for phased rollout

### Timeline
- **Phase 7-8:** 4-6 weeks
- **Phase 9:** 3-4 weeks
- **Phase 10-11:** 4-5 weeks
- **Phase 12:** 5-6 weeks
- **Total:** 16-21 weeks (4-5 months)

---

## Next Steps

1. **Immediate (Week 1):**
   - Review and approve this plan
   - Set up feature branch: `features/throws-extended`
   - Create tracking issues for each phase

2. **Phase 7 Start (Week 2):**
   - Begin IntelliSense completion provider
   - Set up unit test infrastructure
   - Create initial prototypes

3. **Regular Cadence:**
   - Weekly progress reviews
   - Bi-weekly demos to stakeholders
   - Monthly performance reviews

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-27  
**Status:** Ready for Review
