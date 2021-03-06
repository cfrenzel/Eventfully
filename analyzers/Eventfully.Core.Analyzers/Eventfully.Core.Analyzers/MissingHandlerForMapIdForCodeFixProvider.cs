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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingHandlerForMapIdForCodeFixProvider)), Shared]
    public class MissingHandlerForMapIdForCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add Handler<> for MessageType";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MissingMapIdForAnalyzer.DiagnosticId2); }
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
            if (!diagnostic.Properties.TryGetValue("UnhandledMessage", out unmappedMessageType))
                return;  
            //diagnostic.Properties.TryGetValue("UnhandledMessage", out unmappedMessageType);

            context.RegisterCodeFix(
             CodeAction.Create(
                title: title,
                createChangedDocument: c => AddMissingHandler(context.Document, declaration, unmappedMessageType, c),
                equivalenceKey: title),
                diagnostic
            );
            
            return;
        }

        private string MachineClassName = "ProcessManagerMachine";
        private string SagaClassName = "Saga";

        private async Task<Document> AddMissingHandler(Document document, ClassDeclarationSyntax classExpr, string messageType, CancellationToken cancellationToken)
        {
            if (classExpr.BaseList == null)
                return document;
            var genericBases = classExpr.BaseList.ChildNodes()
                .Where(x => x is SimpleBaseTypeSyntax)
                .Select(x => x.ChildNodes().FirstOrDefault(g => g is GenericNameSyntax))
                .Where(x => x != null)
                .Cast<GenericNameSyntax>();
            
            if (!genericBases.Any())
                return document;

            SyntaxTriviaList trailingWhiteSpace = new SyntaxTriviaList().Add(SyntaxFactory.EndOfLine(Environment.NewLine));
            SimpleBaseTypeSyntax newInterface = null;
            
            if (genericBases.Any(x => x.Identifier.ValueText.Equals(MachineClassName)))
                newInterface = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"IMachineMessageHandler<{messageType}>")).WithTrailingTrivia(trailingWhiteSpace);
            else if (genericBases.Any(x => x.Identifier.ValueText.Equals(SagaClassName)))
                newInterface = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"IMessageHandler<{messageType}>")).WithTrailingTrivia(trailingWhiteSpace); 
            else
                return document;
           
            ClassDeclarationSyntax newClassExpr = classExpr.AddBaseListTypes(newInterface);

            var lastComma = newClassExpr.BaseList.DescendantTokens().LastOrDefault(x => x.IsKind(SyntaxKind.CommaToken));
            if (lastComma != null)
                newClassExpr = newClassExpr.ReplaceToken(lastComma, lastComma.WithTrailingTrivia(trailingWhiteSpace));
         
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(classExpr, newClassExpr);
            return document.WithSyntaxRoot(newRoot);
        }

      
    }
}
