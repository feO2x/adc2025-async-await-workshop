using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace AsyncStreamingGrpc.AsyncStreaming;

public sealed class NumberGenerator
{
    private readonly ILogger _logger;
    private readonly NumberGeneratorOptions _options;
    private CancellationTokenSource? _currentCancellationTokenSource;
    private int _currentNumber;
    private Subject<int>? _currentSubject;

    public NumberGenerator(NumberGeneratorOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
    }

    public IAsyncEnumerable<int> GetAsyncEnumerable() => GetOrCreateCurrentSubject().ToAsyncEnumerable();

    public bool TryStartGeneratingNumbers()
    {
        lock (this)
        {
            if (_currentCancellationTokenSource is not null)
            {
                return false;
            }

            var cancellationTokenSource = new CancellationTokenSource();
            _ = GenerateNumbersAsync(cancellationTokenSource.Token);
            _currentCancellationTokenSource = cancellationTokenSource;
            return true;
        }
    }

    public bool TryStopGeneratingNumbers()
    {
        lock (this)
        {
            if (_currentCancellationTokenSource is null)
            {
                return false;
            }

            try
            {
                _currentCancellationTokenSource.Cancel();
                _currentCancellationTokenSource.Dispose();
                _currentCancellationTokenSource = null;
                _logger.Information("Stopped generating numbers");
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "An error occurred while stopping number generation");
                return false;
            }
        }
    }

    /* This method shows that lock can be used in async methods. IMPORTANT: within the scope of a lock,
     * it is not allowed to use the await keyword. await usually results in a return to the caller of the method,
     * which would end the lock scope too early (unless the async operation finishes synchronously).
     * However, your code will work fine as long as there are no await call within the lock scope.
     */
    private async Task GenerateNumbersAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.Information("Starting to generate numbers");
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!PushNextNumber())
                {
                    _logger.Information("Number sequence completed");
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            lock (this)
            {
                CompleteCurrentSubject();
            }

            _logger.Information("Number generation was cancelled");
        }
        catch (Exception e)
        {
            _logger.Error(e, "An error occurred while generating numbers");
        }
    }

    private Subject<int> GetOrCreateCurrentSubject()
    {
        lock (this)
        {
            if (_currentSubject is not null)
            {
                return _currentSubject;
            }

            _currentSubject = new Subject<int>();
            _currentNumber = 0;
            return _currentSubject;
        }
    }

    private bool PushNextNumber()
    {
        lock (this)
        {
            if (_currentSubject is null)
            {
                return false;
            }

            _currentSubject.OnNext(++_currentNumber);
            _logger.Information("Published number {Number}", _currentNumber);

            if (_currentNumber == _options.AmountOfNumbers)
            {
                CompleteCurrentSubject();
                return false;
            }

            return true;
        }
    }

    // Must only be called from within a lock
    private void CompleteCurrentSubject()
    {
        if (_currentSubject is null)
        {
            return;
        }

        _currentSubject.OnCompleted();
        _currentSubject.Dispose();
        _currentSubject = null;
    }
}