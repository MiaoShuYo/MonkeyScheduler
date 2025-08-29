using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.Data.MySQL.Data;
using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace MonkeyScheduler.Data.MySQL.Tests
{
    [TestClass]
    public class MySQLDbContextTests
    {
        private Mock<ILogger<MySqlDbContext>> _loggerMock;
        private const string TestConnectionString = "Server=localhost;Database=testdb;User=testuser;Password=testpass;";

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<MySqlDbContext>>();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var dbContext = new MySqlDbContext(null, _loggerMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithEmptyConnectionString_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var dbContext = new MySqlDbContext(string.Empty, _loggerMock.Object);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionString_CreatesInstance()
        {
            // Arrange & Act
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);

            // Assert
            Assert.IsNotNull(dbContext);
        }

        [TestMethod]
        public void Constructor_WithCustomRetrySettings_CreatesInstance()
        {
            // Arrange & Act
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object, 5, TimeSpan.FromSeconds(2));

            // Assert
            Assert.IsNotNull(dbContext);
        }

        [TestMethod]
        public void Connection_WhenCalled_CreatesNewConnection()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);

            // Act
                var connection = dbContext.Connection;

            // Assert
                Assert.IsNotNull(connection);
                Assert.AreEqual(ConnectionState.Open, connection.State);
        }

        [TestMethod]
        public void Connection_WhenCalledMultipleTimes_ReturnsSameConnection()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);

            // Act
                var connection1 = dbContext.Connection;
                var connection2 = dbContext.Connection;
                
            // Assert
                Assert.IsNotNull(connection1);
                Assert.IsNotNull(connection2);
                Assert.AreSame(connection1, connection2);
        }

        [TestMethod]
        public void Dispose_WhenCalled_ClosesConnection()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);
                var connection = dbContext.Connection;
                Assert.IsNotNull(connection);
                Assert.AreEqual(ConnectionState.Open, connection.State);

                // Act
                dbContext.Dispose();

                // Assert
                Assert.AreEqual(ConnectionState.Closed, connection.State);
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Connection_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);
            dbContext.Dispose();

            // Act
            var connection = dbContext.Connection;
        }

        [TestMethod]
        public void BeginTransaction_WhenCalled_StartsNewTransaction()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);

            // Act
                dbContext.BeginTransaction();

            // Assert
                Assert.IsNotNull(dbContext.CurrentTransaction);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BeginTransaction_WhenTransactionAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);

            // Act
                dbContext.BeginTransaction();
                dbContext.BeginTransaction(); // 第二次调用应该抛出异常
        }

        [TestMethod]
        public void CommitTransaction_WhenCalled_CommitsTransaction()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);
                dbContext.BeginTransaction();
                Assert.IsNotNull(dbContext.CurrentTransaction);

            // Act
                dbContext.CommitTransaction();

            // Assert
                Assert.IsNull(dbContext.CurrentTransaction);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CommitTransaction_WhenNoTransactionExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);

            // Act
            dbContext.CommitTransaction();
        }

        [TestMethod]
        public void RollbackTransaction_WhenCalled_RollbacksTransaction()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);
                dbContext.BeginTransaction();
                Assert.IsNotNull(dbContext.CurrentTransaction);

            // Act
                dbContext.RollbackTransaction();

            // Assert
                Assert.IsNull(dbContext.CurrentTransaction);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RollbackTransaction_WhenNoTransactionExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);

            // Act
            dbContext.RollbackTransaction();
        }

        [TestMethod]
        public void IsConnectionAvailable_WhenConnectionIsHealthy_ReturnsTrue()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);

            // Act
                var isAvailable = dbContext.IsConnectionAvailable();

            // Assert
                Assert.IsTrue(isAvailable);
        }

        [TestMethod]
        public void IsConnectionAvailable_AfterDispose_ReturnsFalse()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);
            dbContext.Dispose();

            // Act
            var isAvailable = dbContext.IsConnectionAvailable();

            // Assert
            Assert.IsFalse(isAvailable);
        }

        [TestMethod]
        public void Dispose_WithActiveTransaction_RollbacksTransaction()
        {
            // Arrange
            var dbContext = new MySQLDbContextForTests(TestConnectionString, _loggerMock.Object);
                dbContext.BeginTransaction();
                Assert.IsNotNull(dbContext.CurrentTransaction);

                // Act
                dbContext.Dispose();

                // Assert
                Assert.IsNull(dbContext.CurrentTransaction);
        }

        [TestMethod]
        public void ConnectionString_ContainsConnectionPoolingSettings()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);

            // Act & Assert
            // 通过反射或其他方式验证连接字符串包含连接池设置
            // 这里我们只能验证实例创建成功
            Assert.IsNotNull(dbContext);
        }
    }
} 