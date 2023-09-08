using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SLControllers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SLControllersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SLControllers";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            Debug.WriteLine("MAIK HERE");
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            //var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            //// Find just those named type symbols with names containing lowercase letters.
            //if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            //{
            //    // For all such symbols, produce a diagnostic.
            //    var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

            //    context.ReportDiagnostic(diagnostic);
            //}

            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Check if the class is derived from class B
            if (namedTypeSymbol.BaseType != null && namedTypeSymbol.BaseType.Name == "SLControllerBase")
            {
                foreach (var member in namedTypeSymbol.GetMembers())
                {
                    if (member is IPropertySymbol || member is IMethodSymbol)
                    {
                        var references = member.DeclaringSyntaxReferences;

                        foreach (var reference in references)
                        {
                            var syntaxNode = reference.GetSyntax();
                            var containingClass = syntaxNode.FirstAncestorOrSelf<ClassDeclarationSyntax>();

                            // Check if the member is using a class derived from class A
                            if (containingClass != null && containingClass.Identifier.ValueText != namedTypeSymbol.Name)
                            {
                                var semanticModel = context.Compilation.GetSemanticModel(syntaxNode.SyntaxTree);
                                var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);

                                if (symbolInfo.Symbol != null && symbolInfo.Symbol.ContainingType != null && symbolInfo.Symbol.ContainingType.BaseType != null &&
                                    symbolInfo.Symbol.ContainingType.BaseType.Name == "ControllerBase")
                                {
                                    var diagnostic = Diagnostic.Create(Rule, syntaxNode.GetLocation());
                                    context.ReportDiagnostic(diagnostic);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation.Type);

            if (symbolInfo.Symbol is INamedTypeSymbol namedTypeSymbol)
            {
                // Check if the class is derived from class A
                if (namedTypeSymbol.BaseType != null && namedTypeSymbol.BaseType.Name == "ControllerBase")
                {
                    var containingClass = objectCreation.FirstAncestorOrSelf<ClassDeclarationSyntax>();

                    // Check if the class is instantiated within a class derived from class B
                    if (containingClass != null && containingClass.BaseList != null)
                    {
                        foreach (var baseType in containingClass.BaseList.Types)
                        {
                            var baseTypeName = baseType.Type.ToString();
                            if (baseTypeName == "SLControllerBase" || baseType.Type is IdentifierNameSyntax identifier && identifier.Identifier.Text == "SLControllerBase")
                            {
                                var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation());
                                context.ReportDiagnostic(diagnostic);
                                break; // No need to check further if already diagnosed.
                            }
                        }
                    }
                }
            }
        }
    }
}
