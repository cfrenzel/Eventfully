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
        public const string DiagnosticId2 = "MissingHandlerForMapIdFor";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor MissingMapRule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static DiagnosticDescriptor MissingHandlerRule = new DiagnosticDescriptor(DiagnosticId2, "Missing Handler for Mapped Event", "{0}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(MissingMapRule, MissingHandlerRule); } }

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
            //    context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            //});
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);

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
                .Where(x=> x != null)
                .Cast<GenericNameSyntax>();
            if (!genericBases.Any())
                return;

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

            Dictionary<string, GenericNameSyntax> mappedMessageInterfaceMap = null;
            var constructorExpr = classExpr.ChildNodes().SingleOrDefault(x => x is ConstructorDeclarationSyntax);
            if(constructorExpr != null)
            {
                mappedMessageInterfaceMap = constructorExpr.DescendantNodes()
                   .OfType<GenericNameSyntax>()
                   .Where(x => x.Identifier.ValueText == "MapIdFor")
                   .ToDictionary(x => (x.TypeArgumentList.Arguments.FirstOrDefault() as IdentifierNameSyntax)
                       ?.Identifier.ValueText,
                       y => y
                    );
                   //.Select(x => (x.TypeArgumentList.Arguments.FirstOrDefault() as IdentifierNameSyntax)
                   //    ?.Identifier.ValueText)
                    
            }

            var missingMappings = interfaceMessageTypeMap.Where(x =>
            {
                return !mappedMessageInterfaceMap.ContainsKey(x.Key);
            });
            foreach (var handlerPair in missingMappings)
            {
                ImmutableDictionary<string, string> properties = new Dictionary<string, string>() { { "UnmappedMessage", handlerPair.Key } }.ToImmutableDictionary();
                var diagn = Diagnostic.Create(MissingMapRule, handlerPair.Value.GetLocation(),
                        properties,
                        $"Missing this.MapIdFor<{handlerPair.Key}> in constructor"
               );
                context.ReportDiagnostic(diagn);
            }

            var missingInterfaces = mappedMessageInterfaceMap.Where(x =>
            {
                return !interfaceMessageTypeMap.ContainsKey(x.Key);
            });

            foreach (var mappingPair in missingInterfaces)
            {
                ImmutableDictionary<string, string> properties = new Dictionary<string, string>() { { "UnhandledMessage", mappingPair.Key } }.ToImmutableDictionary();
                var diagn = Diagnostic.Create(MissingHandlerRule, mappingPair.Value.GetLocation(),
                        properties,
                        $"Missing Handler<{mappingPair.Key}> interface"
               );
                context.ReportDiagnostic(diagn);
            }
        }

    }
}
