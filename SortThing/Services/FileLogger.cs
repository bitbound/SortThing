using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public class FileLogger : ILogger
    {
        private readonly string _logPath = Path.Combine(Path.GetTempPath(), "SortThing.log");
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly string _categoryName;

        protected static ConcurrentStack<string> ScopeStack { get; } = new ConcurrentStack<string>();

        public FileLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            ScopeStack.Push(state.ToString());
            return new NoopDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
#if DEBUG
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return true;
#endif
                case LogLevel.Information:
                case LogLevel.Warning:
                case LogLevel.Error:
                case LogLevel.Critical:
                    return true;
                case LogLevel.None:
                    break;
                default:
                    break;
            }
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var scopeStack = ScopeStack.Any() ?
                new string[] { ScopeStack.FirstOrDefault(), ScopeStack.LastOrDefault() } :
                Array.Empty<string>();

            
            WriteLog(logLevel, _categoryName, state.ToString(), exception, scopeStack).Wait(3000);
        }

        private async Task WriteLog(LogLevel logLevel, string categoryName, string state, Exception exception, string[] scopeStack)
        {
            try
            {
                // TODO: Pool and sink to disk.
                await _writeLock.WaitAsync();

                await CheckLogFileExists();

                var ex = exception;
                var exMessage = exception?.Message;

                while (ex?.InnerException is not null)
                {
                    exMessage += $" | {ex.InnerException.Message}";
                    ex = ex.InnerException;
                }

                var message = $"[{logLevel}]\t" +
                    $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}\t" +
                    $"[{string.Join(" - ", scopeStack)} - {categoryName}]\t" +
                    $"Message: {state}\t" +
                    $"Exception: {exMessage}{Environment.NewLine}";

                File.AppendAllText(_logPath, message);

            }
            catch { }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task CheckLogFileExists()
        {
            if (!File.Exists(_logPath))
            {
                File.Create(_logPath).Close();
                if (OperatingSystem.IsLinux())
                {
                    await Process.Start("sudo", $"chmod 775 {_logPath}").WaitForExitAsync();
                }
            }

            if (File.Exists(_logPath))
            {
                var fi = new FileInfo(_logPath);
                while (fi.Length > 1000000)
                {
                    var content = File.ReadAllLines(_logPath);
                    await File.WriteAllLinesAsync(_logPath, content.Skip(10));
                    fi = new FileInfo(_logPath);
                }
            }
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
                ScopeStack.TryPop(out _);
            }
        }
    }
}
