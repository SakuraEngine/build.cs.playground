using SB;
using SB.Core;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SB
{
    public static partial class BuildSystem
    {
        public static Target Target(string Name, [CallerFilePath] string Location = null)
        {
            if (AllTargets.TryGetValue(Name, out var _))
                throw new ArgumentException($"Target with name {Name} already exists! Name should be unique to every target!");

            var NewTarget = new Target(Name, Location);
            if (!AllTargets.TryAdd(Name, NewTarget))
            {
                throw new ArgumentException($"Failed to add target with name {Name}! Are you adding same targets in parallel?");
            }
            return NewTarget;
        }

        public static Package Package(string Name)
        {
            if (AllPackages.TryGetValue(Name, out var _))
                throw new ArgumentException($"Package with name {Name} already exists! Name should be unique to every package!");

            var NewPackage = new Package(Name);
            if (!AllPackages.TryAdd(Name, NewPackage))
            {
                throw new ArgumentException($"Failed to add package with name {Name}! Are you adding same packages in parallel?");
            }
            return NewPackage;
        }

        public static Target? GetTarget(string Name) => AllTargets.TryGetValue(Name, out var Found) ? Found : null;
        public static Package? GetPackage(string Name) => AllPackages.TryGetValue(Name, out var Found) ? Found : null;

        public static TaskEmitter AddTaskEmitter(string Name, TaskEmitter Emitter)
        {
            if (TaskEmitters.TryGetValue(Name, out var _))
                throw new ArgumentException($"Emitter with name {Name} already exists! Name should be unique to every emitter!");
            TaskEmitters.Add(Name, Emitter);
            Emitter.SelfName = Name;
            return Emitter;
        }

        public static void RunBuild()
        {
            Dictionary<string, Target> PackageTargets = new();
            foreach (var TargetKVP in AllTargets)
                TargetKVP.Value.ResolvePackages(ref PackageTargets);
            AllTargets.AddRange(PackageTargets);

            foreach (var TargetKVP in AllTargets)
                TargetKVP.Value.ResolveDependencies();
            foreach (var TargetKVP in AllTargets)
                TargetKVP.Value.ResolveArguments();

            Dictionary<TaskFingerprint, Task> EmitterTasks = new(TaskEmitters.Count * AllTargets.Count);
            foreach (var TargetKVP in AllTargets)
            {
                var TargetName = TargetKVP.Key;
                var Target = TargetKVP.Value;

                Target.CallAllActions(Target.BeforeBuildActions);

                // emitters
                foreach (var EmitterKVP in TaskEmitters)
                {
                    var EmitterName = EmitterKVP.Key;
                    var Emitter = EmitterKVP.Value;
                    TaskFingerprint Fingerprint = new TaskFingerprint
                    {
                        TargetName = TargetName,
                        File = "",
                        TaskName = EmitterName
                    };
                    var EmitterTask = TaskManager.Run(Fingerprint, () =>
                    {
                        List<Task> FileTasks = new (Target.AllFiles.Count);
                        Emitter.AwaitExternalTargetDependencies(Target).Wait();
                        Emitter.AwaitPerTargetDependencies(Target).Wait();

                        var Result = Emitter.PerTargetTask(Target);
                        foreach (var File in Target.AllFiles)
                        {
                            if (!Emitter.FileFilter(File))
                                continue;

                            Emitter.AwaitPerFileDependencies(Target, File).Wait();
                            TaskFingerprint FileFingerprint = new TaskFingerprint
                            {
                                TargetName = TargetName,
                                File = File,
                                TaskName = EmitterName
                            };
                            var FileTask = TaskManager.Run(FileFingerprint, () =>
                            {
                                return Emitter.PerFileTask(Target, File);
                            });
                            FileTasks.Add(FileTask);
                        }
                        Task.WaitAll(FileTasks);
                        return Result;
                    });

                    EmitterTasks.Add(Fingerprint, EmitterTask);
                }
            }

            Task.WaitAll(EmitterTasks.Values);
        }

        private static async Task AwaitExternalTargetDependencies(this TaskEmitter Emitter, Target Target)
        {
            foreach (var DepTarget in Target.Dependencies)
            {
                // check target is existed
                if (!AllTargets.TryGetValue(DepTarget, out var _))
                    throw new ArgumentException($"TargetEmitter {Emitter.Name}: Target {Target.Name} dependes on {DepTarget}, but it seems not to exist!");

                foreach (var DepEmitter in Emitter.Dependencies.Where(KVP => KVP.Value.Equals(DependencyModel.ExternalTarget)))
                {
                    TaskFingerprint Fingerprint = new TaskFingerprint
                    {
                        TargetName = DepTarget,
                        File = "",
                        TaskName = DepEmitter.Key
                    };
                    await TaskManager.AwaitFingerprint(Fingerprint);
                }
            }
        }
        
        private static async Task AwaitPerTargetDependencies(this TaskEmitter Emitter, Target Target)
        {
            foreach (var Dependency in Emitter.Dependencies.Where(KVP => KVP.Value.Equals(DependencyModel.PerTarget)))
            {
                TaskFingerprint Fingerprint = new TaskFingerprint
                {
                    TargetName = Target.Name,
                    File = "",
                    TaskName = Dependency.Key
                };
                await TaskManager.AwaitFingerprint(Fingerprint);
            }
        }
        private static async Task AwaitPerFileDependencies(this TaskEmitter Emitter, Target Target, string File)
        {
            foreach (var Dependency in Emitter.Dependencies.Where(KVP => KVP.Value.Equals(DependencyModel.PerFile)))
            {
                TaskFingerprint Fingerprint = new TaskFingerprint
                {
                    TargetName = Target.Name,
                    File = File,
                    TaskName = Dependency.Key
                };
                await TaskManager.AwaitFingerprint(Fingerprint);
            }
        }

        private static void CallAllActions(this Target Target, IList<Action<Target>> Actions)
        {
            foreach (var Action in Actions)
                Action(Target);
        }

        private static Dictionary<string, TaskEmitter> TaskEmitters = new();
        internal static Dictionary<string, Target> AllTargets { get; } = new();
        internal static Dictionary<string, Package> AllPackages { get; } = new();
    }
}