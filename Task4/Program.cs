using CustomThreadPool.ThreadPools;

namespace CustomThreadPool
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ThreadPoolTests.Run<MyThreadPool>();
        }
    }
}