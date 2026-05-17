// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private BoundExpression BindCsxElementExpression(CsxElementExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
#if DEBUG
            MarkCsxOriginalExpressionsAsBound(node);
#endif

            var generatedExpression = SyntaxFactory.ParseExpression(BuildCsxElementExpression(node));
            var boundExpression = BindGeneratedCsxExpression(generatedExpression, diagnostics);
            boundExpression.WasCompilerGenerated = true;
            return boundExpression;
        }

#if DEBUG
        private void MarkCsxOriginalExpressionsAsBound(CsxElementExpressionSyntax node)
        {
            foreach (var attribute in node.Attributes)
            {
                if (attribute.Expression is { } expression)
                {
                    _ = BindExpression(expression, BindingDiagnosticBag.Discarded);
                }
            }

            foreach (var child in node.Children)
            {
                switch (child)
                {
                    case CsxElementChildSyntax elementChild:
                        MarkCsxOriginalExpressionsAsBound(elementChild.Element);
                        break;
                    case CsxExpressionChildSyntax expressionChild:
                        _ = BindExpression(expressionChild.Expression, BindingDiagnosticBag.Discarded);
                        break;
                }
            }
        }

        private BoundExpression BindGeneratedCsxExpression(ExpressionSyntax generatedExpression, BindingDiagnosticBag diagnostics)
        {
            var inMethodBinder = GetInMethodBinderWithIdentifierMap();
            var identifierMap = inMethodBinder?.IdentifierMap;

            if (inMethodBinder is not null)
            {
                inMethodBinder.IdentifierMap = null!;
            }

            try
            {
                return BindExpression(generatedExpression, diagnostics);
            }
            finally
            {
                if (inMethodBinder is not null)
                {
                    inMethodBinder.IdentifierMap = identifierMap!;
                }
            }
        }

        private InMethodBinder? GetInMethodBinderWithIdentifierMap()
        {
            Binder? current = this;
            while (current is not null)
            {
                if (current is InMethodBinder { IdentifierMap: not null } inMethodBinder)
                {
                    return inMethodBinder;
                }

                current = current.Next;
            }

            return null;
        }
#else
        private BoundExpression BindGeneratedCsxExpression(ExpressionSyntax generatedExpression, BindingDiagnosticBag diagnostics)
        {
            return BindExpression(generatedExpression, diagnostics);
        }
#endif

        private static string BuildCsxElementExpression(CsxElementExpressionSyntax node)
        {
            var builder = new StringBuilder();
            AppendCsxElementExpression(builder, node);
            return builder.ToString();
        }

        private static void AppendCsxElementExpression(StringBuilder builder, CsxElementExpressionSyntax node)
        {
            builder.Append(BuildCsxElementExpressionText(node));
        }

        private static string BuildCsxElementExpressionText(CsxElementExpressionSyntax node)
        {
            var builder = new StringBuilder();
            builder.Append("global::H.CSX.Markup.Factory.Create(");
            AppendStringLiteral(builder, node.Identifier.ValueText);
            builder.Append(')');
            var expression = builder.ToString();

            foreach (var attribute in node.Attributes)
            {
                var attributeName = attribute.Identifier.ValueText;
                var methodName = attributeName.StartsWith("On", StringComparison.Ordinal) ? "Event" : "Attr";

                builder.Clear();
                builder.Append("global::H.CSX.Markup.Factory.").Append(methodName).Append('(');
                builder.Append(expression);
                builder.Append(", ");
                AppendStringLiteral(builder, attributeName);
                builder.Append(", ");
                AppendCsxAttributeValue(builder, attributeName, attribute, methodName == "Event");
                builder.Append(')');
                expression = builder.ToString();
            }

            foreach (var child in node.Children)
            {
                builder.Clear();
                builder.Append("global::H.CSX.Markup.Factory.Child(");
                builder.Append(expression);
                builder.Append(", ");
                switch (child)
                {
                    case CsxElementChildSyntax elementChild:
                        builder.Append(BuildCsxElementExpressionText(elementChild.Element));
                        break;
                    case CsxExpressionChildSyntax expressionChild:
                        builder.Append(expressionChild.Expression.ToFullString());
                        break;
                }
                builder.Append(')');
                expression = builder.ToString();
            }

            return expression;
        }

        private static void AppendCsxAttributeValue(StringBuilder builder, string attributeName, CsxAttributeSyntax attribute, bool isEvent)
        {
            if (attribute.Expression is null)
            {
                builder.Append("true");
                return;
            }

            if (isEvent ||
                attributeName != "Text" ||
                attribute.Expression.Kind() == SyntaxKind.StringLiteralExpression ||
                (attribute.OpenBraceToken.RawKind == 0 && attribute.CloseBraceToken.RawKind == 0))
            {
                builder.Append(attribute.Expression.ToFullString());
                return;
            }

            if (attribute.Expression.Kind() == SyntaxKind.InterpolatedStringExpression)
            {
                builder.Append("global::H.CSX.Markup.BindingRuntime.Interpolation(");
            }
            else
            {
                builder.Append("global::H.CSX.Markup.BindingRuntime.Value(");
            }

            builder.Append(attribute.Expression.ToFullString());
            builder.Append(')');
        }

        private static void AppendStringLiteral(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (var c in value)
            {
                builder.Append(c switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\0' => "\\0",
                    '\a' => "\\a",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\v' => "\\v",
                    _ => c
                });
            }

            builder.Append('"');
        }
    }
}
