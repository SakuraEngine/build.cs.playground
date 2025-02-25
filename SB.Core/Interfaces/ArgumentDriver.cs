using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SB.Core
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Argument : Attribute
    {

    }

    public enum Architecture
    {
        X86,
        X64,
        ARM64
    }

    public enum SIMDArchitecture
    {
        SSE2,
        SSE4_2,
        AVX,
        AVX512,
        AVX10_1
    }

    public enum PDBMode
    {
        Disable,
        Embed,
        Standalone
    }

    public enum OptimizationLevel
    {
        O0,
        O1,
        O2,
        O3
    }

    public enum FpModel
    {
        Precise,
        Fast,
        Strict
    }

    public enum MSVCWarningLevel
    {
        W0,
        W1,
        W2,
        W3,
        W4,
        Wall
    }

    public interface IArgumentDriver
    {
        public void AddArgument(string Arg)
        {
            RawArguments.Add(Arg);
        }

        public List<string> CalculateArguments()
        {
            List<string> Arguments = new List<string>();
            Arguments.Capacity = Semantics.Count + RawArguments.Count;
            var DriverType = GetType();
            foreach (var Method in DriverType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (Attribute.GetCustomAttribute(Method, typeof(Argument)) is not null)
                {
                    if (Semantics.TryGetValue(Method.Name, out var SemanticValue))
                    {
                        try
                        {
                            var Result = Method.Invoke(this, SemanticValue);
                            if (Result is string)
                                Arguments.Add(Result as string);
                            if (Result is string[])
                                Arguments = Arguments.Union(Result as string[]).ToList();
                            if (Result is List<string>)
                                Arguments = Arguments.Union(Result as List<string>).ToList();
                        }
                        catch (ArgumentException e)
                        {
                            continue;
                        }
                    }
                }
            }
            Arguments = Arguments.Union(RawArguments).ToList();
            return Arguments;
        }

        public Dictionary<string, object?[]?> Semantics { get; }
        public HashSet<string> RawArguments { get; }
    }
}