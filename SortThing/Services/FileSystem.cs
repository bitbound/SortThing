﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IFileSystem
    {
        Task AppendAllLinesAsync(string path, IEnumerable<string> lines);
        void CopyFile(string sourceFile, string destinationFile, bool overwrite);
        DirectoryInfo CreateDirectory(string directoryPath);

        Stream CreateFile(string filePath);
        bool FileExists(string path);
        string[] GetFiles(string path, string searchPattern, EnumerationOptions enumOptions);

        void MoveFile(string sourceFile, string destinationFile, bool overwrite);
        string ReadAllText(string filePath);
        Task<string> ReadAllTextAsync(string path);
        void WriteAllText(string filePath, string contents);
        Task WriteAllTextAsync(string path, string content);
    }

    public class FileSystem : IFileSystem
    {
        public Task AppendAllLinesAsync(string path, IEnumerable<string> lines)
        {
            return File.AppendAllLinesAsync(path, lines);
        }

        public void CopyFile(string sourceFile, string destinationFile, bool overwrite)
        {
            File.Copy(sourceFile, destinationFile, overwrite);
        }

        public DirectoryInfo CreateDirectory(string directoryPath)
        {
            return Directory.CreateDirectory(directoryPath);
        }

        public Stream CreateFile(string filePath)
        {
            return File.Create(filePath);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumOptions)
        {
            return Directory.GetFiles(path, searchPattern, enumOptions);
        }

        public void MoveFile(string sourceFile, string destinationFile, bool overwrite)
        {
            File.Move(sourceFile, destinationFile, overwrite);
        }

        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public Task<string> ReadAllTextAsync(string path)
        {
            return File.ReadAllTextAsync(path);
        }

        public void WriteAllText(string filePath, string contents)
        {
            File.WriteAllText(filePath, contents);
        }

        public Task WriteAllTextAsync(string path, string content)
        {
            return File.WriteAllTextAsync(path, content);
        }
    }
}
