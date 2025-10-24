// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ThrowsClause;

/// <summary>
/// Analyzer that warns when a throws clause declares redundant exception types.
/// A type is redundant if a base type of it is also declared in the same throws clause.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class RedundantThrowsTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: IDEDiagnosticIds.RedundantThrowsTypeDiagnosticId,
        title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Redundant_throws_type), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        messageFormat: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Exception_0_is_redundant_base_type_1_is_already_declared), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        category: DiagnosticCategory.Style,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticDescriptorHelper.GetHelpLinkUri(IDEDiagnosticIds.RedundantThrowsTypeDiagnosticId));

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

        var exceptionTypes = methodDeclaration.ThrowsClause.ExceptionTypes;
        if (exceptionTypes.Count < 2)
            return; // Need at least 2 types to have redundancy

        var semanticModel = context.SemanticModel;

        // Build a list of (syntax, symbol) pairs
        var typeInfo = new (TypeSyntax Syntax, ITypeSymbol Symbol)[exceptionTypes.Count];
        for (var i = 0; i < exceptionTypes.Count; i++)
        {
            var typeSyntax = exceptionTypes[i];
            var typeSymbol = semanticModel.GetTypeInfo(typeSyntax, context.CancellationToken).Type;
            
            if (typeSymbol == null || typeSymbol.TypeKind == TypeKind.Error)
                return; // Skip if we can't resolve all types

            typeInfo[i] = (typeSyntax, typeSymbol);
        }

        // Check each type to see if it's redundant
        for (var i = 0; i < typeInfo.Length; i++)
        {
            var (currentSyntax, currentType) = typeInfo[i];

            // Check if any other type in the list is a base type of this one
            for (var j = 0; j < typeInfo.Length; j++)
            {
                if (i == j)
                    continue;

                var (_, otherType) = typeInfo[j];

                // If currentType inherits from otherType, then currentType is redundant
                if (currentType.InheritsFromOrEquals(otherType) &&
                    !currentType.Equals(otherType, SymbolEqualityComparer.Default))
                {
                    // currentType is redundant because otherType (a base type) is already declared
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            s_descriptor,
                            currentSyntax.GetLocation(),
                            currentType.Name,
                            otherType.Name));
                    
                    // Only report once per redundant type (for the first base type found)
                    break;
                }
            }
        }
    }
}
