using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace SecureConsoleFileManager.Models
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
    public enum Command
    {
        CreateUser,
        Login,
        CreateFile,
        DeleteFile,
        WriteInFile,
        DeleteDirectory,
        CreateDirectory,
        ReadFile,
        MoveFile,
        MoveDirectory,
        MoveDirectoryRecursive
    }
    public class LogEntity
    {
        public Guid Id { get; set; }
        public LogLevel Level { get; set; }
        public Command Command { get; set; } 
        [MaxLength(1000)]
        public required string Message { get; set; }

        public Guid? OperationId { get; set; }
        public Operation? Operation { get; set; }

        public LogEntity() { }


        [SetsRequiredMembers]
        public LogEntity(LogLevel level, Command command, string message)
        {
            Level = level;
            Command = command;
            Message = message;
        }
        [SetsRequiredMembers]
        public LogEntity(LogLevel level, Command command, string message, Operation operation)
        {
            Level = level;
            Command = command;
            Message = message;
            Operation = operation;
        }
        
    }
}