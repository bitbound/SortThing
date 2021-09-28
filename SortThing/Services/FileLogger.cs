﻿using SortThing.Enums;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IFileLogger
    {
        Task DeleteLogs();
        Task<byte[]> ReadAllLogs();
        Task Write(Exception ex, EventType eventType = EventType.Error, [CallerMemberName] string callerName = "");
        Task Write(Exception ex, string message, EventType eventType = EventType.Error, [CallerMemberName] string callerName = "");
        Task Write(string message, EventType eventType = EventType.Info, [CallerMemberName] string callerName = "");
    }

    public class FileLogger : IFileLogger
    {

        private readonly string _logPath = Path.Combine(Path.GetTempPath(), "SortThing.log");
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public FileLogger()
        {
            Console.WriteLine($"Logger initialized.  Writing to {_logPath}.\n\n");
        }

        public async Task DeleteLogs()
        {
            try
            {
                await _writeLock.WaitAsync();

                if (File.Exists(_logPath))
                {
                    File.Delete(_logPath);
                }
            }
            catch { }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task<byte[]> ReadAllLogs()
        {
            try
            {
                await _writeLock.WaitAsync();

                await CheckLogFileExists();

                return await File.ReadAllBytesAsync(_logPath);
            }
            catch (Exception ex)
            {
                await Write(ex);
                return Array.Empty<byte>();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task Write(string message, EventType eventType = EventType.Info, [CallerMemberName] string callerName = "")
        {
            try
            {
                // TODO: Pool and sink to disk.
                await _writeLock.WaitAsync();

                await CheckLogFileExists();
                var formattedMessage = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}\t[{eventType}]\t[{callerName}]\t{message}{Environment.NewLine}";
                File.AppendAllText(_logPath, formattedMessage);
                Console.WriteLine(formattedMessage);
            }
            catch { }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task Write(Exception ex, EventType eventType = EventType.Error, [CallerMemberName] string callerName = "")
        {
            try
            {
                // TODO: Pool and sink to disk.
                await _writeLock.WaitAsync();

                await CheckLogFileExists();

                var exception = ex;

                while (exception != null)
                {
                    var formattedMessage = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}\t[{eventType}]\t[{callerName}]\t{exception?.Message}\t{exception?.StackTrace}\t{exception?.Source}{Environment.NewLine}";
                    File.AppendAllText(_logPath, formattedMessage);
                    Console.WriteLine(formattedMessage);
                    exception = exception.InnerException;
                }
            }
            catch { }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task Write(Exception ex, string message, EventType eventType = EventType.Error, [CallerMemberName] string callerName = "")
        {
            await Write(message, eventType, callerName);
            await Write(ex, eventType, callerName);
        }

        private async Task CheckLogFileExists()
        {
            if (!File.Exists(_logPath))
            {
                File.Create(_logPath).Close();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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
    }
}