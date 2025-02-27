using System.Collections.Concurrent;

namespace SB.Core
{
    public struct TaskFingerprint
    {
        public string TargetName { get; init; }
        public string File { get; init; }
        public string TaskName { get; init; }
    }

    public static class TaskManager
    {
        public static async Task<TResult> Run<TResult>(TaskFingerprint Fingerprint, Func<TResult> Function)
        {
            if (AllTasks.TryGetValue(Fingerprint, out var _))
                throw new ArgumentException($"Task with fingerprint {Fingerprint} already exists! Fingerprint should be unique to every task!");

            var NewTask = Task.Run(Function);
            if (!AllTasks.TryAdd(Fingerprint, NewTask))
            {
                throw new ArgumentException($"Failed to add task with fingerprint {Fingerprint}! Are you adding same tasks in parallel?");
            }

            return await NewTask;
        }

        public static async Task AwaitFingerprint(TaskFingerprint Fingerprint)
        {
            Task ToAwait = null;
            const int YieldThreshold = 1000;
            int YieldTimes = 0;
            while (!AllTasks.TryGetValue(Fingerprint, out ToAwait))
            {
                await Task.Yield();
                YieldTimes += 1;
                if (YieldTimes > YieldThreshold)
                    await Task.Delay(5);
            }
            await ToAwait;
        }

        private static ConcurrentDictionary<TaskFingerprint, Task> AllTasks = new();
    }
}
