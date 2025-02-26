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
                    var Result = Method.Invoke(this, ArgumentValue);
                    if (Result is string)
                        Args.Add(Method.Name, new string[] { Result as string });
                    if (Result is string[])
                        Args.Add(Method.Name, Result as string[]);
                    if (Result is List<string>)
                        Args.Add(Method.Name, (Result as List<string>).ToArray());
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
            JsonSerializerOptions opts = new System.Text.Json.JsonSerializerOptions();
            opts.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            return JsonSerializer.Serialize(compile_commands, opts);
        }

        public IArgumentDriver AddArgument(ArgumentName key, object value)
        {
            object[] args = { value };
            Arguments.Add(key, args);
            return this;
        }

        public IArgumentDriver AddArguments(ArgumentName key, object?[] value)
        {
            Arguments.Add(key, value);
            return this;
        }
        public void AddRawArgument(string Arg)
        {
            RawArguments.Add(Arg);
        }

        public Dictionary<ArgumentName, object?[]?> Arguments { get; }
        public HashSet<string> RawArguments { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class Argument : Attribute
    {

    }
}