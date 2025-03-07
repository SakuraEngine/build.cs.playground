using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.CommandLine;

var RootDirectory = Directory.GetCurrentDirectory();
var RootBuildScriptPath = Path.Combine(RootDirectory, "build.csx");
RootCommand rootCommand = new RootCommand("Script CLI for SB.");

if (!File.Exists(RootBuildScriptPath))
{
    Console.WriteLine("Error: ./build.csx does not exist! Dont know which script to run.");
    return;
}

var Options = ScriptOptions.Default     
    .WithFilePath(RootBuildScriptPath);
foreach (var Assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    Options = Options.AddReferences(Assembly);
}

var Script = CSharpScript.Create(
    File.Open(RootBuildScriptPath, FileMode.Open),
    Options
);
await Script.RunAsync();

public static class ImportHelper
{
    public static SB.Target PlaceHolder;
}