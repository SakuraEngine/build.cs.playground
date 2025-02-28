using Microsoft.Extensions.FileSystemGlobbing;
using SB.Core;
using System.Runtime.CompilerServices;

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

        internal void Resolve()
        {
            var GlobMatcher = new Matcher();
            GlobMatcher.AddIncludePatterns(Globs);
            Absolutes.AddRange(GlobMatcher.GetResultsInFullPath(Directory));
        }

        public IReadOnlySet<string> AllFiles => Absolutes;

        public string Name { get; }
        public string Location { get; }
        public string Directory { get; }
        private SortedSet<string> Globs = new();
        private SortedSet<string> Absolutes = new();
    }
}
