using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Data.MySQL.Data;
using MonkeyScheduler.Data.MySQL.Models;
using MonkeyScheduler.Data.MySQL.Repositories;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.Data.MySQL.Tests
{
    [TestClass]
    public class MySQLTaskRepositoryTests
    {
        private Mock<ILogger<MySqlDbContext>> _loggerMock;
        private Mock<IDbConnection> _mockConnection;
        private MySqlDbContext _dbContext;
        private MySQLTaskRepository _repository;
        private const string TestConnectionString = "Server=localhost;Database=testdb;User=testuser;Password=testpass;";

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<MySqlDbContext>>();
            _mockConnection = new Mock<IDbConnection>();
            _dbContext = new MySQLDbContextForTests(TestConnectionString, _mockConnection.Object, _loggerMock.Object);
            _repository = new MySQLTaskRepository(_dbContext);
        }

        [TestMethod]
        public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new MySQLTaskRepository(null));
        }
        
        [TestMethod]
        public void UpdateTask_WithValidDomainModel_UpdatesCorrectDbModel()
        {
            // Arrange
            var task = new Core.Models.ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "Updated Task",
                CronExpression = "0 0 * * *",
                Enabled = false
            };

            // 模拟命令
            var mockCommand = new Mock<IDbCommand>();
            var mockParameters = new Mock<IDataParameterCollection>();
            var parameters = new List<IDbDataParameter>();

            // 设置命令属性
            mockCommand.SetupProperty(c => c.CommandText);
            mockCommand.SetupProperty(c => c.CommandType);
            mockCommand.Setup(c => c.CreateParameter()).Returns(() => 
            {
                var param = new Mock<IDbDataParameter>();
                param.SetupProperty(p => p.ParameterName);
                param.SetupProperty(p => p.Value);
                param.SetupProperty(p => p.DbType);
                return param.Object;
            });
            mockCommand.Setup(c => c.Parameters).Returns(mockParameters.Object);
            mockCommand.Setup(c => c.ExecuteNonQuery()).Returns(1);

            // 设置参数集合行��
            mockParameters.Setup(p => p.Add(It.IsAny<object>()))
                .Callback<object>(p => 
                {
                    var param = p as IDbDataParameter;
                    Assert.IsNotNull(param);
                    parameters.Add(param);
                })
                .Returns(0);

            // 设置连接
            _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
            _mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
            _mockConnection.Setup(c => c.BeginTransaction()).Returns(new Mock<IDbTransaction>().Object);

            // Act
            _repository.UpdateTask(task);

            // Assert
            mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once);
            Assert.AreEqual(8, parameters.Count); // Id, Name, Description, CronExpression, IsEnabled, LastModifiedAt, TaskType, TaskParameters
            
            // 检查所有参数是否存在
            var parameterNames = parameters.Select(p => p.ParameterName).ToList();
            Assert.IsTrue(parameterNames.Contains("Id"), $"Missing Id parameter. Actual parameters: {string.Join(", ", parameterNames)}");
            Assert.IsTrue(parameterNames.Contains("Name"), $"Missing Name parameter. Actual parameters: {string.Join(", ", parameterNames)}");
            Assert.IsTrue(parameterNames.Contains("Description"), $"Missing Description parameter. Actual parameters: {string.Join(", ", parameterNames)}");
            Assert.IsTrue(parameterNames.Contains("CronExpression"), $"Missing CronExpression parameter. Actual parameters: {string.Join(", ", parameterNames)}");
            Assert.IsTrue(parameterNames.Contains("IsEnabled"), $"Missing IsEnabled parameter. Actual parameters: {string.Join(", ", parameterNames)}");
            Assert.IsTrue(parameterNames.Contains("LastModifiedAt"), $"Missing LastModifiedAt parameter. Actual parameters: {string.Join(", ", parameterNames)}");
            Assert.IsTrue(parameterNames.Contains("TaskType"), $"Missing TaskType parameter. Actual parameters: {string.Join(", ", parameterNames)}");
            Assert.IsTrue(parameterNames.Contains("TaskParameters"), $"Missing TaskParameters parameter. Actual parameters: {string.Join(", ", parameterNames)}");
        }

        [TestMethod]
        public void DeleteTask_WithValidTaskId_ExecutesDeleteCommand()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var expectedId = int.Parse(taskId.ToString("N").Substring(0, 8), System.Globalization.NumberStyles.HexNumber);

            // 模拟命令
            var mockCommand = new Mock<IDbCommand>();
            var mockParameters = new Mock<IDataParameterCollection>();
            var parameters = new List<IDbDataParameter>();

            // 设置命令属性
            mockCommand.SetupProperty(c => c.CommandText);
            mockCommand.SetupProperty(c => c.CommandType);
            mockCommand.Setup(c => c.CreateParameter()).Returns(() => 
            {
                var param = new Mock<IDbDataParameter>();
                param.SetupProperty(p => p.ParameterName);
                param.SetupProperty(p => p.Value);
                param.SetupProperty(p => p.DbType);
                return param.Object;
            });
            mockCommand.Setup(c => c.Parameters).Returns(mockParameters.Object);
            mockCommand.Setup(c => c.ExecuteNonQuery()).Returns(1);

            // 设置参数集合行为
            mockParameters.Setup(p => p.Add(It.IsAny<object>()))
                .Callback<object>(p => 
                {
                    var param = p as IDbDataParameter;
                    Assert.IsNotNull(param);
                    parameters.Add(param);
                })
                .Returns(0);

            // 设置连接
            _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
            _mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
            _mockConnection.Setup(c => c.BeginTransaction()).Returns(new Mock<IDbTransaction>().Object);

            // Act
            _repository.DeleteTask(taskId);

            // Assert
            mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once);
            Assert.AreEqual(1, parameters.Count);
            Assert.AreEqual("Id", parameters[0].ParameterName);
            Assert.AreEqual(expectedId, parameters[0].Value);
            Assert.AreEqual(DbType.Int32, parameters[0].DbType);
        }

        [TestMethod]
        public void AddTask_WithNullTask_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _repository.AddTask(null));
        }

        [TestMethod]
        public void UpdateTask_WithNullTask_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _repository.UpdateTask(null));
        }
    }
}
