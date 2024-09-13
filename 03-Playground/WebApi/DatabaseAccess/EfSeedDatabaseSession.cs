using System.Threading;
using System.Threading.Tasks;
using Light.DatabaseAccess.EntityFrameworkCore;
using Npgsql;

namespace WebApi.DatabaseAccess;

public sealed class EfSeedDatabaseSession : EfSession<WebApiDbContext>.WithTransaction
{
    public EfSeedDatabaseSession(WebApiDbContext dbContext) : base(dbContext) { }

    public async Task InsertSeedDataAsync(CancellationToken cancellationToken = default)
    {
        await using var command = await CreateCommandAsync<NpgsqlCommand>(
            """
            INSERT INTO "Contacts" ("Id", "FirstName", "LastName", "Email", "PhoneNumber")
            VALUES
                ('D10DF224-7E72-4CB0-94B2-81725D818A1C', 'Alice', 'Smith', 'alice.smith@live.com', '555-1234'),
                ('054AB8AC-369F-410C-9F66-140D1F240613', 'Bob', 'Johnson', 'bob.johnson@gmail.com', '555-2345'),
                ('CCC51159-2AC7-435B-B7D2-4CC25791622D', 'Carol', 'Williams', 'carol.williams@yahoo.com', '555-3456')
            ON CONFLICT ("Id") DO NOTHING;

            INSERT INTO "Addresses" ("Id", "ContactId", "Street", "ZipCode", "City")
            VALUES
                ('E3F45628-00E6-4CB7-99C9-A45DDDC49615', 'D10DF224-7E72-4CB0-94B2-81725D818A1C', '123 Maple Street', '90210', 'Springfield'),
                ('BF718594-C61B-4FF4-AD16-D2347046CEDE', 'D10DF224-7E72-4CB0-94B2-81725D818A1C', '456 Oak Avenue', '12345', 'Shelbyville'),
                ('2D570181-2186-4FDE-B1AD-25DDF04D5AE3', '054AB8AC-369F-410C-9F66-140D1F240613', '789 Pine Road', '67890', 'Evergreen')
            ON CONFLICT ("Id") DO NOTHING;

            INSERT INTO "Orders" ("Id", "State")
            VALUES
                ('963BA496-FE6C-4635-9B59-19B50FF59759', 0),
                ('F857605B-A55C-493C-96A0-CD6F1D7F6DD0', 0),
                ('400F74D4-5770-4F31-99A6-A2B79B9CECEE', 0)
            ON CONFLICT ("Id") DO UPDATE
            SET "State" = excluded."State";
            """,
            cancellationToken
        );
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}