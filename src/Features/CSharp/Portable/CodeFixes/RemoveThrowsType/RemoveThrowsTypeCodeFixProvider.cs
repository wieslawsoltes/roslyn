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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveThrowsType;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveThrowsType), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class RemoveThrowsTypeCodeFixProvider() : CodeFixProvider
{
    // This code fix is triggered by the UnnecessaryThrowsTypeAnalyzer (IDE0391)
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UnnecessaryThrowsTypeDiagnosticId];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpFeaturesResources.Remove_throws_type,
                cancellationToken => RemoveThrowsTypeAsync(document, diagnosticSpan, cancellationToken),
                nameof(CSharpFeaturesResources.Remove_throws_type)),
            diagnostic);

        return Task.CompletedTask;
    }

    private static async Task<Document> RemoveThrowsTypeAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Find the exception type in the throws clause
        var exceptionType = root.FindNode(span).FirstAncestorOrSelf<TypeSyntax>();
        if (exceptionType == null)
            return document;

        // Find the containing throws clause
        var throwsClause = exceptionType.FirstAncestorOrSelf<ThrowsClauseSyntax>();
        if (throwsClause == null)
            return document;

        // Find the containing method
        var method = throwsClause.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method == null)
            return document;

        MethodDeclarationSyntax newMethod;
        var exceptionTypes = throwsClause.ExceptionTypes;

        if (exceptionTypes.Count == 1)
        {
            // Remove the entire throws clause if this is the only exception
            newMethod = method.WithThrowsClause(null);
        }
        else
        {
            // Remove just this exception type from the list
            var index = exceptionTypes.IndexOf(exceptionType);
            if (index < 0)
                return document;

            var newExceptionTypes = exceptionTypes.RemoveAt(index);
            var newThrowsClause = throwsClause.WithExceptionTypes(newExceptionTypes);
            newMethod = method.WithThrowsClause(newThrowsClause);
        }

        var newRoot = root.ReplaceNode(method, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }
}
