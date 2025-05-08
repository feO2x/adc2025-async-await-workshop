# How to async await: internals and expert knowledge for scalable .NET Apps

*Contains slides and sample code for Kenny Pflug's workshop held at the Advanced Developers Conference 2025 in Regensburg, Germany.*

## Slides

Can be found in the PDF in the repository root folder:
https://github.com/feO2x/adc2025-async-await-workshop/blob/main/async-await-workshop.pdf

## Code Samples

### Sample 1: Async vs Sync in ASP.NET Core backends

This example shows how thread starvation can affect your ASP.NET Core backend. Open up two terminals. In the first terminal, run the following command:

```bash
cd 01-AsyncVsSync/AsyncVsSync.Backend
dotnet run -c Release
```

This starts up the backend in Release mode.

The frontend console app essentially performs a spike test against either the async or sync endpoint. Check out `Program.cs` on how you can configure the app.

```bash
cd 01-AsyncVsSync/AsyncVsSync.App
dotnet run -c Release -- -t=async -n=1000 -w=1000
```

This performs a spike test of 1000 request where each request calls `await Task.Delay` for 1000ms. It should take around 1 to 3 seconds to complete, the number of worker threads should be around 2n+1 where n is the number of logical processors in your CPU.

Now run the same test against the sync endpoint:

```bash
dotnet run -c Release -- -t=sync -n=1000 -w=1000
```

You should recognize that this takes roughly 30 seconds to complete. Roughly twice the amount of worker threads will be used on the .NET thread pool to handle the constant blocking of `Thread.Sleep`.

### Sample 2: async await decompiled

This example shows you how an async method is transformed by the C# compiler to a state machine. Go to `02-AsyncDecompiled/AsyncDecompiled.AvaloniaApp/MainWindowViewModel.cs` and take a look at the `CalculateOnBackgroundThreadDecompiled` method and the nested `AsyncStateMachine` struct. To debug the state machine, change the target of the `DelegateCommand` in line 15 of `MainWindowViewModel`.

### Sample 3: Playground

This is a CRUD Web API for contacts and also contains the Transactional Outbox example. To run it, you require Docker or Podman. Start up a PostgreSQL server and RabbitMQ with the following command:

```bash
cd 03-Playground
docker compose up -d
```

Afterward, you can run/debug the `WebApi` project. It also contains a request.http file which calls into the different endpoints.

### Sample 4: IAsyncEnumerable&lt;T&gt; streaming with GRPC and advanced usage of Cancellation Tokens

This example shows a GRPC server which streams to several clients. The clients are implemented in the tests project. Simply execute the tests.

### Sample 5: CPU cache optimization problem

This small console app indicates how certain variable values are simply hold in CPU registers and not shared with other CPU cores. While the console app ends in Debug mode, it will hang forever in Release mode. You need to introduce synchronization primitives like `Thread.MemoryBarrier` or `Volatile.Read` and `Volatile.Write` to ensure that the values are actually written to memory as well as synchronized with other CPU cores, and not just held in CPU registers. For the app to finish in Release mode, you need to change it like this:

```csharp
public static class Program
{
    public static void Main() => CpuCoreCachingOptimization();

    private static void CpuCoreCachingOptimization()
    {
        var complete = false;
        var t = new Thread(() =>
            {
                var toggle = false;
                while (!Volatile.Read(ref complete))
                {
                    toggle = !toggle;
                }
            }
        );
        Console.WriteLine("Starting thread...");
        t.Start();
        Thread.Sleep(1000);
        Console.WriteLine("Setting complete to true...");
        Volatile.Write(ref complete, true);
        Console.WriteLine("Waiting for thread to stop...");
        t.Join();
        Console.WriteLine("Thread stopped.");
    }
}
```