using System.Collections.Generic;
using FluentAssertions;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Tests.TransactionalOutbox;

public sealed class OutboxProcessorSessionMockFactory
{
    private readonly List<OutboxProcessorSessionMock> _createdSessions = new ();
    private readonly OutboxFailureContext _failureContext;

    public OutboxProcessorSessionMockFactory(OutboxFailureContext failureContext) => _failureContext = failureContext;

    public List<OutboxItem> OutboxItems { get; } = new ();

    public OutboxProcessorSessionMock Create()
    {
        _failureContext.AdvanceToNextFailure();
        var currentFailure = _failureContext.CurrentFailure;

        var sessionMock = new OutboxProcessorSessionMock(OutboxItems, currentFailure);
        _createdSessions.Add(sessionMock);
        return sessionMock;
    }

    public OutboxProcessorSessionMockFactory OutboxItemsShouldBeEmpty()
    {
        OutboxItems.Should().BeEmpty();
        return this;
    }

    public OutboxProcessorSessionMockFactory AllSuccessfulSessionsShouldBeCommitted()
    {
        for (var i = 0; i < _createdSessions.Count; i++)
        {
            var session = _createdSessions[i];
            // Save changes must not be called on all sessions with errors and the last session
            // (which returns no outbox items from the database)
            if (session.Failure != OutboxFailure.None || i == _createdSessions.Count - 1)
            {
                session.SaveChangesMustNotHaveBeenCalled();
            }
            else
            {
                session.SaveChangesMustHaveBeenCalled();
            }
        }

        return this;
    }

    public OutboxProcessorSessionMockFactory AllSessionsShouldBeDisposed()
    {
        foreach (var session in _createdSessions)
        {
            session.MustBeDisposed();
        }

        return this;
    }

    public void AllSessionsShouldBeCommittedAndDisposed()
    {
        for (var i = 0; i < _createdSessions.Count; i++)
        {
            var session = _createdSessions[i];

            // Save changes must be called on all but the last session (which returns no outbox items from the database)
            if (i < _createdSessions.Count - 1)
            {
                session.SaveChangesMustHaveBeenCalled();
            }

            session.MustBeDisposed();
        }
    }
}