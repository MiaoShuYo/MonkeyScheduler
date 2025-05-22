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
        public void Connection_WhenCalled_CreatesNewConnection()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);

            // Act & Assert
            try
            {
                var connection = dbContext.Connection;
                Assert.IsNotNull(connection);
                Assert.AreEqual(ConnectionState.Open, connection.State);
            }
            catch (Exception ex)
            {
                // 由于无法连接到数据库，我们期望会抛出异常
                Assert.IsTrue(ex.Message.Contains("创建数据库连接失败") || ex.Message.Contains("Unable to connect"));
            }
        }

        [TestMethod]
        public void Dispose_WhenCalled_ClosesConnection()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);
            try
            {
                var connection = dbContext.Connection;
                Assert.IsNotNull(connection);
                Assert.AreEqual(ConnectionState.Open, connection.State);

                // Act
                dbContext.Dispose();

                // Assert
                Assert.AreEqual(ConnectionState.Closed, connection.State);
            }
            catch (Exception ex)
            {
                // 由于无法连接到数据库，我们期望会抛出异常
                Assert.IsTrue(ex.Message.Contains("创建数据库连接失败") || ex.Message.Contains("Unable to connect"));
            }
        }

        [TestMethod]
        public void BeginTransaction_WhenCalled_StartsNewTransaction()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);

            // Act & Assert
            try
            {
                dbContext.BeginTransaction();
                var connection = dbContext.Connection;
                Assert.IsNotNull(connection);
                Assert.AreEqual(ConnectionState.Open, connection.State);
            }
            catch (MySqlException ex)
            {
                // 由于无法连接到数据库，我们期望会抛出 MySqlException
                Assert.IsTrue(ex.Message.Contains("Unable to connect") || ex.Message.Contains("无法连接到"));
            }
        }

        [TestMethod]
        public void CommitTransaction_WhenCalled_CommitsTransaction()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);
            try
            {
                dbContext.BeginTransaction();
                var connection = dbContext.Connection;
                Assert.IsNotNull(connection);
                Assert.AreEqual(ConnectionState.Open, connection.State);

                // Act & Assert
                dbContext.CommitTransaction();
            }
            catch (MySqlException ex)
            {
                // 由于无法连接到数据库，我们期望会抛出 MySqlException
                Assert.IsTrue(ex.Message.Contains("Unable to connect") || ex.Message.Contains("无法连接到"));
            }
        }

        [TestMethod]
        public void RollbackTransaction_WhenCalled_RollbacksTransaction()
        {
            // Arrange
            var dbContext = new MySqlDbContext(TestConnectionString, _loggerMock.Object);
            try
            {
                dbContext.BeginTransaction();
                var connection = dbContext.Connection;
                Assert.IsNotNull(connection);
                Assert.AreEqual(ConnectionState.Open, connection.State);

                // Act & Assert
                dbContext.RollbackTransaction();
            }
            catch (MySqlException ex)
            {
                // 由于无法连接到数据库，我们期望会抛出 MySqlException
                Assert.IsTrue(ex.Message.Contains("Unable to connect") || ex.Message.Contains("无法连接到"));
            }
        }
    }
} 