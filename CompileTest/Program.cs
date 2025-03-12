using SB;
using SB.Core;
using Serilog.Events;
using Serilog;
using System.Diagnostics;
using Serilog.Sinks.SystemConsole.Themes;

SystemConsoleTheme ConsoleLogTheme = new SystemConsoleTheme(
       new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>
       {
           [ConsoleThemeStyle.Text] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.White },
           [ConsoleThemeStyle.SecondaryText] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Gray },
           [ConsoleThemeStyle.TertiaryText] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.DarkGray },
           [ConsoleThemeStyle.Invalid] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Yellow },
           [ConsoleThemeStyle.Null] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Blue },
           [ConsoleThemeStyle.Name] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Gray },
           [ConsoleThemeStyle.String] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.White },
           [ConsoleThemeStyle.Number] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Green },
           [ConsoleThemeStyle.Boolean] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Blue },
           [ConsoleThemeStyle.Scalar] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Green },

           [ConsoleThemeStyle.LevelVerbose] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Gray },
           [ConsoleThemeStyle.LevelDebug] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Gray },
           [ConsoleThemeStyle.LevelInformation] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.White },
           [ConsoleThemeStyle.LevelWarning] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.Yellow },
           [ConsoleThemeStyle.LevelError] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.White, Background = ConsoleColor.Red },
           [ConsoleThemeStyle.LevelFatal] = new SystemConsoleThemeStyle { Foreground = ConsoleColor.White, Background = ConsoleColor.Red },
       });

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    // .Enrich.WithThreadId()
    // .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.ffff zzz} {Message:lj}{NewLine}{Exception}")
    .WriteTo.Async(a => a.Logger(l => l
        .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
        .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: "{Message:lj}{NewLine}{Exception}", theme: ConsoleLogTheme)
    ))
    .WriteTo.Async(a => a.Logger(l => l
        .Filter.ByExcluding(e => e.Level == LogEventLevel.Information)
        .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: "{Level:u}: {Message:lj}{NewLine}{Exception}", theme: ConsoleLogTheme)
    ))
    .CreateLogger();

var LibSourceFile = "D:/SakuraEngine/Simple CXX/lib.cpp";
var ExeSourceFile = "D:/SakuraEngine/Simple CXX/main.cpp";
var PakSourceFile = "D:/SakuraEngine/Simple CXX/package.cpp";
BuildSystem.TempPath = Directory.CreateDirectory("D:/SakuraEngine/Simple CXX/.sb").FullName;
BuildSystem.BuildPath = Directory.CreateDirectory("D:/SakuraEngine/Simple CXX/sbuild").FullName;
BuildSystem.PackageTempPath = Directory.CreateDirectory("D:/SakuraEngine/Simple CXX/.pkgs/.sb").FullName;
BuildSystem.PackageBuildPath = Directory.CreateDirectory("D:/SakuraEngine/Simple CXX/.pkgs/sbuild").FullName;
VisualStudio VS = new VisualStudio(2022);
await VS.Initialize();

Stopwatch sw = new();
sw.Start();

BuildSystem.AddTaskEmitter("Cpp.Compile", new CppCompileEmitter(VS));
BuildSystem.AddTaskEmitter("Cpp.Link", new CppLinkEmitter(VS))
    .AddDependency("Cpp.Link", DependencyModel.ExternalTarget)
    .AddDependency("Cpp.Compile", DependencyModel.PerTarget);

var Target1 = BuildSystem.Package($"Hello10")
    .AddTarget("Hello", (Target Target, PackageConfig Config) =>
    {
        Target.TargetType(TargetType.Static)
           .CppVersion("20")
           .Exception(true)
           .RuntimeLibrary("MD")
           .Defines(Visibility.Public, $"A1=A1000")
           .Defines(Visibility.Public, $"B1=A1000")
           .Defines(Visibility.Public, $"C1=A1000")
           .AddFiles(PakSourceFile);
    });

for (int i = 0; i < 1000; i++)
{
    int Mod = i % 5;

    var Target2 = BuildSystem.Target($"Hello2{i}")
        .TargetType(TargetType.Static)
        .CppVersion("20")
        .Exception(true)
        .RuntimeLibrary("MD")
        .Defines(Visibility.Public, $"A0=A{i}")
        .Defines(Visibility.Public, $"B0=A{i}")
        .Defines(Visibility.Public, $"C0=A{i}")
        .Defines(Visibility.Public, $"D0=A{i}")
        .Defines(Visibility.Public, $"E0=A{i}")
        .Defines(Visibility.Public, $"F0=A{i}")
        .Defines(Visibility.Public, $"G0=A{i}")
        .Defines(Visibility.Public, $"H0=A{i}")
        .Defines(Visibility.Public, $"I0=A{i}")
        .Defines(Visibility.Public, $"J0=A{i}")
        .AddFiles(LibSourceFile);

    if (i > 0)
    {
        for (int j = 1; j < Mod; j++)
            Target2.Depend($"Hello2{i - j}");
    }
}

