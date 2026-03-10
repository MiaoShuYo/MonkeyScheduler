using System.Data;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Data.MySQL.Data;
using Moq;

namespace MonkeyScheduler.Data.MySQL.Tests
{
    public class MySQLDbContextForTests : Data.MySqlDbContext
    {
        private readonly IDbConnection _mockConnection;
        private readonly IDbTransaction _mockTransaction;

        public MySQLDbContextForTests(string connectionString, ILogger<Data.MySqlDbContext> logger = null)
            : base(connectionString, logger)
        {
            // 创建模拟连接和事务
            var mockConnection = new Mock<IDbConnection>();
            var mockTransaction = new Mock<IDbTransaction>();

            // 设置模拟连接的基本行为
            var connectionState = ConnectionState.Open;
            mockConnection.Setup(c => c.State).Returns(() => connectionState);
            mockConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);
            mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>())).Returns(mockTransaction.Object);
            mockConnection.Setup(c => c.ConnectionString).Returns(connectionString);
            
            // 设置Close方法的行为
            mockConnection.Setup(c => c.Close()).Callback(() => connectionState = ConnectionState.Closed);
            
            // 设置Dispose方法的行为
            mockConnection.Setup(c => c.Dispose()).Callback(() => connectionState = ConnectionState.Closed);

            // 设置模拟事务的基本行为
            mockTransaction.Setup(t => t.Connection).Returns(mockConnection.Object);
            mockTransaction.Setup(t => t.Dispose()).Callback(() => { });

            _mockConnection = mockConnection.Object;
            _mockTransaction = mockTransaction.Object;
        }

        public MySQLDbContextForTests(string connectionString, IDbConnection mockConnection, ILogger<Data.MySqlDbContext> logger = null)
            : base(connectionString, logger)
        {
            _mockConnection = mockConnection;
            _mockTransaction = new Mock<IDbTransaction>().Object;
        }

        protected override IDbConnection CreateAndOpenConnection()
        {
            // 返回模拟连接而不是真实的数据库连接
            return _mockConnection;
        }

        protected override bool IsConnectionHealthy(IDbConnection connection)
        {
            // 对于测试，我们总是返回true，表示连接健康
            return true;
        }
    }
} 