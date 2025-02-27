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
        public static bool GetUnderlyingTypeIfIsICollection(this ITypeSymbol Symbol, ref ITypeSymbol ElementType)
        {
            bool isCollection = false;
            foreach (var Interface in Symbol.AllInterfaces)
                if (Interface.MetadataName.Equals("ICollection`1"))
                {
                    ElementType = Interface.TypeArguments[0];
                    isCollection = true;
                    break;
                }
            return isCollection;
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
                return Methods;
            });
            
            // generate a class that contains their values as const strings
            initContext.RegisterSourceOutput(MethodsProvider, (spc, Methods) =>
            {
                StringBuilder sourceBuilder = new StringBuilder(@"
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
                    // Normal Method
                    {
                        sourceBuilder.Append($@"
         // SB.Target {MethodName}({Param.Type.GetFullTypeName()} {Param.Name});");
                    }
                    // ICollection Method
                    {
                        ITypeSymbol ElementType = null;
                        sourceBuilder.Append("\n// " + String.Join("", Param.Type.AllInterfaces.Select(x => x.IsGenericType)));
                        if (Param.Type.GetUnderlyingTypeIfIsICollection(ref ElementType))
                        {
                            sourceBuilder.Append($@"
        // SB.Target {MethodName}({ElementType?.GetFullTypeName()} {Param.Name});");
                        }
                    }
                }

                sourceBuilder.Append(@"
        public Dictionary<string, object?> Arguments { get; }
    }
}");
                spc.AddSource("TargetSetters.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
            });
        }
    }
}