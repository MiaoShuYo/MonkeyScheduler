using System.Data;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.Data.MySQL.Tests
{
    public class MySQLDbContextForTests : Data.MySqlDbContext
    {
        private readonly IDbConnection _mockConnection;

        public MySQLDbContextForTests(string connectionString, IDbConnection mockConnection, ILogger<Data.MySqlDbContext> logger = null)
            : base(connectionString, logger)
        {
            _mockConnection = mockConnection;
        }

        public override IDbConnection Connection
        {
            get => _mockConnection;
        }
    }
} 