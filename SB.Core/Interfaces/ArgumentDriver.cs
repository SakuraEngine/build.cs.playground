using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SB.Core
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Argument : Attribute
    {

    };

    public enum Architecture
    {
        X86,
        X64,
        ARM64
    };

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
                            Arguments.Add(Method.Invoke(this, SemanticValue) as string);
                        }
                        catch (ArgumentException e)
                        {
                            continue;
                        }
                    }
                }
            }
            Arguments.Union(RawArguments);
            return Arguments;
        }

        public Dictionary<string, object?[]?> Semantics { get; }
        public HashSet<string> RawArguments { get; }
    }
}
