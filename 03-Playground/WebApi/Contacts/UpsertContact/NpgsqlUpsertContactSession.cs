using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApi.Contacts.Common;
using WebApi.DatabaseAccess;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Contacts.UpsertContact;

public sealed class NpgsqlUpsertContactSession : NpgsqlBatchSession, IUpsertContactSession
{
    public NpgsqlUpsertContactSession(WebApiDbContext dbContext) : base(
        dbContext,
        IsolationLevel.ReadCommitted,
        QueryTrackingBehavior.NoTracking
    ) { }

    public Task<Dictionary<Guid, Address>> GetContactAddressesAsync(
        List<Guid> addressIds,
        Guid contactId,
        CancellationToken cancellationToken = default
    ) =>
        DbContext
           .Addresses
           .Where(a => a.ContactId == contactId || addressIds.Contains(a.Id))
           .ToDictionaryAsync(a => a.Id, cancellationToken);

    public async Task UpsertContactAsync(ContactDetailDto dto, CancellationToken cancellationToken = default)
    {
        var batch = await GetRequiredBatchAsync(cancellationToken);
        batch.AddBatchCommand(
            """
            INSERT INTO "Contacts" ("Id", "FirstName", "LastName", "Email", "PhoneNumber")
            VALUES ($1, $2, $3, $4, $5)
            ON CONFLICT ("Id") DO UPDATE
            SET "FirstName" = excluded."FirstName",
                "LastName" = excluded."LastName",
                "Email" = excluded."Email",
                "PhoneNumber" = excluded."PhoneNumber";
            """,
            dto.Id,
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.PhoneNumber
        );
    }

    public async Task UpsertAddressAsync(Address address, CancellationToken cancellationToken = default)
    {
        var batch = await GetRequiredBatchAsync(cancellationToken);
        batch.AddBatchCommand(
            """
            INSERT INTO "Addresses" ("Id", "ContactId", "Street", "ZipCode", "City")
            VALUES ($1, $2, $3, $4, $5)
            ON CONFLICT ("Id") DO UPDATE
            SET "Street" = excluded."Street",
                "ZipCode" = excluded."ZipCode",
                "City" = excluded."City";
            """,
            address.Id,
            address.ContactId,
            address.Street,
            address.ZipCode,
            address.City
        );
    }

    public async Task RemoveAddressAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        var batch = await GetRequiredBatchAsync(cancellationToken);
        batch.AddBatchCommand(
            """
            DELETE FROM "Addresses"
            WHERE "Id" = $1;
            """,
            addressId
        );
    }
}