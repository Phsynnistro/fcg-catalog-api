using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fcg.Catalog.Api.Infrastructure.Persistence;

public static class DbUpdateExceptionExtensions
{
    /// <summary>SQLSTATE 23505 = unique_violation no PostgreSQL.</summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
