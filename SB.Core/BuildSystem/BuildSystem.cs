using SB;
using SB.Core;
using System.Collections.Concurrent;
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

        public static Target? GetTarget(string Name) => AllTargets.TryGetValue(Name, out var Found) ? Found : null;

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
            foreach (var TargetKVP in AllTargets)
            {
                TargetKVP.Value.Resolve();
            }

            Dictionary<TaskFingerprint, Task> EmitterTasks = new(TaskEmitters.Count * AllTargets.Count);
            foreach (var TargetKVP in AllTargets)
            {
                var TargetName = TargetKVP.Key;
                var Target = TargetKVP.Value;
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
                        Emitter.AwaitPerTargetDependencies(Target).Wait();
                        var Result = Emitter.PerTargetTask(Target);
                        List<Task> FileTasks = new (Target.AllFiles.Count);
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

        private static Dictionary<string, TaskEmitter> TaskEmitters = new();
        private static Dictionary<string, Target> AllTargets { get; } = new();
    }
}