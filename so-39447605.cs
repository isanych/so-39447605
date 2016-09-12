using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Linq;

namespace so39447605
{
    internal class Walker : CSharpSyntaxWalker
    {
        public SemanticModel Model { get; set; }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var operation = (IAssignmentExpression)Model.GetOperation(node);
            if (operation.Value.Kind == OperationKind.InvocationExpression)
            {
                var invocation = (IInvocationExpression)operation.Value;
                foreach (
                    var syntax in
                    invocation.TargetMethod.DeclaringSyntaxReferences.Select(
                        x => (MethodDeclarationSyntax)x.GetSyntax()))
                {
                    var e = (TypeOfExpressionSyntax)syntax.ExpressionBody.Expression;
                    Console.WriteLine($"Generic type {invocation.TargetMethod.TypeParameters.First()} specialized with {invocation.TargetMethod.TypeArguments.First()}");
                    // How to get specialized body here? Currently it is just generic TypeParameters
                    var symbol = Model.GetSymbolInfo(e.Type).Symbol;
                    var typeofOperation = (ITypeOfExpression)Model.GetOperation(e);
                    Console.WriteLine($"{syntax.Identifier.Text} {symbol} {typeofOperation.TypeOperand}");
                }
            }
            base.VisitAssignmentExpression(node);
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
public class Program
{
    static Type TypeOf<T>() => typeof(T);

    public static void Main()
    {
        Type t;
        t = TypeOf<string>();
    }
}");
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create(null, new[] { tree }, new[] { mscorlib });
            var walker = new Walker { Model = compilation.GetSemanticModel(tree) };
            walker.Visit(tree.GetRoot());
        }
    }
}
