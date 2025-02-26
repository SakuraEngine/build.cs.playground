using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace SB.Core
{
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
        public List<string> CalculateArguments()
        {
            List<string> Args = new List<string>();
            Args.Capacity = Arguments.Count + RawArguments.Count;
            var DriverType = GetType();
            foreach (var Method in DriverType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (Arguments.TryGetValue(Method.Name, out var ArgumentValue))
                {
                    var Result = Method.Invoke(this, ArgumentValue);
                    if (Result is string)
                        Args.Add(Result as string);
                    if (Result is string[])
                        Args = Args.Union(Result as string[]).ToList();
                    if (Result is List<string>)
                        Args = Args.Union(Result as List<string>).ToList();
                }
            }
            Args = Args.Union(RawArguments).ToList();
            Args.Remove("");
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

        public IArgumentDriver AddArgument(string key, object value)
        {
            object[] args = { value };
            Arguments.Add(key, args);
            return this;
        }

        public IArgumentDriver AddArgument(string key, object?[] value)
        {
            Arguments.Add(key, value);
            return this;
        }
        public void AddRawArgument(string Arg)
        {
            RawArguments.Add(Arg);
        }

        public Dictionary<string, object?[]?> Arguments { get; }
        public HashSet<string> RawArguments { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class Argument : Attribute
    {

    }
}