for (int i = 0; i < 1000; i++)
{
    if ((i % 5) == 0)
    {
        var EXE = BuildSystem.Target($"World{i / 5}")
            .TargetType(TargetType.Executable)
            .RuntimeLibrary("MD")
            .AddFiles(ExeSourceFile);
        EXE.Require($"Hello10", new PackageConfig { Version = new Version(1, 1, 0) });
        EXE.Depend($"Hello10@Hello");
        EXE.Depend($"Hello2{i}");
    }
}

BuildSystem.RunBuild();

/*
struct InstallCtx
{
    Version Version;
    // configs...
}

Target("")
    .Require("Zlib", Config)
    .Depend("Zlib", "Zlib")
    .Depend("Zlib", "Zlib2")

BuildSystem.AddPackage("Zlib")
    .AddTarget("Zlib", InstallCtx => Target...)
    .AddTarget("Zlib2", InstallCtx => Target...)
    ...;
*/

sw.Stop();
Log.CloseAndFlush();
Console.WriteLine($"Total: {sw.ElapsedMilliseconds}");
Console.WriteLine($"Compile Total: {CppCompileEmitter.Time}");
Console.WriteLine($"Link Total: {CppLinkEmitter.Time}");

public class CppCompileEmitter : TaskEmitter
{
    public CppCompileEmitter(IToolchain Toolchain)
    {
        this.Toolchain = Toolchain;
    }
    public override bool EmitFileTask => true;

    public override bool FileFilter(string File) => true;

    public override IArtifact PerFileTask(Target Target, string SourceFile)
    {
        Stopwatch sw = new();
        sw.Start();

        var DependFile = Path.Combine(Target.GetStorePath(BuildSystem.DepsStore), BuildSystem.GetUniqueTempFileName(SourceFile, Target.Name + this.Name, "task.deps.json"));
        var SourceDependencies = Path.Combine(Target.GetStorePath(BuildSystem.DepsStore), BuildSystem.GetUniqueTempFileName(SourceFile, Target.Name + this.Name, "source.deps.json"));
        var ObjectFile = GetObjectFilePath(Target, SourceFile);

        var CLDriver = (new CLArgumentDriver() as IArgumentDriver)
            .AddArguments(Target.Arguments)
            .AddArgument("Source", SourceFile)
            .AddArgument("Object", ObjectFile)
            .AddArgument("SourceDependencies", SourceDependencies)
            .AddArgument("DependFile", DependFile);
        
        var R = Toolchain.Compiler.Compile(CLDriver);
        sw.Stop();
        Time += (int)sw.ElapsedMilliseconds;
        return R;
    }

    public static string GetObjectFilePath(Target Target, string SourceFile) => Path.Combine(Target.GetStorePath(BuildSystem.ObjsStore), BuildSystem.GetUniqueTempFileName(SourceFile, Target.Name, "obj"));

    private IToolchain Toolchain { get; }
    public static volatile int Time = 0;
}

public class CppLinkEmitter : TaskEmitter
{
    public CppLinkEmitter(IToolchain Toolchain) => this.Toolchain = Toolchain;
    public override bool EmitTargetTask => true;
    public override IArtifact PerTargetTask(Target Target)
    {
        Stopwatch sw = new();
        sw.Start();

        var LinkedFileName = GetLinkedFileName(Target);
        var DependFile = Path.Combine(Target.GetStorePath(BuildSystem.DepsStore), BuildSystem.GetUniqueTempFileName(LinkedFileName, Target.Name + this.Name, "task.deps.json"));

        var Inputs = new ArgumentList<string>();
        // add obj files
        Inputs.AddRange(Target.AllFiles.Select(SourceFile => CppCompileEmitter.GetObjectFilePath(Target, SourceFile)));
        // add dep links
        Inputs.AddRange(Target.Dependencies.Select(T => GetLinkedFileName(BuildSystem.GetTarget(T))));

        var LINKDriver = (new LINKArgumentDriver() as IArgumentDriver)
            .AddArguments(Target.Arguments)
            .AddArgument("Inputs", Inputs)
            .AddArgument("Output", LinkedFileName)
            .AddArgument("DependFile", DependFile);
        var R = Toolchain.Linker.Link(LINKDriver);

        sw.Stop();
        Time += (int)sw.ElapsedMilliseconds;
        return R;
    }

    private static string GetLinkedFileName(Target Target)
    {
        var OutputType = (TargetType)Target.Arguments["TargetType"];
        var Extension = (OutputType == TargetType.Static) ? "lib" :
                        (OutputType == TargetType.Dynamic) ? "dll" :
                        (OutputType == TargetType.Executable) ? "exe" : "unknown";
        var OutputFile = Path.Combine(Target.GetBuildPath(), $"{Target.Name}.{Extension}");
        return OutputFile;
    }

    private IToolchain Toolchain { get; }
    public static volatile int Time = 0;
}