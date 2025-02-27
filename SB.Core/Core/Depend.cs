using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SB.Core
{
    public struct Depend
    {
        [JsonInclude]
        private readonly ImmutableSortedDictionary<string, DateTime>? InputFiles { get; init; }
        [JsonInclude]
        private readonly List<string>? InputArgs { get; init; }
        [JsonInclude]
        private ImmutableSortedDictionary<string, DateTime>? ExternalDeps { get; set; }

        [JsonIgnore]
        public readonly List<string> ExternalFiles { get; } = new();


        public struct Options
        {
            public bool UseSHA { get; init; }
            public bool Force { get; init; }
        }

        public Depend() { }

        public static void OnChanged(string DepFile, Action<Depend> func, IEnumerable<string> Files, IEnumerable<string> Args, Options? opt = null, [CallerFilePath] string CallerLoc = null)
        {
            Options option = opt ?? new Options { Force = false, UseSHA = false };
            if (!Path.IsPathFullyQualified(DepFile))
            {
                var CallerPath = Path.GetDirectoryName(Path.GetFullPath(CallerLoc));
                DepFile = Path.Combine(CallerPath, DepFile);
            }

            var SortedFiles = Files?.ToList() ?? new(); SortedFiles.Sort();
            var SortedArgs = Args?.ToList() ?? new(); SortedArgs.Sort();
            var check = () => {
                if (Path.Exists(DepFile))
                {
                    var Deps = Json.Deserialize<Depend>(File.ReadAllText(DepFile));
                    // check file list change
                    if (!SortedFiles.SequenceEqual(Deps.InputFiles.Keys))
                        return false;
                    // check arg list change
                    if (!SortedArgs.SequenceEqual(Deps.InputArgs))
                        return false;
                    // check input file mtime change
                    foreach (var File in Deps.InputFiles)
                    {
                        if (!Path.Exists(File.Key)) // deleted
                            return false;
                        if (File.Value != Directory.GetLastWriteTimeUtc(File.Key)) // modified
                            return false;
                    }
                    // check output file mtime change
                    foreach (var File in Deps.ExternalDeps)
                    {
                        if (!Path.Exists(File.Key)) // deleted
                            return false;
                        if (File.Value != Directory.GetLastWriteTimeUtc(File.Key)) // modified
                            return false;
                    }
                    return true;
                }
                return false;
            };

            var update = (Depend depFile) =>
            {
                depFile.ExternalFiles.Sort();
                depFile.ExternalDeps = depFile.ExternalFiles.Select(x => new KeyValuePair<string, DateTime>(x, Directory.GetLastWriteTimeUtc(x))).ToImmutableSortedDictionary();
                File.WriteAllText(DepFile, Json.Serialize(depFile));
            };

            if (!check())
            {
                Depend depFile = new Depend
                {
                    InputFiles = SortedFiles.Select(x => new KeyValuePair<string, DateTime>(x, Directory.GetLastWriteTimeUtc(x))).ToImmutableSortedDictionary(),
                    InputArgs = SortedArgs
                };
                func(depFile);
                update(depFile);
            }
        }
    }
}