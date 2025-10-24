// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

/// <summary>
/// Provides IntelliSense completion for exception types in throws clauses.
/// Suggests System.Exception-derived types when typing after 'throws' keyword.
/// </summary>
[ExportCompletionProvider(nameof(ThrowsClauseCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(SymbolCompletionProvider))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ThrowsClauseCompletionProvider() : LSPCompletionProvider
{
    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);

            // Check if we're in a throws clause context
            if (!IsInThrowsClauseContext(token, position, out var throwsClause))
                return;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var exceptionType = semanticModel.Compilation.GetTypeByMetadataName("System.Exception");

            if (exceptionType == null)
                return;

            // Get all exception types from the compilation
            var exceptionTypes = GetExceptionTypes(semanticModel.Compilation, exceptionType, cancellationToken);

            // Create completion items for each exception type
            foreach (var type in exceptionTypes)
            {
                var item = CreateCompletionItem(type, context);
                context.AddItem(item);
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // Swallow exceptions from completion provider
        }
    }

    private static bool IsInThrowsClauseContext(SyntaxToken token, int position, out ThrowsClauseSyntax? throwsClause)
    {
        throwsClause = null;

        // Walk up the tree to find if we're inside a throws clause
        var node = token.Parent;
        while (node != null)
        {
            if (node is ThrowsClauseSyntax throws)
            {
                throwsClause = throws;
                return true;
            }

            // Stop if we hit a method declaration (but check its throws clause first)
            if (node is MethodDeclarationSyntax method)
            {
                throwsClause = method.ThrowsClause;
                return throwsClause != null && throwsClause.FullSpan.Contains(position);
            }

            node = node.Parent;
        }

        // Also check if the token itself is the 'throws' keyword
        if (token.IsKind(SyntaxKind.ThrowsKeyword))
        {
            // Look for the parent throws clause
            throwsClause = token.Parent as ThrowsClauseSyntax;
            return throwsClause != null;
        }

        return false;
    }

    private static ImmutableArray<INamedTypeSymbol> GetExceptionTypes(
        Compilation compilation,
        INamedTypeSymbol exceptionType,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        // Add System.Exception itself
        builder.Add(exceptionType);

        // Add common exception types
        AddCommonExceptionTypes(builder, compilation);

        // Get all types from the compilation that derive from System.Exception
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type, cancellationToken)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.IsAccessibleWithin(compilation.Assembly) &&
                       !t.IsAbstract &&
                       InheritsFrom(t, exceptionType))
            .OrderBy(t => GetExceptionPriority(t))
            .ThenBy(t => t.Name);

        builder.AddRange(allTypes);

        return builder.ToImmutable();
    }

    private static void AddCommonExceptionTypes(ImmutableArray<INamedTypeSymbol>.Builder builder, Compilation compilation)
    {
        // Add commonly used exception types at the top
        var commonTypes = new[]
        {
            "System.ArgumentException",
            "System.ArgumentNullException",
            "System.InvalidOperationException",
            "System.NotSupportedException",
            "System.NotImplementedException",
            "System.IO.IOException",
            "System.IO.FileNotFoundException",
            "System.UnauthorizedAccessException",
            "System.TimeoutException",
            "System.FormatException",
            "System.OverflowException"
        };

        foreach (var typeName in commonTypes)
        {
            var type = compilation.GetTypeByMetadataName(typeName);
            if (type != null)
            {
                builder.Add(type);
            }
        }
    }

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static int GetExceptionPriority(INamedTypeSymbol type)
    {
        // Prioritize more commonly used exceptions
        var name = type.Name;
        if (name.Contains("ArgumentException"))
            return 1;
        if (name.Contains("InvalidOperationException"))
            return 2;
        if (name.Contains("IOException"))
            return 3;
        if (name.Contains("NotSupported"))
            return 4;
        if (name.Contains("NotImplemented"))
            return 5;
        if (name.Contains("Unauthorized"))
            return 6;
        if (name.Contains("Timeout"))
            return 7;
        if (name.Contains("Format"))
            return 8;

        // Framework exceptions get priority over user exceptions
        if (type.ContainingNamespace?.ToDisplayString().StartsWith("System") == true)
            return 10;

        return 100; // User-defined exceptions
    }

    private static CompletionItem CreateCompletionItem(INamedTypeSymbol type, CompletionContext context)
    {
        var displayText = type.Name;
        var fullyQualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Get XML documentation summary
        var documentation = type.GetDocumentationComment(context.Document.Project.Solution.Services);
        var summaryText = documentation?.SummaryText ?? string.Empty;

        return CompletionItem.Create(
            displayText: displayText,
            displayTextSuffix: string.Empty,
            filterText: displayText,
            sortText: $"{GetExceptionPriority(type):D3}_{displayText}",
            properties: ImmutableDictionary<string, string>.Empty.Add("FullyQualifiedName", fullyQualifiedName),
            tags: ImmutableArray.Create(WellKnownTags.Class, WellKnownTags.Public),
            rules: CompletionItemRules.Default,
            displayTextPrefix: string.Empty,
            inlineDescription: summaryText.Length > 100 ? summaryText.Substring(0, 100) + "..." : summaryText);
    }

    public override Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
    {
        // Return the full description including XML docs
        var description = item.InlineDescription;
        if (!string.IsNullOrEmpty(description))
        {
            var textContent = new List<TaggedText>
            {
                new TaggedText(TextTags.Text, description)
            };

            return Task.FromResult<CompletionDescription?>(
                CompletionDescription.Create(textContent.ToImmutableArray()));
        }

        return Task.FromResult<CompletionDescription?>(null);
    }

    public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitCharacter, CancellationToken cancellationToken)
    {
        // Insert the exception type name
        var textChange = new TextChange(item.Span, item.DisplayText);
        return Task.FromResult(CompletionChange.Create(textChange));
    }
}
