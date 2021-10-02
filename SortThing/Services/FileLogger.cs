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
        private static readonly ConcurrentQueue<string> _logQueue = new();
        private static readonly ConcurrentStack<string> _scopeStack = new();
        private static readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly string _categoryName;
        private readonly System.Timers.Timer _sinkTimer = new(5000) { AutoReset = false };

        public FileLogger(string categoryName)
        {
            _categoryName = categoryName;
            _sinkTimer.Elapsed += SinkTimer_Elapsed;
        }

        private string LogPath => Path.Combine(Path.GetTempPath(), $"SortThing_{DateTime.Now:yyyy-MM-dd}.log");

        public IDisposable BeginScope<TState>(TState state)
        {
            _scopeStack.Push(state.ToString());
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
            try
            {
                var scopeStack = _scopeStack.Any() ?
                    new string[] { _scopeStack.FirstOrDefault(), _scopeStack.LastOrDefault() } :
                    Array.Empty<string>();

                var message = FormatLogEntry(logLevel, _categoryName, state?.ToString(), exception, scopeStack);
                _logQueue.Enqueue(message);
                _sinkTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error queueing log entry: {ex.Message}");
            }
        }

        private async Task CheckLogFileExists()
        {
            if (!File.Exists(LogPath))
            {
                File.Create(LogPath).Close();
                if (OperatingSystem.IsLinux())
                {
                    await Process.Start("sudo", $"chmod 775 {LogPath}").WaitForExitAsync();
                }
            }
        }

        private string FormatLogEntry(LogLevel logLevel, string categoryName, string state, Exception exception, string[] scopeStack)
        {
            var ex = exception;
            var exMessage = exception?.Message;

            while (ex?.InnerException is not null)
            {
                exMessage += $" | {ex.InnerException.Message}";
                ex = ex.InnerException;
            }

            return $"[{logLevel}]\t" +
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}\t" +
                (
                    scopeStack.Any() ?
                        $"[{string.Join(" - ", scopeStack)} - {categoryName}]\t" :
                        $"[{categoryName}]\t"
                ) +
                $"Message: {state}\t" +
                $"Exception: {exMessage}{Environment.NewLine}";
        }

        private async void SinkTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                await _writeLock.WaitAsync();

                await CheckLogFileExists();

                var message = string.Empty;

                while (_logQueue.TryDequeue(out var entry))
                {
                    message += entry;
                }

                File.AppendAllText(LogPath, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log entry: {ex.Message}");
            }
            finally
            {
                _writeLock.Release();
            }
        }
        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
                _scopeStack.TryPop(out _);
            }
        }
    }
}
