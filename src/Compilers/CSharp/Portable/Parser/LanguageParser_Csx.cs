// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

    internal sealed partial class LanguageParser
    {
        private bool IsPossibleCsxElement()
            => CurrentToken.Kind == SyntaxKind.LessThanToken &&
               IsCsxNameToken(PeekToken(1));

        private ExpressionSyntax ParseCsxElementExpression()
            => ParseCsxElementSyntax();

        private CsxElementExpressionSyntax ParseCsxElementSyntax()
        {
            var lessThanToken = EatToken(SyntaxKind.LessThanToken);
            var identifier = EatCsxNameToken();

            var attributesBuilder = _pool.Allocate<CsxAttributeSyntax>();
            while (CurrentToken.Kind != SyntaxKind.GreaterThanToken &&
                   CurrentToken.Kind != SyntaxKind.SlashGreaterThanToken &&
                   CurrentToken.Kind != SyntaxKind.EndOfFileToken)
            {
                if (CurrentToken.Kind == SyntaxKind.SlashToken && PeekToken(1).Kind == SyntaxKind.GreaterThanToken)
                {
                    break;
                }

                if (!IsCsxNameToken(CurrentToken))
                {
                    EatToken();
                    continue;
                }

                attributesBuilder.Add(ParseCsxAttribute());
            }

            var attributes = _pool.ToListAndFree(attributesBuilder);

            SyntaxToken? greaterThanToken = null;
            SyntaxToken? slashGreaterThanToken = null;
            var children = default(SyntaxList<CsxChildSyntax>);
            SyntaxToken? endLessThanToken = null;
            SyntaxToken? endSlashToken = null;
            SyntaxToken? endIdentifier = null;
            SyntaxToken? endGreaterThanToken = null;

            if (CurrentToken.Kind == SyntaxKind.SlashGreaterThanToken)
            {
                slashGreaterThanToken = EatToken(SyntaxKind.SlashGreaterThanToken);
            }
            else if (CurrentToken.Kind == SyntaxKind.SlashToken && PeekToken(1).Kind == SyntaxKind.GreaterThanToken)
            {
                var slash = EatToken(SyntaxKind.SlashToken);
                var greaterThan = EatToken(SyntaxKind.GreaterThanToken);
                slashGreaterThanToken = SyntaxFactory.Token(slash.GetLeadingTrivia(), SyntaxKind.SlashGreaterThanToken, greaterThan.GetTrailingTrivia());
            }
            else
            {
                greaterThanToken = EatToken(SyntaxKind.GreaterThanToken);
                children = ParseCsxChildren();
                endLessThanToken = EatToken(SyntaxKind.LessThanToken);
                endSlashToken = EatToken(SyntaxKind.SlashToken);
                endIdentifier = IsCsxNameToken(CurrentToken)
                    ? EatCsxNameToken()
                    : EatToken(SyntaxKind.IdentifierToken);
                endGreaterThanToken = EatToken(SyntaxKind.GreaterThanToken);
            }

            return _syntaxFactory.CsxElementExpression(
                lessThanToken,
                identifier,
                attributes,
                greaterThanToken,
                slashGreaterThanToken,
                children,
                endLessThanToken,
                endSlashToken,
                endIdentifier,
                endGreaterThanToken);
        }

        private CsxAttributeSyntax ParseCsxAttribute()
        {
            var identifier = EatCsxNameToken();
            SyntaxToken? equalsToken = null;
            SyntaxToken? openBraceToken = null;
            ExpressionSyntax? expression = null;
            SyntaxToken? closeBraceToken = null;

            if (CurrentToken.Kind == SyntaxKind.EqualsToken)
            {
                equalsToken = EatToken(SyntaxKind.EqualsToken);
                if (CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    openBraceToken = EatToken(SyntaxKind.OpenBraceToken);
                    expression = ParseExpressionCore();
                    closeBraceToken = EatToken(SyntaxKind.CloseBraceToken);
                }
                else if (CurrentToken.Kind == SyntaxKind.StringLiteralToken)
                {
                    expression = _syntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        EatToken(SyntaxKind.StringLiteralToken));
                }
                else
                {
                    expression = ParseExpressionCore();
                }
            }

            return _syntaxFactory.CsxAttribute(
                identifier,
                equalsToken,
                openBraceToken,
                expression,
                closeBraceToken);
        }

        private SyntaxList<CsxChildSyntax> ParseCsxChildren()
        {
            var childrenBuilder = _pool.Allocate<CsxChildSyntax>();

            while (CurrentToken.Kind != SyntaxKind.EndOfFileToken)
            {
                if (CurrentToken.Kind == SyntaxKind.LessThanToken && PeekToken(1).Kind == SyntaxKind.SlashToken)
                {
                    break;
                }

                if (IsPossibleCsxElement())
                {
                    childrenBuilder.Add(_syntaxFactory.CsxElementChild(ParseCsxElementSyntax()));
                    continue;
                }

                if (CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    var openBraceToken = EatToken(SyntaxKind.OpenBraceToken);
                    var expression = ParseExpressionCore();
                    var closeBraceToken = EatToken(SyntaxKind.CloseBraceToken);
                    childrenBuilder.Add(_syntaxFactory.CsxExpressionChild(openBraceToken, expression, closeBraceToken));
                    continue;
                }

                // Raw text children are not part of the MVP. Preserve parser progress.
                EatToken();
            }

            return _pool.ToListAndFree(childrenBuilder);
        }

        private SyntaxToken EatCsxNameToken()
        {
            return IsCsxNameToken(CurrentToken)
                ? EatToken()
                : EatToken(SyntaxKind.IdentifierToken);
        }

        private static bool IsCsxNameToken(SyntaxToken token)
            => token.Kind == SyntaxKind.IdentifierToken || SyntaxFacts.IsKeywordKind(token.Kind);
    }
}
