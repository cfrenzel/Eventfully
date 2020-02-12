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
                    createChangedDocument:c => AddMissingMapIdFors(context.Document, declaration, unmappedMessageType, c),
                    equivalenceKey: title),
                diagnostic);
            return;
        }
        private async Task<Document> AddMissingMapIdFors(Document document, ClassDeclarationSyntax classExpr, string messageType, CancellationToken cancellationToken)
        {

            var constructorExpr = classExpr.ChildNodes().SingleOrDefault(x => x is ConstructorDeclarationSyntax);
            if (constructorExpr == null)
                return document;

            ExpressionSyntax es = SyntaxFactory.ParseExpression($"this.MapIdFor<{messageType}>((m, md) => m.Id);")
                 .WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n"));
            var statement = SyntaxFactory.ExpressionStatement(es);

            var toappend = statement.DescendantNodesAndSelf();
            var oldblock = constructorExpr.ChildNodes().OfType<BlockSyntax>().First();
            var newblock = oldblock.AddStatements(statement);

            //var newConstructorExpr = constructorExpr.InsertNodesAfter(appendto, toappend  );
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(oldblock, newblock);
            return document.WithSyntaxRoot(newRoot);
        }

        //private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        //{
        //    // Compute new uppercase name.
        //    var identifierToken = typeDecl.Identifier;
        //    var newName = identifierToken.Text.ToUpperInvariant();

        //    // Get the symbol representing the type to be renamed.
        //    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        //    var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

        //    // Produce a new solution that has all references to that type renamed, including the declaration.
        //    var originalSolution = document.Project.Solution;
        //    var optionSet = originalSolution.Workspace.Options;
        //    var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

        //    // Return the new solution with the now-uppercase type name.
        //    return newSolution;
        //}
    }
}
