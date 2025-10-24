// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ThrowsClause;

/// <summary>
/// Analyzer that warns when a throws clause declares an exception type that is never thrown in the method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UnnecessaryThrowsTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: IDEDiagnosticIds.UnnecessaryThrowsTypeDiagnosticId,
        title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Unnecessary_throws_type), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        messageFormat: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Exception_0_is_declared_but_never_thrown), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        category: DiagnosticCategory.Style,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticDescriptorHelper.GetHelpLinkUri(IDEDiagnosticIds.UnnecessaryThrowsTypeDiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [s_descriptor];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            // Only analyze if the compilation supports throws clauses
            if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp13)
                return;

            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Only analyze methods with throws clauses
        if (methodDeclaration.ThrowsClause == null)
            return;

        var semanticModel = context.SemanticModel;
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) as IMethodSymbol;
        if (methodSymbol == null || methodSymbol.ThrowsTypes.IsDefaultOrEmpty)
            return;

        // Get the method body
        var body = methodDeclaration.Body ?? (SyntaxNode?)methodDeclaration.ExpressionBody?.Expression;
        if (body == null)
            return;

        // Collect all exception types that could be thrown
        var thrownTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        CollectThrownExceptions(body, semanticModel, thrownTypes, context.CancellationToken);

        // Check each declared exception type
        var declaredTypes = methodDeclaration.ThrowsClause.ExceptionTypes;
        for (var i = 0; i < declaredTypes.Count; i++)
        {
            var declaredTypeSyntax = declaredTypes[i];
            var declaredType = semanticModel.GetTypeInfo(declaredTypeSyntax, context.CancellationToken).Type;
            
            if (declaredType == null)
                continue;

            // Check if this type or any derived type is actually thrown
            var isThrown = thrownTypes.Any(thrown =>
                thrown.Equals(declaredType, SymbolEqualityComparer.Default) ||
                thrown.InheritsFromOrEquals(declaredType));

            if (!isThrown)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        s_descriptor,
                        declaredTypeSyntax.GetLocation(),
                        declaredType.Name));
            }
        }
    }

    private static void CollectThrownExceptions(
        SyntaxNode node,
        SemanticModel semanticModel,
        HashSet<ITypeSymbol> thrownTypes,
        CancellationToken cancellationToken)
    {
        // Walk the syntax tree to find all throw statements and method calls
        foreach (var descendant in node.DescendantNodesAndSelf())
        {
            switch (descendant)
            {
                case ThrowStatementSyntax throwStatement:
                    // Skip rethrow statements
                    if (throwStatement.Expression != null)
                    {
                        var thrownType = semanticModel.GetTypeInfo(throwStatement.Expression, cancellationToken).Type;
                        if (thrownType != null && thrownType.TypeKind != TypeKind.Error)
                        {
                            // Only add if not caught by surrounding try-catch
                            if (!IsExceptionCaught(throwStatement, thrownType, semanticModel, cancellationToken))
                            {
                                thrownTypes.Add(thrownType);
                            }
                        }
                    }
                    break;

                case InvocationExpressionSyntax invocation:
                    // Check if the invoked method has a throws clause
                    var invokedSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
                    if (invokedSymbol?.ThrowsTypes.IsDefaultOrEmpty == false)
                    {
                        // Add all declared exception types from the called method
                        foreach (var exceptionType in invokedSymbol.ThrowsTypes)
                        {
                            // Only add if not caught by surrounding try-catch
                            if (!IsExceptionCaughtForInvocation(invocation, exceptionType, semanticModel, cancellationToken))
                            {
                                thrownTypes.Add(exceptionType);
                            }
                        }
                    }
                    break;

                case TryStatementSyntax:
                    // Don't descend into try blocks - they're handled separately
                    // This is handled by IsExceptionCaught checks
                    break;
            }
        }
    }

    private static bool IsExceptionCaught(
        ThrowStatementSyntax throwStatement,
        ITypeSymbol thrownType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var currentNode = throwStatement.Parent;
        while (currentNode != null)
        {
            if (currentNode is TryStatementSyntax tryStatement)
            {
                foreach (var catchClause in tryStatement.Catches)
                {
                    if (catchClause.Declaration == null)
                        return true; // Catch-all

                    var catchType = semanticModel.GetTypeInfo(catchClause.Declaration.Type, cancellationToken).Type;
                    if (catchType != null &&
                        (thrownType.Equals(catchType, SymbolEqualityComparer.Default) ||
                         thrownType.InheritsFromOrEquals(catchType)))
                    {
                        return true;
                    }
                }
            }

            if (currentNode is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax)
                break;

            currentNode = currentNode.Parent;
        }

        return false;
    }

    private static bool IsExceptionCaughtForInvocation(
        InvocationExpressionSyntax invocation,
        ITypeSymbol exceptionType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var currentNode = invocation.Parent;
        while (currentNode != null)
        {
            if (currentNode is TryStatementSyntax tryStatement)
            {
                foreach (var catchClause in tryStatement.Catches)
                {
                    if (catchClause.Declaration == null)
                        return true; // Catch-all

                    var catchType = semanticModel.GetTypeInfo(catchClause.Declaration.Type, cancellationToken).Type;
                    if (catchType != null &&
                        (exceptionType.Equals(catchType, SymbolEqualityComparer.Default) ||
                         exceptionType.InheritsFromOrEquals(catchType)))
                    {
                        return true;
                    }
                }
            }

            if (currentNode is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax)
                break;

            currentNode = currentNode.Parent;
        }

        return false;
    }
}
