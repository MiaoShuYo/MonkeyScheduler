using System.Data;
using Dapper;

namespace MonkeyScheduler.Data.MySQL.Tests
{
    public class TestDbConnection : IDbConnection
    {
        private readonly IDbConnection _innerConnection;
        private ConnectionState _state;

        public TestDbConnection(IDbConnection innerConnection)
        {
            _innerConnection = innerConnection;
            _state = ConnectionState.Closed;
        }

        public string ConnectionString
        {
            get => _innerConnection.ConnectionString;
            set => _innerConnection.ConnectionString = value;
        }

        public int ConnectionTimeout => _innerConnection.ConnectionTimeout;
        public string Database => _innerConnection.Database;
        public ConnectionState State => _state;

        public IDbTransaction BeginTransaction()
        {
            return _innerConnection.BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return _innerConnection.BeginTransaction(il);
        }

        public void ChangeDatabase(string databaseName)
        {
            _innerConnection.ChangeDatabase(databaseName);
        }

        public void Close()
        {
            _innerConnection.Close();
            _state = ConnectionState.Closed;
        }

        public IDbCommand CreateCommand()
        {
            return _innerConnection.CreateCommand();
        }

        public void Dispose()
        {
            _innerConnection.Dispose();
        }

        public void Open()
        {
            _innerConnection.Open();
            _state = ConnectionState.Open;
        }

        // 添加对Dapper扩展方法的支持
        public Task<int> ExecuteScalarAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            var result = _innerConnection.ExecuteScalar(sql, param, transaction, commandTimeout, commandType);
            if (result is T typedResult)
            {
                return Task.FromResult(Convert.ToInt32(typedResult));
            }

            return Task.FromResult(Convert.ToInt32(result));
        }

        public Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            return Task.FromResult(
                _innerConnection.Query<T>(sql, param, transaction, true, commandTimeout, commandType));
        }

        // 添加同步版本的Dapper方法
        public T ExecuteScalar<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            return _innerConnection.ExecuteScalar<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            return _innerConnection.Query<T>(sql, param, transaction, true, commandTimeout, commandType);
        }

        // 添加对Dapper扩展方法的支持（非泛型版本）
        public Task<object> ExecuteScalarAsync(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            return Task.FromResult(_innerConnection.ExecuteScalar(sql, param, transaction, commandTimeout,
                commandType));
        }

        public object ExecuteScalar(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            return _innerConnection.ExecuteScalar(sql, param, transaction, commandTimeout, commandType);
        }

        // 添加对Dapper扩展方法的支持（带bool参数版本）
        public Task<int> ExecuteScalarAsync<T>(string sql, object param, IDbTransaction transaction,
            int? commandTimeout, bool buffered, CommandType? commandType)
        {
            var result = _innerConnection.ExecuteScalar<T>(sql, param, transaction, commandTimeout, commandType);
            return Task.FromResult(Convert.ToInt32(result));
        }

        public Task<IEnumerable<T>> QueryAsync<T>(string sql, object param, IDbTransaction transaction,
            int? commandTimeout, bool buffered, CommandType? commandType)
        {
            return Task.FromResult(
                _innerConnection.Query<T>(sql, param, transaction, true, commandTimeout, commandType));
        }

        public T ExecuteScalar<T>(string sql, object param, IDbTransaction transaction, int? commandTimeout,
            bool buffered, CommandType? commandType)
        {
            return _innerConnection.ExecuteScalar<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public IEnumerable<T> Query<T>(string sql, object param, IDbTransaction transaction, int? commandTimeout,
            bool buffered, CommandType? commandType)
        {
            return _innerConnection.Query<T>(sql, param, transaction, true, commandTimeout, commandType);
        }

        // 添加ExecuteScalar的6参数重载
        public int ExecuteScalar(string sql, object param, IDbTransaction transaction, int? commandTimeout,
            bool buffered, CommandType? commandType)
        {
            var result = _innerConnection.ExecuteScalar(sql, param, transaction, commandTimeout, commandType);
            return Convert.ToInt32(result);
        }

        // 添加ExecuteScalarAsync<int>的6参数重载
        public Task<int> ExecuteScalarAsync(string sql, object param, IDbTransaction transaction, int? commandTimeout,
            bool buffered, CommandType? commandType)
        {
            var result = _innerConnection.ExecuteScalar<int>(sql, param, transaction, commandTimeout, commandType);
            return Task.FromResult(result);
        }
    }
}