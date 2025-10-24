// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddThrowsClause;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddThrowsClause), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class AddThrowsClauseCodeFixProvider() : CodeFixProvider
{
    // This code fix is triggered by the MissingThrowsTypeAnalyzer (IDE0390)
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.MissingThrowsTypeDiagnosticId];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpFeaturesResources.Add_throws_clause,
                cancellationToken => AddThrowsClauseAsync(document, diagnosticSpan, cancellationToken),
                nameof(CSharpFeaturesResources.Add_throws_clause)),
            diagnostic);

        return Task.CompletedTask;
    }

    private static async Task<Document> AddThrowsClauseAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Find the throw statement
        var throwStatement = root.FindNode(span).FirstAncestorOrSelf<ThrowStatementSyntax>();
        if (throwStatement?.Expression == null)
            return document;

        // Get the exception type being thrown
        var exceptionType = semanticModel.GetTypeInfo(throwStatement.Expression, cancellationToken).Type as INamedTypeSymbol;
        if (exceptionType == null)
            return document;

        // Find the containing method
        var method = throwStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method == null)
            return document;

        // Get the method symbol to check existing throws types
        var methodSymbol = semanticModel.GetDeclaredSymbol(method, cancellationToken) as IMethodSymbol;
        if (methodSymbol == null)
            return document;

        // Check if the exception type is already in the throws clause
        if (methodSymbol.ThrowsTypes.Any(t => SymbolEqualityComparer.Default.Equals(t, exceptionType)))
            return document;

        // Create the new throws clause or update existing one
        var generator = SyntaxGenerator.GetGenerator(document);
        var exceptionTypeSyntax = (TypeSyntax)generator.TypeExpression(exceptionType);

        MethodDeclarationSyntax newMethod;
        if (method.ThrowsClause == null)
        {
            // Create new throws clause
            var throwsClause = SyntaxFactory.ThrowsClause(
                SyntaxFactory.Token(SyntaxKind.ThrowsKeyword),
                SyntaxFactory.SingletonSeparatedList(exceptionTypeSyntax));

            newMethod = method.WithThrowsClause(throwsClause);
        }
        else
        {
            // Add to existing throws clause
            var newExceptionTypes = method.ThrowsClause.ExceptionTypes.Add(exceptionTypeSyntax);
            var newThrowsClause = method.ThrowsClause.WithExceptionTypes(newExceptionTypes);
            newMethod = method.WithThrowsClause(newThrowsClause);
        }

        var newRoot = root.ReplaceNode(method, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }
}
