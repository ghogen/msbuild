﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Build.Shared.FileSystem;

#nullable disable

namespace Microsoft.Build.Shared
{
    /// <summary>
    /// This class contains utility methods for file IO.
    /// It is in a separate file so that it can be selectively included into an assembly.
    /// </summary>
    internal static partial class FileUtilities
    {
        private static string tempFileDirectory = Path.GetTempPath();
        internal static string TempFileDirectory
        {
            get
            {
                if (BuildEnvironmentHelper.Instance.RunningTests)
                {
                    return Path.GetTempPath();
                }

                return tempFileDirectory;
            }
        }

        /// <summary>
        /// Generates a unique directory name in the temporary folder.
        /// Caller must delete when finished.
        /// </summary>
        /// <param name="createDirectory"></param>
        /// <param name="subfolder"></param>
        internal static string GetTemporaryDirectory(bool createDirectory = true, string subfolder = null)
        {
            string temporaryDirectory = Path.Combine(Path.Combine(TempFileDirectory, "Temporary" + Guid.NewGuid().ToString("N")), subfolder ?? string.Empty);

            if (createDirectory)
            {
                Directory.CreateDirectory(temporaryDirectory);
            }

            return temporaryDirectory;
        }

        /// <summary>
        /// Generates a unique temporary file name with a given extension in the temporary folder.
        /// File is guaranteed to be unique.
        /// Extension may have an initial period.
        /// File will NOT be created.
        /// May throw IOException.
        /// </summary>
        internal static string GetTemporaryFileName(string extension)
        {
            return GetTemporaryFile(null, null, extension, false);
        }

        /// <summary>
        /// Generates a unique temporary file name with a given extension in the temporary folder.
        /// If no extension is provided, uses ".tmp".
        /// File is guaranteed to be unique.
        /// Caller must delete it when finished.
        /// </summary>
        internal static string GetTemporaryFile()
        {
            return GetTemporaryFile(".tmp");
        }

        /// <summary>
        /// Generates a unique temporary file name with a given extension in the temporary folder.
        /// File is guaranteed to be unique.
        /// Caller must delete it when finished.
        /// </summary>
        internal static string GetTemporaryFile(string fileName, string extension, bool createFile)
        {
            return GetTemporaryFile(null, fileName, extension, createFile);
        }

        /// <summary>
        /// Generates a unique temporary file name with a given extension in the temporary folder.
        /// File is guaranteed to be unique.
        /// Extension may have an initial period.
        /// Caller must delete it when finished.
        /// May throw IOException.
        /// </summary>
        internal static string GetTemporaryFile(string extension)
        {
            return GetTemporaryFile(null, null, extension);
        }

        /// <summary>
        /// Creates a file with unique temporary file name with a given extension in the specified folder.
        /// File is guaranteed to be unique.
        /// Extension may have an initial period.
        /// If folder is null, the temporary folder will be used.
        /// Caller must delete it when finished.
        /// May throw IOException.
        /// </summary>
        internal static string GetTemporaryFile(string directory, string fileName, string extension, bool createFile = true)
        {
            ErrorUtilities.VerifyThrowArgumentLengthIfNotNull(directory, nameof(directory));

            try
            {
                directory ??= TempFileDirectory;

                // If the extension needs a dot prepended, do so.
                if (extension is null)
                {
                    extension = string.Empty;
                }
                else if (extension.Length > 0 && extension[0] != '.')
                {
                    extension = '.' + extension;
                }

                // If the fileName is null, use tmp{Guid}; otherwise use fileName.
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"tmp{Guid.NewGuid():N}";
                }

                Directory.CreateDirectory(directory);

                string file = Path.Combine(directory, $"{fileName}{extension}");

                ErrorUtilities.VerifyThrow(!FileSystems.Default.FileExists(file), "Guid should be unique");

                if (createFile)
                {
                    File.WriteAllText(file, string.Empty);
                }

                return file;
            }
            catch (Exception ex) when (ExceptionHandling.IsIoRelatedException(ex))
            {
                throw new IOException(ResourceUtilities.FormatResourceStringStripCodeAndKeyword("Shared.FailedCreatingTempFile", ex.Message), ex);
            }
        }

        internal static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);

            DirectoryInfo sourceInfo = new DirectoryInfo(source);
            foreach (var fileInfo in sourceInfo.GetFiles())
            {
                string destFile = Path.Combine(dest, fileInfo.Name);
                fileInfo.CopyTo(destFile);
            }
            foreach (var subdirInfo in sourceInfo.GetDirectories())
            {
                string destDir = Path.Combine(dest, subdirInfo.Name);
                CopyDirectory(subdirInfo.FullName, destDir);
            }
        }

        public class TempWorkingDirectory : IDisposable
        {
            public string Path { get; }

            public TempWorkingDirectory(string sourcePath,
#if !CLR2COMPATIBILITY
                [CallerMemberName]
#endif
            string name = null)
            {
                Path = name == null
                    ? GetTemporaryDirectory()
                    : System.IO.Path.Combine(TempFileDirectory, name);

                if (FileSystems.Default.DirectoryExists(Path))
                {
                    Directory.Delete(Path, true);
                }

                CopyDirectory(sourcePath, Path);
            }

            public void Dispose()
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
