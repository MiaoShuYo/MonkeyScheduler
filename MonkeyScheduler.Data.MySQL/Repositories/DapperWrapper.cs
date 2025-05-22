using System.Data;
using Dapper;

namespace MonkeyScheduler.Data.MySQL.Repositories
{
    public class DapperWrapper : IDapperWrapper
    {
        public async Task<T?> ExecuteScalarAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }
    }
}
