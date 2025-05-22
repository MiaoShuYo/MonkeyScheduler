-- 创建日志表
CREATE TABLE IF NOT EXISTS Logs (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Level VARCHAR(20) NOT NULL,
    Message TEXT NOT NULL,
    Exception TEXT,
    Timestamp DATETIME NOT NULL,
    Source VARCHAR(255),
    Category VARCHAR(255),
    EventId VARCHAR(100),
    INDEX idx_timestamp (Timestamp),
    INDEX idx_level (Level)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 创建计划任务表
CREATE TABLE IF NOT EXISTS ScheduledTasks (
    Id VARCHAR(36)  PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    CronExpression VARCHAR(100) NOT NULL,
    IsEnabled BOOLEAN NOT NULL DEFAULT true,
    CreatedAt DATETIME NOT NULL,
    LastModifiedAt DATETIME,
    TaskType VARCHAR(100) NOT NULL,
    TaskParameters TEXT,
    INDEX idx_enabled (IsEnabled),
    INDEX idx_created (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 创建任务执行结果表
CREATE TABLE IF NOT EXISTS TaskExecutionResults (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TaskId VARCHAR(36) NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME,
    Status VARCHAR(50) NOT NULL,
    Result TEXT,
    ErrorMessage TEXT,
    StackTrace TEXT,
    WorkerNodeUrl VARCHAR(255) NOT NULL DEFAULT '',
    Success BOOLEAN NOT NULL DEFAULT false,
    INDEX idx_task (TaskId),
    INDEX idx_start_time (StartTime),
    INDEX idx_status (Status),
    FOREIGN KEY (TaskId) REFERENCES ScheduledTasks(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci; 