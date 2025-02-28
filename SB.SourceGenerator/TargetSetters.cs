using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using System.Data;

namespace SB.Generators
{
    public static class TypeHelper
    {
        public static string GetFullTypeName(this ITypeSymbol Symbol) => Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        public static bool GetUnderlyingTypeIfIsArgumentList(this ITypeSymbol Symbol, ref ITypeSymbol ElementType)
        {
            if (Symbol is INamedTypeSymbol)
                if (Symbol.MetadataName.Equals("ArgumentList`1"))
                {
                    ElementType = (Symbol as INamedTypeSymbol).TypeArguments[0];
                    return true;
                }
            return false;
        }
    }

    public struct MethodSignatureComparer : IEqualityComparer<IMethodSymbol>
    {
        public bool Equals(IMethodSymbol X, IMethodSymbol Y)
        {
            return X.Name == Y.Name;
        }

        public int GetHashCode(IMethodSymbol X)
        {
            return X.Name.GetHashCode();
        }
    }

    [Generator]
    public class TargetSetterGenerator : IIncrementalGenerator
    {
        
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            // define the execution pipeline here via a series of transformations:

            // find all additional files that end with .txt
            IncrementalValueProvider<Compilation> AsyncCompile = initContext.CompilationProvider;
            var MethodsProvider = AsyncCompile.Select((Compile, Cancel) =>
            {
                List<IMethodSymbol> Methods = new List<IMethodSymbol>();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var CL = Compile.GetTypeByMetadataName("SB.Core.CLArgumentDriver");
                    var LINK = Compile.GetTypeByMetadataName("SB.Core.LINKArgumentDriver");
                    var AllMembers = CL.GetMembers().Concat(LINK.GetMembers());
                    foreach (var Method in AllMembers.Where(M => M.Kind == SymbolKind.Method))
                    {
                        if (Method.GetAttributes().Any(A => A.AttributeClass.GetFullTypeName().Equals("global::SB.Core.TargetSetter")))
                            Methods.Add(Method as IMethodSymbol);
                    }
                }
                return Methods.Distinct(new MethodSignatureComparer());
            });
            
            // generate a class that contains their values as const strings
            initContext.RegisterSourceOutput(MethodsProvider, (spc, Methods) =>
            {
                StringBuilder sourceBuilder = new StringBuilder(@"
using SB.Core;

namespace SB
{    
    public abstract class TargetSetters
    {
");

                foreach (var Method in Methods)
                {
                    // Method.ReturnType.Is
                    var Param = Method.Parameters[0];
                    var MethodName = Method.Name;
                    {
                        ITypeSymbol ElementType = null;
                        if (Param.Type.GetUnderlyingTypeIfIsArgumentList(ref ElementType))
                        {
                            sourceBuilder.Append($@"
        public SB.Target {MethodName}(params {ElementType.GetFullTypeName()}[] {Param.Name}) {{ Arguments.GetOrAddNew<string, {Param.Type}>(""{MethodName}"").AddRange({Param.Name}); return this as SB.Target; }}
");
                        }
                        else
                        {
                            sourceBuilder.Append($@"
        public SB.Target {MethodName}({Param.Type.GetFullTypeName()} {Param.Name}) {{ Arguments.Override(""{MethodName}"", {Param.Name}); return this as SB.Target; }}
");
                        }
                    }
                }

                sourceBuilder.Append(@"
        public Dictionary<string, object?> Arguments { get; } = new();
    }
}");
                spc.AddSource("TargetSetters.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
            });
        }
    }
}