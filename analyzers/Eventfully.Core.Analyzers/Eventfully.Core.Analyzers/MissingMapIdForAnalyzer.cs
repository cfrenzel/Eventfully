using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Eventfully.Core.Analyzers
{
 
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingMapIdForAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MissingMapIdForAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            //// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            //// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);

            //context.RegisterCompilationStartAction(compilationContext =>
            //{
            //    // Search Meziantou.SampleType
            //    var typeSymbol = compilationContext.Compilation.GetTypeByMetadataName("Eventfully.ProcessManagerMachine");
            //    if (typeSymbol == null)
            //        return;

            //    // register the analyzer on Method symbol
            //    compilationContext.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            //});
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);

        }

        private static List<string> _sagaClasses = new List<string>() {
            "ProcessManagerMachine",
            "Saga"
        };
        private static List<string> _handlerIntefaces = new List<string>()
        {
            "ITriggeredBy",
            "IMachineMessageHandler",
            "IMessageHandler",
        };
        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
           
            var classExpr = (ClassDeclarationSyntax)context.Node;
            if (classExpr.BaseList == null)
                return;
            var genericBases = classExpr.BaseList.ChildNodes()
                .Where(x => x is SimpleBaseTypeSyntax)
                .Select(x => x.ChildNodes().FirstOrDefault(g => g is GenericNameSyntax))
                .Cast<GenericNameSyntax>();

            var sagaBases = genericBases.Where(x => _sagaClasses.Contains(x.Identifier.ValueText));
            if (sagaBases == null || sagaBases.Count() < 1)
                return;

            var handlerBases = genericBases.Where(x => _handlerIntefaces.Contains(x.Identifier.ValueText));
            if (handlerBases == null || handlerBases.Count() < 1)
                return;
            var interfaceMessageTypeMap = handlerBases
                .ToDictionary(key => (key.TypeArgumentList.Arguments.FirstOrDefault() as IdentifierNameSyntax)
                        ?.Identifier.ValueText,
                        value => value
                 );

            List<string> mappedMessageTypes = null;
            var constructorExpr = classExpr.ChildNodes().SingleOrDefault(x => x is ConstructorDeclarationSyntax);
            if(constructorExpr != null)
            {
                mappedMessageTypes = constructorExpr.DescendantNodes()
                   .OfType<GenericNameSyntax>()
                   .Where(x => x.Identifier.ValueText == "MapIdFor")
                   .Select(x => (x.TypeArgumentList.Arguments.FirstOrDefault() as IdentifierNameSyntax)
                       ?.Identifier.ValueText)
                    .ToList();
            }

            var missingMappings = interfaceMessageTypeMap.Where(x =>
            {
                return !mappedMessageTypes.Contains(x.Key);
            });
            foreach(var handlerPair in missingMappings)
            {
                ImmutableDictionary<string, string> properties = new Dictionary<string, string>() { { "UnmappedMessage", handlerPair.Key } }.ToImmutableDictionary();
                var diagn = Diagnostic.Create(Rule, handlerPair.Value.GetLocation(),
                        properties,
                        $"Missing this.MapIdFor<{handlerPair.Key}> in constructor"
               );
                context.ReportDiagnostic(diagn);
            }

         
        }

    }
}
