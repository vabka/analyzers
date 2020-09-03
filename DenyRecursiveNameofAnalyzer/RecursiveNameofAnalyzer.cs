using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DenyRecursiveNameofAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RecursiveNameofAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                RecursiveNameofInField,
                RecursiveNameofInPropertyDeclaration
            );

        public override void Initialize(AnalysisContext context)
        {
            RegisterRecursiveNameofInFieldDeclaration(context);
        }

        private static void RegisterRecursiveNameofInFieldDeclaration(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ctx =>
            {
                var isRecursiveNameofField =
                    ctx.Node is FieldDeclarationSyntax {
                        Declaration: { Variables: { Count: 1} vars }
                        } &&
                    vars[0] is
                        {
                        Identifier: { Text: var fieldIdentifier },
                        Initializer: {
                        Value: InvocationExpressionSyntax {
                        Expression: IdentifierNameSyntax {
                        Identifier: { Text: "nameof" }
                        },
                        ArgumentList: {
                        Arguments: { Count: 1 } args
                        },
                        Span: var nameofInvocationSpan
                        }
                        }
                        } &&
                    args[0].Expression is IdentifierNameSyntax {
                        Identifier: { Text: var nameofText}
                        } &&
                    nameofText == fieldIdentifier;
                if (isRecursiveNameofField) //TODO add possibility to ignore
                {
                    var diagnosticLocation = Location.Create(ctx.Node.SyntaxTree, nameofInvocationSpan);
                    var diagnostic = Diagnostic.Create(RecursiveNameofInField, diagnosticLocation);
                    ctx.ReportDiagnostic(diagnostic);
                }
            }, SyntaxKind.FieldDeclaration);
        }

        private const string RecursiveNameofIsNotSafeForRefactorings =
            "Recursive nameof can vent to issues during rename refactoring";

        private static readonly DiagnosticDescriptor RecursiveNameofInField =
            new DiagnosticDescriptor(
                DiagnosticIds.RecursiveNameofInFieldDeclarationRuleId,
                "Recursive nameof in field declaration",
                "Field '{0}' references to it's own name",
                DiagnosticCategories.Nameof,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: RecursiveNameofIsNotSafeForRefactorings);

        private static readonly DiagnosticDescriptor RecursiveNameofInPropertyDeclaration =
            new DiagnosticDescriptor(
                DiagnosticIds.RecursiveNameofInPropertyDeclarationRuleId,
                "Recursive nameof in property declaration",
                "Property '{0}' references to it's own name",
                DiagnosticCategories.Nameof,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: RecursiveNameofIsNotSafeForRefactorings);
        
        
    }
}