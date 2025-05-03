using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using WebApi.Contacts.Common;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Contacts.DeleteContact;

public static class DeleteContactEndpoint
{
    public static void MapDeleteContact(this WebApplication app) =>
        app.MapDelete("/api/contacts/{id:required:guid}", DeleteContactDecompiled);

    public static async Task<IResult> DeleteContact(
        IDeleteContactSession deleteContactSession,
        ILogger logger,
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var contact = await deleteContactSession.GetContactAsync(id, cancellationToken);
        if (contact is null)
        {
            return Results.NotFound();
        }

        deleteContactSession.RemoveContact(contact);
        await deleteContactSession.SaveChangesAsync(cancellationToken);

        logger.Information("{@Contact} was deleted successfully", contact);
        return Results.Ok(ContactDetailDto.FromContact(contact));
    }
    
    public static Task<IResult> DeleteContactDecompiled(
        IDeleteContactSession deleteContactSession,
        ILogger logger,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var stateMachine = new AsyncStateMachine
        {
            Builder = AsyncTaskMethodBuilder<IResult>.Create(),
            DeleteContactSession = deleteContactSession,
            Logger = logger,
            Id = id,
            CancellationToken = cancellationToken,
            State = -1
        };
        stateMachine.Builder.Start(ref stateMachine);
        return stateMachine.Builder.Task;
    }

    private struct AsyncStateMachine : IAsyncStateMachine
    {
        // MethodBuilder - the reusable part of the state machine
        public AsyncTaskMethodBuilder<IResult> Builder;
        
        // Parameters
        public IDeleteContactSession DeleteContactSession;
        public ILogger Logger;
        public Guid Id;
        public CancellationToken CancellationToken;

        // Variables
        private Contact? _contact;
        
        // Task awaiters
        private TaskAwaiter<Contact?> _firstAwaiter;
        private TaskAwaiter _secondAwaiter;
        
        // -2 = done (successful or exception caught), -1 = running, other states for different await statements
        public int State;
        
        public void MoveNext()
        {
            try
            {
                if (State == -2)
                {
                    return;
                }

                if (State == 0)
                {
                    goto GetResultFromFirstAwaiter;
                }

                if (State == 1)
                {
                    goto GetResultFromSecondAwaiter;
                }
                
                var firstAwaiter = DeleteContactSession.GetContactAsync(Id, CancellationToken).GetAwaiter();
                if (firstAwaiter.IsCompleted)
                {
                    goto FirstContinuation;
                }

                State = 0;
                _firstAwaiter = firstAwaiter;
                Builder.AwaitOnCompleted(ref firstAwaiter, ref this);
                return;
                
                GetResultFromFirstAwaiter:
                firstAwaiter = _firstAwaiter;
                _firstAwaiter = default;
                State = -1;
                
                FirstContinuation:
                _contact = firstAwaiter.GetResult();
                if (_contact is null)
                {
                    State = -2;
                    Builder.SetResult(Results.NotFound());
                    return;
                }
                
                DeleteContactSession.RemoveContact(_contact);
                var secondAwaiter = DeleteContactSession.SaveChangesAsync(CancellationToken).GetAwaiter();
                if (secondAwaiter.IsCompleted)
                {
                    goto SecondContinuation;
                }

                State = 1;
                _secondAwaiter = secondAwaiter;
                Builder.AwaitOnCompleted(ref _secondAwaiter, ref this);
                return;
                
                GetResultFromSecondAwaiter:
                secondAwaiter = _secondAwaiter;
                _secondAwaiter = default;
                State = -1;
                
                SecondContinuation:
                secondAwaiter.GetResult();
                
                Debug.Assert(_contact != null);
                Logger.Information("{@Contact} was deleted successfully", _contact);
                State = -2;
                Builder.SetResult(Results.Ok(ContactDetailDto.FromContact(_contact)));
            }
            catch (Exception exception)
            {
                State = -2;
                Builder.SetException(exception);
            }
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) => Builder.SetStateMachine(stateMachine);
    }
}

