using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace SB.Core
{
    using ArgumentName = string;

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

    public enum TargetType
    {
        Static,
        Dynamic,
        Executable
    }

    public interface IArgumentDriver
    {
        public Dictionary<ArgumentName, string[]> CalculateArguments()
        {
            Dictionary<ArgumentName, string[]> Args = new();
            var DriverType = GetType();
            foreach (var Method in DriverType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (Arguments.TryGetValue(Method.Name, out var ArgumentValue))
                {
                    var Result = Method.Invoke(this, new object[] { ArgumentValue });
                    if (Result is string)
                        Args.Add(Method.Name, new string[] { Result as string });
                    if (Result is string[])
                        Args.Add(Method.Name, Result as string[]);
                    else if (Result is IEnumerable<string>)
                        Args.Add(Method.Name, (Result as IEnumerable<string>).ToArray());
                }
            }
            Args.Add("RAW", RawArguments.ToArray());
            return Args;
        }

        public string CompileCommands(string directory)
        {
            dynamic compile_commands = new
            {
                directory = directory,
                arguments = CalculateArguments(),
                file = Arguments["Source"]
            };
            return Json.Serialize(compile_commands);
        }

        public IArgumentDriver AddArgument(ArgumentName key, object value)
        {
            Arguments.Add(key, value);
            return this;
        }
        
        public IArgumentDriver AddArguments(IDictionary<ArgumentName, object>? Args)
        {
            Arguments.AddRange(Args);
            return this;
        }

        public IArgumentDriver AddRawArgument(string Arg)
        {
            RawArguments.Add(Arg);
            return this;
        }

        public Dictionary<ArgumentName, object?> Arguments { get; }
        public HashSet<string> RawArguments { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TargetSetter : Attribute
    {

    }
}