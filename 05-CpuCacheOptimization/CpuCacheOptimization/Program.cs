// ReSharper disable AccessToModifiedClosure
// ReSharper disable LoopVariableIsNeverChangedInsideLoop

namespace CpuCacheOptimization;

public static class Program
{
    public static void Main() => CpuCoreCachingOptimization();

    private static void CpuCoreCachingOptimization()
    {
        var complete = false;
        var t = new Thread(() =>
            {
                var toggle = false;
                while (!complete)
                {
                    toggle = !toggle;
                }
            }
        );
        Console.WriteLine("Starting thread...");
        t.Start();
        Thread.Sleep(1000);
        Console.WriteLine("Setting complete to true...");
        complete = true;
        Console.WriteLine("Waiting for thread to stop...");
        t.Join();
        Console.WriteLine("Thread stopped.");
    }
}