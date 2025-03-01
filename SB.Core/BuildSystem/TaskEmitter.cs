using System.Collections.Generic;

namespace SB
{
    public enum DependencyModel
    {
        PerTarget,
        PerFile,
        ExternalTarget
    }

    public abstract class TaskEmitter
    { 
        public TaskEmitter AddDependency(string EmitterName, DependencyModel Model)
        {
            Dependencies.Add(new KeyValuePair<string, DependencyModel>(EmitterName, Model));
            return this;
        }

        public abstract object? PerTargetTask(Target target);
        public abstract bool FileFilter(string File);
        public abstract object? PerFileTask(Target target, string File);

        public string Name => SelfName;

        internal string SelfName = "";
        internal HashSet<KeyValuePair<string, DependencyModel>> Dependencies { get; } = new();
    }
}
