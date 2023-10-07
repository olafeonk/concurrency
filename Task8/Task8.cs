using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Task8
{
    internal sealed class AsyncMultiLock : IAsyncMultiLock
    {
        private readonly object lockObject = new();
        private readonly Dictionary<string, LockKey> lockKeys = new();

        public async Task<IDisposable> AcquireLockAsync(params string[] keys)
        {
            var keysToLock = keys.OrderBy(key => key);
            var lockedKeys = new List<IDisposable>(keys.Length);

            foreach (var key in keysToLock)
                lockedKeys.Add(await LockAsync(key));

            return new CustomDisposable(() =>
            {
                foreach (var key in lockedKeys)
                {
                    key.Dispose();
                }
            });
        }

        private async Task<IDisposable> LockAsync(string key)
        {
            var lockKey = new LockKey();
            LockKey? lastLockKey;
            lock (lockObject)
            {
                if (!lockKeys.TryGetValue(key, out lastLockKey))
                {
                    lockKeys[key] = lockKey;
                    return lockKey;
                }

                lockKeys[key] = lockKey;
            }

            await lastLockKey.Wait();
            return lockKey;
        }

        private sealed class CustomDisposable : IDisposable
        {
            private readonly Action action;

            public CustomDisposable(Action action)
            {
                this.action = action;
            }

            public void Dispose()
            {
                action();
            }
        }
    }

    internal sealed class LockKey : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(0, 1);
        private int isDisposed;

        public async Task Wait() => await semaphore.WaitAsync();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, 1) == 1)
            {
                return;
            }

            semaphore.Release();
        }
    }

    public interface IAsyncMultiLock
    {
        Task<IDisposable> AcquireLockAsync(params string[] keys);
    }

    public static class Test
    {
        public static void Main()
        {
            var @lock = new AsyncMultiLock();

            async Task Func1()
            {
                using (await @lock.AcquireLockAsync("lock1", "lock2"))
                {
                    var a = 0;
                    while (a < 100)
                    {
                        Thread.Sleep(100);
                        a++;
                        Console.WriteLine($"thread1 {a}");
                    }
                }
            }

            async Task Func2()
            {
                using (await @lock.AcquireLockAsync("lock2"))
                {
                    var a = 0;
                    while (a < 100)
                    {
                        Thread.Sleep(100);
                        a++;
                        Console.WriteLine($"thread2 {a}");
                    }
                }
            }

            var thread1 = new Thread(() => Func1().Wait());
            var thread2 = new Thread(() => Func2().Wait());

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();
        }
    }
}