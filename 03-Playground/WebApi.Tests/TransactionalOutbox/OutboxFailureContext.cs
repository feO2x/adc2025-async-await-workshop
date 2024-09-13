using System.Collections.Generic;

namespace WebApi.Tests.TransactionalOutbox;

public sealed class OutboxFailureContext
{
    private int _currentFailureIndex = -1;

    public List<OutboxFailure> Failures { get; } = new ();

    public OutboxFailure CurrentFailure =>
        _currentFailureIndex >= Failures.Count ? OutboxFailure.None : Failures[_currentFailureIndex];

    public void AdvanceToNextFailure() => _currentFailureIndex++;
}