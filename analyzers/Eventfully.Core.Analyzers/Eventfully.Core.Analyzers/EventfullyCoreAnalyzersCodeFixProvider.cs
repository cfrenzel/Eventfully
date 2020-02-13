using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;

namespace Eventfully.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EventfullyCoreAnalyzersCodeFixProvider)), Shared]
    public class EventfullyCoreAnalyzersCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add MapIdFor<> to constructor";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EventfullyCoreAnalyzersAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            //var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
            string unmappedMessageType = null;
            //if (!diagnostic.Properties.TryGetValue("UnmappedMessage", out unmappedMessageType))
            //return;  
            diagnostic.Properties.TryGetValue("UnmappedMessage", out unmappedMessageType);

            context.RegisterCodeFix(
        CodeAction.Create(
            title: title,
            createChangedDocument: c => AddMissingMapIdFors(context.Document, declaration, unmappedMessageType, c),
            equivalenceKey: title),
        diagnostic);
            return;
        }
        private async Task<Document> AddMissingMapIdFors(Document document, ClassDeclarationSyntax classExpr, string messageType, CancellationToken cancellationToken)
        {

            var constructorExpr = classExpr.ChildNodes().SingleOrDefault(x => x is ConstructorDeclarationSyntax);
            if (constructorExpr == null)
                return document;

            var oldblock = constructorExpr.ChildNodes().OfType<BlockSyntax>().First();

            var leadingTrivia = GetLeadingWhiteSpaceForExpression(oldblock);
            ExpressionSyntax es = SyntaxFactory.ParseExpression($"this.MapIdFor<{messageType}>((m, md) => m.<ProcessId>);")
                .WithLeadingTrivia(leadingTrivia) 
                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n"));

            var statement = SyntaxFactory.ExpressionStatement(es);
            var newblock = oldblock.AddStatements(statement);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(oldblock, newblock);
            return document.WithSyntaxRoot(newRoot);
        }

        public SyntaxTriviaList GetLeadingWhiteSpaceForExpression(SyntaxNode nearbyNode)
        {
            string _tabSpace = "    ";
            SyntaxTriviaList leadingWhiteSpace;
            //SyntaxTriviaList endingWhiteSpace;

            if (nearbyNode == null)
                return new SyntaxTriviaList();

            SyntaxNode parentBlock = null;
            if (!(nearbyNode is BlockSyntax))
                parentBlock = nearbyNode.Ancestors().FirstOrDefault(x => x is BlockSyntax);
            else
                parentBlock = nearbyNode;

            if (parentBlock == null)
                return nearbyNode.GetTrailingTrivia();

            leadingWhiteSpace = parentBlock.GetLeadingTrivia()
                          .Where(t => t.Kind() == SyntaxKind.WhitespaceTrivia)
                          .ToSyntaxTriviaList()
                          .Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, _tabSpace));

            return leadingWhiteSpace;
            //if (nearbyNode == null)
            //    return new SyntaxTriviaList();

            //if (nearbyNode.Kind() == SyntaxKind.PropertyDeclaration ||
            //     nearbyNode.Kind() == SyntaxKind.MethodDeclaration ||
            //     nearbyNode.Kind() == SyntaxKind.FieldDeclaration ||
            //     nearbyNode.Kind() == SyntaxKind.ConstructorDeclaration
            //)
            //{
            //    leadingWhiteSpace = nearbyNode.GetLeadingTrivia().Where(t =>
            //        t.Kind() == SyntaxKind.WhitespaceTrivia).ToSyntaxTriviaList();

            //    //endingWhiteSpace = addingTo.GetTrailingTrivia().Where(t =>
            //    //    t.Kind() == SyntaxKind.WhitespaceTrivia).ToSyntaxTriviaList();
            //}
            //else
            //{
            //    leadingWhiteSpace = nearbyNode.GetLeadingTrivia()
            //                  .Where(t => t.Kind() == SyntaxKind.WhitespaceTrivia)
            //                  .ToSyntaxTriviaList()
            //                  .Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, _tabSpace));
            //    //endingWhiteSpace = addingTo.GetTrailingTrivia().Where(t =>
            //    //    t.Kind() == SyntaxKind.WhitespaceTrivia).ToSyntaxTriviaList();
            //}
            //return leadingWhiteSpace;
        }



    }
}
