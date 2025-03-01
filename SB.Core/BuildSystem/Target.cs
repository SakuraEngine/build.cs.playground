using Microsoft.Extensions.FileSystemGlobbing;
using SB.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SB
{
    public class Target : TargetSetters
    {
        internal Target(string Name, [CallerFilePath] string Location = null)
        {
            this.Name = Name;
            this.Location = Location;
            this.Directory = Path.GetDirectoryName(Location);
        }

        public Target AddFiles(params string[] files)
        {
            foreach (var file in files)
            {
                if (file.Contains("*"))
                    Globs.Add(file);
                else if (Path.IsPathFullyQualified(file))
                    Absolutes.Add(file);
                else
                    Absolutes.Add(Path.Combine(Directory, file));
            }
            return this;
        }

        public Target Depend(string TargetName)
        {
            TargetDependencies.Add(TargetName);
            return this;
        }

        internal void Resolve()
        {
            // Files
            if (Globs.Count != 0)
            {
                var GlobMatcher = new Matcher();
                GlobMatcher.AddIncludePatterns(Globs);
                Absolutes.AddRange(GlobMatcher.GetResultsInFullPath(Directory));
            }
            // Arguments
            MergeArguments(FinalArguments, PublicArguments);
            MergeArguments(FinalArguments, PrivateArguments);
            RecursiveMergeDependencies(FinalArguments, Dependencies);
        }

        internal void RecursiveMergeDependencies(Dictionary<string, object?> To, IReadOnlySet<string> DepNames)
        {
            foreach (var DepName in DepNames)
            {
                Target DepTarget = BuildSystem.GetTarget(DepName);
                MergeArguments(To, DepTarget.PublicArguments);
                MergeArguments(To, DepTarget.InterfaceArguments);
                RecursiveMergeDependencies(To, DepTarget.Dependencies);
            }
        }

        internal void MergeArguments(Dictionary<string, object?> To, Dictionary<string, object?> From)
        {
            foreach (var KVP in From)
            {
                var K = KVP.Key;
                var V = KVP.Value;
                if (V is IArgumentList)
                {
                    var ArgList = V as IArgumentList;
                    if (To.TryGetValue(K, out var Existed))
                        (Existed as IArgumentList).Merge(ArgList);
                    else
                        To.Add(K, ArgList);
                }
                else
                {
                    if (To.TryGetValue(K, out var Existed))
                        throw new ArgumentException("MergeArguments: This is a unique argument, we should never merge it!");
                    To.Add(K, V);
                }
            }
        }

        public IReadOnlySet<string> Dependencies => TargetDependencies;
        public IReadOnlySet<string> AllFiles => Absolutes;

        public string Name { get; }
        public string Location { get; }
        public string Directory { get; }
        private SortedSet<string> Globs = new();
        private SortedSet<string> Absolutes = new();
        private SortedSet<string> TargetDependencies = new();
    }
}
