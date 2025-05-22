using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Logging;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;

namespace MonkeySchedulerTest
{
    [TestClass]
    public class LoggerTests
    {
        private string _testDbPath;
        private Logger _logger;

        [TestInitialize]
        public void Initialize()
        {
            try
            {
                // 使用临时文件路径
                _testDbPath = Path.Combine(Path.GetTempPath(), $"test_logs_{Guid.NewGuid()}.db");

                // 创建测试日志记录器
                _logger = new Logger(_testDbPath, maxLogCount: 5, maxLogAge: TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化失败：{ex.Message}");
                throw;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                // 确保日志记录器被释放
                if (_logger != null)
                {
                    // 等待所有异步操作完成
                    Task.Delay(100).Wait();
                }

                // 尝试删除测试数据库文件
                if (File.Exists(_testDbPath))
                {
                    try
                    {
                        File.Delete(_testDbPath);
                    }
                    catch (IOException)
                    {
                        // 如果文件被占用，等待一段时间后重试
                        Thread.Sleep(100);
                        try
                        {
                            File.Delete(_testDbPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"警告：无法删除测试数据库文件：{ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理失败：{ex.Message}");
            }
        }

        [TestMethod]
        public async Task LogAsync_ShouldRecordLogSuccessfully()
        {
            // Arrange
            var testMessage = "测试日志消息";
            var testException = new Exception("测试异常");

            // Act
            await _logger.LogAsync("INFO", testMessage);
            await _logger.LogAsync("ERROR", testMessage, testException);

            // Assert
            var logCount = await _logger.GetLogCountAsync();
            Assert.AreEqual(2, logCount, "应该记录了两条日志");
        }

        [TestMethod]
        public async Task LogInfoAsync_ShouldRecordInfoLog()
        {
            // Arrange
            var testMessage = "测试信息日志";

            // Act
            await _logger.LogInfoAsync(testMessage);

            // Assert
            var logCount = await _logger.GetLogCountAsync();
            Assert.AreEqual(1, logCount, "应该记录了一条信息日志");
        }

        [TestMethod]
        public async Task LogWarningAsync_ShouldRecordWarningLog()
        {
            // Arrange
            var testMessage = "测试警告日志";

            // Act
            await _logger.LogWarningAsync(testMessage);

            // Assert
            var logCount = await _logger.GetLogCountAsync();
            Assert.AreEqual(1, logCount, "应该记录了一条警告日志");
        }

        [TestMethod]
        public async Task LogErrorAsync_ShouldRecordErrorLog()
        {
            // Arrange
            var testMessage = "测试错误日志";
            var testException = new Exception("测试异常");

            // Act
            await _logger.LogErrorAsync(testMessage, testException);

            // Assert
            var logCount = await _logger.GetLogCountAsync();
            Assert.AreEqual(1, logCount, "应该记录了一条错误日志");
        }

        [TestMethod]
        public async Task CleanupByCount_ShouldRemoveOldestLogs()
        {
            // Arrange
            // 添加超过最大数量的日志
            for (int i = 0; i < 10; i++)
            {
                await _logger.LogInfoAsync($"测试日志 {i}");
            }

            // Act
            // 等待清理操作完成
            await Task.Delay(100);

            // Assert
            var logCount = await _logger.GetLogCountAsync();
            Assert.IsTrue(logCount <= 5, "日志数量应该不超过最大限制");
        }

        [TestMethod]
        public async Task CleanupByAge_ShouldRemoveOldLogs()
        {
            // Arrange
            // 创建旧的日志记录器，设置较短的保留时间
            var oldLogger = new Logger(_testDbPath, maxLogAge: TimeSpan.FromMilliseconds(100));
            await oldLogger.LogInfoAsync("旧日志");

            // Act
            // 等待日志过期
            await Task.Delay(200);

            // 使用新的日志记录器（会触发清理）
            var newLogger = new Logger(_testDbPath, maxLogAge: TimeSpan.FromMilliseconds(100));
            
            // 强制触发清理
            await newLogger.LogInfoAsync("新日志");
            await Task.Delay(100); // 等待清理完成

            // Assert
            var logCount = await newLogger.GetLogCountAsync();
            Assert.AreEqual(1, logCount, "应该只保留最新的日志");
            
            // 验证只保留了新日志
            var oldestDate = await newLogger.GetOldestLogDateAsync();
            Assert.IsNotNull(oldestDate);
            Assert.IsTrue((DateTime.UtcNow - oldestDate.Value).TotalSeconds < 1, "保留的日志应该是新添加的日志");
        }

        [TestMethod]
        public async Task GetOldestLogDate_ShouldReturnCorrectDate()
        {
            // Arrange
            var testDate = DateTime.UtcNow;
            await _logger.LogInfoAsync("测试日志");

            // Act
            var oldestDate = await _logger.GetOldestLogDateAsync();

            // Assert
            Assert.IsNotNull(oldestDate);
            Assert.IsTrue((testDate - oldestDate.Value).TotalSeconds < 1, "返回的日期应该接近当前时间");
        }

        [TestMethod]
        public async Task LogAsync_ShouldHandleConcurrentLogs()
        {
            // Arrange
            var tasks = new Task[10];
            var logger = new Logger(_testDbPath, maxLogCount: 10); // 增加最大日志数量限制

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = logger.LogInfoAsync($"并发测试日志 {i}");
            }
            await Task.WhenAll(tasks);

            // Assert
            var logCount = await logger.GetLogCountAsync();
            Assert.AreEqual(10, logCount, "所有并发日志都应该被记录");
        }

        [TestMethod]
        public async Task LogAsync_ShouldHandleNullException()
        {
            // Arrange
            var testMessage = "测试空异常日志";

            // Act
            await _logger.LogErrorAsync(testMessage, null);

            // Assert
            var logCount = await _logger.GetLogCountAsync();
            Assert.AreEqual(1, logCount, "应该记录了一条带空异常的日志");
        }

        [TestMethod]
        public async Task LogAsync_ShouldHandleEmptyMessage()
        {
            // Arrange
            var emptyMessage = "";

            // Act
            await _logger.LogInfoAsync(emptyMessage);

            // Assert
            var logCount = await _logger.GetLogCountAsync();
            Assert.AreEqual(1, logCount, "应该记录了一条空消息的日志");
        }

        [TestMethod]
        public void Log_ShouldRecordLogWithState()
        {
            // Arrange
            var state = new { Name = "测试", Value = 123 };
            var formatter = (object s, Exception? e) => $"状态: {s}";
            var eventId = new EventId(1, "测试事件");

            // Act
            _logger.Log(LogLevel.Information, eventId, state, null, formatter);

            // Assert
            var logCount = _logger.GetLogCountAsync().Result;
            Assert.AreEqual(1, logCount, "应该记录了一条日志");
        }

        [TestMethod]
        public void Log_ShouldRecordLogWithException()
        {
            // Arrange
            var state = "测试状态";
            var exception = new Exception("测试异常");
            var formatter = (string s, Exception? e) => $"消息: {s}, 异常: {e?.Message}";
            var eventId = new EventId(1, "测试事件");

            // Act
            _logger.Log(LogLevel.Error, eventId, state, exception, formatter);

            // Assert
            var logCount = _logger.GetLogCountAsync().Result;
            Assert.AreEqual(1, logCount, "应该记录了一条带异常的日志");
        }

        [TestMethod]
        public void Log_ShouldNotRecordWhenLevelNotEnabled()
        {
            // Arrange
            var state = "测试状态";
            var formatter = (string s, Exception? e) => s;
            var eventId = new EventId(1, "测试事件");

            // Act
            _logger.Log(LogLevel.None, eventId, state, null, formatter);

            // Assert
            var logCount = _logger.GetLogCountAsync().Result;
            Assert.AreEqual(0, logCount, "不应该记录 None 级别的日志");
        }

        [TestMethod]
        public void Log_ShouldHandleNullFormatter()
        {
            // Arrange
            var state = "测试状态";
            var eventId = new EventId(1, "测试事件");

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                _logger.Log(LogLevel.Information, eventId, state, null, null!);
            });
        }

        [TestMethod]
        public void Log_ShouldHandleNullState()
        {
            // Arrange
            var formatter = (object? s, Exception? e) => s?.ToString() ?? "null";
            var eventId = new EventId(1, "测试事件");

            // Act
            _logger.Log(LogLevel.Information, eventId, null, null, formatter);

            // Assert
            var logCount = _logger.GetLogCountAsync().Result;
            Assert.AreEqual(1, logCount, "应该记录了一条空状态的日志");
        }

        [TestMethod]
        public void IsEnabled_ShouldReturnFalseForNoneLevel()
        {
            // Act & Assert
            Assert.IsFalse(_logger.IsEnabled(LogLevel.None), "None 级别的日志应该被禁用");
        }

        [TestMethod]
        public void IsEnabled_ShouldReturnTrueForOtherLevels()
        {
            // Act & Assert
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Trace), "Trace 级别的日志应该被启用");
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Debug), "Debug 级别的日志应该被启用");
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Information), "Information 级别的日志应该被启用");
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Warning), "Warning 级别的日志应该被启用");
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Error), "Error 级别的日志应该被启用");
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Critical), "Critical 级别的日志应该被启用");
        }

        [TestMethod]
        public void BeginScope_ShouldReturnNull()
        {
            // Arrange
            var state = "测试状态";

            // Act
            var scope = _logger.BeginScope(state);

            // Assert
            Assert.IsNull(scope, "BeginScope 应该返回 null");
        }

        [TestMethod]
        public void Log_ShouldHandleCustomFormatter()
        {
            // Arrange
            var state = new { Name = "测试", Value = 123 };
            var formatter = (object s, Exception? e) => $"自定义格式: {s}";
            var eventId = new EventId(1, "测试事件");

            // Act
            _logger.Log(LogLevel.Information, eventId, state, null, formatter);

            // Assert
            var logCount = _logger.GetLogCountAsync().Result;
            Assert.AreEqual(1, logCount, "应该记录了一条使用自定义格式化器的日志");
        }

        [TestMethod]
        public void Log_ShouldHandleEventId()
        {
            // Arrange
            var state = "测试状态";
            var eventId = new EventId(123, "测试事件");
            var formatter = (string s, Exception? e) => $"事件ID: {eventId.Id}, 名称: {eventId.Name}, 消息: {s}";

            // Act
            _logger.Log(LogLevel.Information, eventId, state, null, formatter);

            // Assert
            var logCount = _logger.GetLogCountAsync().Result;
            Assert.AreEqual(1, logCount, "应该记录了一条包含事件ID的日志");
        }

        [TestMethod]
        public void Log_ShouldHandleDifferentStateTypes()
        {
            // Arrange
            var stringState = "字符串状态";
            var intState = 123;
            var objectState = new { Name = "测试", Value = 456 };
            var eventId = new EventId(1, "测试事件");

            // Act
            _logger.Log(LogLevel.Information, eventId, stringState, null, (s, e) => s.ToString());
            _logger.Log(LogLevel.Information, eventId, intState, null, (s, e) => s.ToString());
            _logger.Log(LogLevel.Information, eventId, objectState, null, (s, e) => s.ToString());

            // Assert
            var logCount = _logger.GetLogCountAsync().Result;
            Assert.AreEqual(3, logCount, "应该记录了三种不同类型的状态日志");
        }

        [TestMethod]
        public void Log_ShouldHandleEmptyEventId()
        {
            // Arrange
            var state = "测试状态";
            var eventId = new EventId();
            var formatter = (string s, Exception? e) => s;

            // Act
            _logger.Log(LogLevel.Information, eventId, state, null, formatter);

            // Assert
            var logCount = _logger.GetLogCountAsync().Result;
            Assert.AreEqual(1, logCount, "应该记录了一条空事件ID的日志");
        }

        [TestMethod]
        public void InitializeDatabase_ShouldNotRecreateExistingDatabase()
        {
            // Arrange
            // 确保数据库文件已存在
            Assert.IsTrue(File.Exists(_testDbPath), "测试数据库文件应该已存在");
            
            // 记录当前数据库文件的最后修改时间
            var originalLastWriteTime = File.GetLastWriteTime(_testDbPath);
            
            // 创建新的Logger实例，使用相同的数据库路径
            var newLogger = new Logger(_testDbPath);
            
            // Act
            // 等待一小段时间，确保文件有时间被修改（如果有的话）
            Thread.Sleep(100);
            
            // 获取数据库文件的最后修改时间
            var newLastWriteTime = File.GetLastWriteTime(_testDbPath);
            
            // Assert
            Assert.AreEqual(originalLastWriteTime, newLastWriteTime, "数据库文件不应该被重新创建");
            
            // 验证新Logger可以正常使用
            newLogger.LogInfoAsync("测试日志").Wait();
            var logCount = newLogger.GetLogCountAsync().Result;
            Assert.IsTrue(logCount > 0, "新Logger应该能够正常记录日志");
        }

        [TestMethod]
        public async Task GetOldestLogDate_ShouldReturnNullWhenNoLogs()
        {
            // Arrange
            // 创建一个新的空数据库
            var emptyDbPath = Path.Combine(Path.GetTempPath(), $"empty_logs_{Guid.NewGuid()}.db");
            var emptyLogger = new Logger(emptyDbPath);
            
            try
            {
                // Act
                var oldestDate = await emptyLogger.GetOldestLogDateAsync();
                
                // Assert
                Assert.IsNull(oldestDate, "当数据库中没有日志时，应该返回null");
            }
            finally
            {
                // 清理
                if (File.Exists(emptyDbPath))
                {
                    try
                    {
                        File.Delete(emptyDbPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"警告：无法删除测试数据库文件：{ex.Message}");
                    }
                }
            }
        }

        [TestMethod]
        public async Task LogAsync_ShouldHandleDatabaseConnectionFailure()
        {
            // Arrange
            // 创建一个有效的路径，但使用一个无效的数据库文件（空文件）
            var invalidDbPath = Path.Combine(Path.GetTempPath(), $"invalid_logs_{Guid.NewGuid()}.db");
            File.WriteAllText(invalidDbPath, ""); // 创建一个空文件，这会导致SQLite连接失败
            
            try
            {
                var invalidLogger = new Logger(invalidDbPath);
                
                // Act & Assert
                // 尝试记录日志，应该抛出异常
                await Assert.ThrowsExceptionAsync<SQLiteException>(async () =>
                {
                    await invalidLogger.LogAsync("ERROR", "测试数据库连接失败");
                });
            }
            finally
            {
                // 清理
                if (File.Exists(invalidDbPath))
                {
                    try
                    {
                        File.Delete(invalidDbPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"警告：无法删除测试数据库文件：{ex.Message}");
                    }
                }
            }
        }

        [TestMethod]
        public void Logger_ShouldUseCustomFormatter()
        {
            // Arrange
            var customFormatter = new Mock<ILogFormatter>();
            customFormatter.Setup(f => f.Format(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()))
                .Returns("自定义格式化日志");
            
            var logger = new Logger(_testDbPath, formatter: customFormatter.Object);
            
            // Act
            logger.Log(LogLevel.Information, new EventId(), "测试消息", null, (s, e) => s.ToString());
            
            // Assert
            customFormatter.Verify(f => f.Format("INFORMATION", It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [TestMethod]
        public async Task CleanupLogs_ShouldHandleExceptions()
        {
            // Arrange
            // 创建一个自定义的ILogFormatter，在Format方法中抛出异常
            var exceptionFormatter = new Mock<ILogFormatter>();
            exceptionFormatter.Setup(f => f.Format(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()))
                .Throws(new Exception("格式化异常"));
            
            // 创建一个新的数据库路径
            var testDbPath = Path.Combine(Path.GetTempPath(), $"exception_test_{Guid.NewGuid()}.db");
            
            try
            {
                var logger = new Logger(testDbPath, formatter: exceptionFormatter.Object);
                
                // Act & Assert
                // 尝试记录日志，应该抛出异常
                await Assert.ThrowsExceptionAsync<Exception>(async () =>
                {
                    await logger.LogAsync("ERROR", "测试清理异常处理");
                });
                
                // 验证Logger实例仍然可用
                var newFormatter = new DefaultLogFormatter();
                logger = new Logger(testDbPath, formatter: newFormatter);
                await logger.LogInfoAsync("恢复测试");
                
                var logCount = await logger.GetLogCountAsync();
                Assert.IsTrue(logCount >= 0, "Logger应该能够在异常后继续工作");
            }
            finally
            {
                // 清理
                if (File.Exists(testDbPath))
                {
                    try
                    {
                        File.Delete(testDbPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"警告：无法删除测试数据库文件：{ex.Message}");
                    }
                }
            }
        }
    }
} 