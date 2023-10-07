using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CustomThreadPool.ThreadPools
{
    public class MyThreadPool : IThreadPool
    {
        private long _processedTask;
        private readonly Queue<Action> _publicQueue = new();
        private readonly Dictionary<int, WorkStealingQueue<Action>> _queues = new();
        public long GetTasksProcessedCount() => _processedTask;

        private void Action()
        {
            while (true)
            {
                Action task = null;
                if (_queues[Thread.CurrentThread.ManagedThreadId].LocalPop(ref task))
                {
                    task();
                    Interlocked.Increment(ref _processedTask);
                }
                else
                {
                    lock (_publicQueue)
                    {
                        if (_publicQueue.TryDequeue(out task))
                            _queues[Thread.CurrentThread.ManagedThreadId].LocalPush(task);
                        else if (!_queues.Any(id =>
                                     id.Key != Thread.CurrentThread.ManagedThreadId && !id.Value.IsEmpty))
                            Monitor.Wait(_publicQueue);
                    }

                    if (task is not null) 
                        continue;

                    KeyValuePair<int, WorkStealingQueue<Action>> first = new KeyValuePair<int, WorkStealingQueue<Action>>();
                    foreach (var id in _queues)
                    {
                        if (id.Key != Thread.CurrentThread.ManagedThreadId && !id.Value.IsEmpty)
                        {
                            first = id;
                            break;
                        }
                    }

                    var queueToSteal = first.Value;

                    if (queueToSteal is null || !queueToSteal.TrySteal(ref task)) 
                        continue;

                    task();
                    Interlocked.Increment(ref _processedTask);
                }
            }
        }
        
        public MyThreadPool()
        {
            var threads = CreateBackThreads(Action, Environment.ProcessorCount * 3);

            foreach (var thread in threads)
                _queues[thread.ManagedThreadId] = new WorkStealingQueue<Action>();

            foreach (var thread in threads)
                thread.Start();
        }

        public void EnqueueAction(Action action)
        {
            if (_queues.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                _queues[Thread.CurrentThread.ManagedThreadId].LocalPush(action);
            else
            {
                lock (_publicQueue)
                {
                    _publicQueue.Enqueue(action);
                    Monitor.Pulse(_publicQueue);
                }
            }
        }

        private static Thread[] CreateBackThreads(Action action, int count)
        {
            return Enumerable
                .Range(0, count)
                .Select(_ => new Thread(() => action()) { IsBackground = true })
                .ToArray();
        }
    }
}