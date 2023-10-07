using System;
using System.Threading;
using System.Diagnostics;

namespace Task1
{
    class Program
    {
        static void InfLoop()
        {
            while (true) {};
        }

        static void CalcLen()
        {
            double lastTime = 0;
            var iters = 0;
            double totalTime = 0;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {
                var time = stopWatch.Elapsed.TotalMilliseconds;
                var iterLen = time - lastTime;
                lastTime = time;

                if (iterLen > 5)
                {
                    totalTime += iterLen;
                    iters += 1;
                }

                if (iters == 50)
                    break;
            }

            Console.WriteLine("{0} ms", totalTime/iters);
        }

        static void Main()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 0);

            var inf = new Thread(() => InfLoop()){Priority = ThreadPriority.Highest};
            var calc = new Thread(() => CalcLen()){Priority = ThreadPriority.Highest};

            inf.Start();
            calc.Start();
        }
    }
}