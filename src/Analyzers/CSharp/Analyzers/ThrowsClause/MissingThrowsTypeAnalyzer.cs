// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ThrowsClause;

/// <summary>
/// Analyzer that warns when a method throws an exception that is not declared in its throws clause.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class MissingThrowsTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: IDEDiagnosticIds.MissingThrowsTypeDiagnosticId,
        title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Missing_throws_type), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        messageFormat: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Exception_0_is_thrown_but_not_declared_in_throws_clause), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        category: DiagnosticCategory.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticDescriptorHelper.GetHelpLinkUri(IDEDiagnosticIds.MissingThrowsTypeDiagnosticId));

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

            context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
        });
    }

    private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
    {
        var throwStatement = (ThrowStatementSyntax)context.Node;
        
        // Skip rethrow statements (throw without expression)
        if (throwStatement.Expression == null)
            return;

        var semanticModel = context.SemanticModel;
        
        // Get the type of the thrown exception
        var thrownType = semanticModel.GetTypeInfo(throwStatement.Expression, context.CancellationToken).Type;
        if (thrownType == null || thrownType.TypeKind == TypeKind.Error)
            return;

        // Find the containing method
        var containingMethod = GetContainingMethod(throwStatement);
        if (containingMethod == null)
            return;

        var methodSymbol = semanticModel.GetDeclaredSymbol(containingMethod, context.CancellationToken) as IMethodSymbol;
        if (methodSymbol == null)
            return;

        // Check if the exception is caught by a surrounding try-catch
        if (IsExceptionCaught(throwStatement, thrownType, semanticModel, context.CancellationToken))
            return;

        // Check if the exception type is declared in the throws clause
        if (IsDeclaredInThrowsClause(thrownType, methodSymbol))
            return;

        // Report diagnostic
        context.ReportDiagnostic(
            Diagnostic.Create(
                s_descriptor,
                throwStatement.Expression.GetLocation(),
                thrownType.Name));
    }

    private static BaseMethodDeclarationSyntax? GetContainingMethod(SyntaxNode node)
    {
        return node.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>(
            n => n is MethodDeclarationSyntax or
                 ConstructorDeclarationSyntax or
                 DestructorDeclarationSyntax or
                 OperatorDeclarationSyntax or
                 ConversionOperatorDeclarationSyntax);
    }

    private static bool IsExceptionCaught(ThrowStatementSyntax throwStatement, ITypeSymbol thrownType, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        // Walk up the syntax tree to find any try-catch blocks
        var currentNode = throwStatement.Parent;
        while (currentNode != null)
        {
            if (currentNode is TryStatementSyntax tryStatement)
            {
                // Check each catch clause
                foreach (var catchClause in tryStatement.Catches)
                {
                    if (catchClause.Declaration == null)
                    {
                        // Catch-all clause (catch without type)
                        return true;
                    }

                    var catchType = semanticModel.GetTypeInfo(catchClause.Declaration.Type, cancellationToken).Type;
                    if (catchType != null)
                    {
                        // Check if the thrown type is the same as or derived from the catch type
                        var compilation = semanticModel.Compilation;
                        if (thrownType.Equals(catchType, SymbolEqualityComparer.Default) ||
                            thrownType.InheritsFromOrEquals(catchType))
                        {
                            return true;
                        }
                    }
                }
            }

            // Don't check beyond the containing method
            if (currentNode is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax)
                break;

            currentNode = currentNode.Parent;
        }

        return false;
    }

    private static bool IsDeclaredInThrowsClause(ITypeSymbol thrownType, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ThrowsTypes.IsDefaultOrEmpty)
            return false;

        foreach (var declaredType in methodSymbol.ThrowsTypes)
        {
            // Check if the thrown type is the same as or derived from a declared type
            if (thrownType.Equals(declaredType, SymbolEqualityComparer.Default) ||
                thrownType.InheritsFromOrEquals(declaredType))
            {
                return true;
            }
        }

        return false;
    }
}
