/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * Copyright (c) 2018-2019 Andreas Truetschel and contributors.
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * --------------------------------------------------------------------------------------------------------------------
 */

/* Based on
 * --------------------------------------------------------------------------------------------------------------------
 * corefx (https://github.com/dotnet/corefx)
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils
{
    // TODO: Fix XML-comments
    public static class ZipArchiveEntryExtension
    {
        /// <summary>
        /// Creates a file on the file system with the entry?s contents and the specified name. The last write time of the file is set to the
        /// entry?s last write time. This method does not allow overwriting of an existing file with the same name. Attempting to extract explicit
        /// directories (entries with names that end in directory separator characters) will not result in the creation of a directory.
        /// </summary>
        /// 
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">destinationFileName is a zero-length string, contains only whitespace, or contains one or more
        /// invalid characters as defined by InvalidPathChars. -or- destinationFileName specifies a directory.</exception>
        /// <exception cref="ArgumentNullException">destinationFileName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in destinationFileName is invalid (for example, it is on
        /// an unmapped drive).</exception>
        /// <exception cref="IOException">destinationFileName already exists.
        /// -or- An I/O error has occurred. -or- The entry is currently open for writing.
        /// -or- The entry has been deleted from the archive.</exception>
        /// <exception cref="NotSupportedException">destinationFileName is in an invalid format
        /// -or- The ZipArchive that this entry belongs to was opened in a write-only mode.</exception>
        /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read
        /// -or- The entry has been compressed using a compression method that is not supported.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
        /// 
        /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.
        /// The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        public static Task ExtractToFileAsync(this ZipArchiveEntry source, string destinationFileName, CancellationToken cancellation)
        {
            return ExtractToFileAsync(source, destinationFileName, false, cancellation);
        }

        /// <summary>
        /// Creates a file on the file system with the entry?s contents and the specified name.
        /// The last write time of the file is set to the entry?s last write time.
        /// This method does allows overwriting of an existing file with the same name.
        /// </summary>
        /// 
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">destinationFileName is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by InvalidPathChars. -or- destinationFileName specifies a directory.</exception>
        /// <exception cref="ArgumentNullException">destinationFileName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in destinationFileName is invalid
        /// (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">destinationFileName exists and overwrite is false.
        /// -or- An I/O error has occurred.
        /// -or- The entry is currently open for writing.
        /// -or- The entry has been deleted from the archive.</exception>
        /// <exception cref="NotSupportedException">destinationFileName is in an invalid format
        /// -or- The ZipArchive that this entry belongs to was opened in a write-only mode.</exception>
        /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read
        /// -or- The entry has been compressed using a compression method that is not supported.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
        /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.
        /// The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        /// <param name="overwrite">True to indicate overwrite.</param>
        public static async Task ExtractToFileAsync(this ZipArchiveEntry source, string destinationFileName, bool overwrite, CancellationToken cancellation)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            // Rely on FileStream's ctor for further checking destinationFileName parameter
            var fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;

            using (var fileStream = new FileStream(destinationFileName, fileMode, FileAccess.Write, FileShare.None, bufferSize: 0x1000, useAsync: true))
            {
                using (var zipStream = source.Open())
                {
                    await zipStream.CopyToAsync(fileStream, bufferSize: 81920, cancellation);
                }

            }

            File.SetLastWriteTime(destinationFileName, source.LastWriteTime.DateTime);
        }

        internal static Task ExtractRelativeToDirectoryAsync(this ZipArchiveEntry source, string destinationDirectoryName, CancellationToken cancellation)
        {
            return ExtractRelativeToDirectoryAsync(source, destinationDirectoryName, overwrite: false, cancellation);
        }

        internal static Task ExtractRelativeToDirectoryAsync(this ZipArchiveEntry source, string destinationDirectoryName, bool overwrite, CancellationToken cancellation)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
            var di = Directory.CreateDirectory(destinationDirectoryName);
            var destinationDirectoryFullPath = di.FullName;
            var fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, source.FullName));

            if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, PathInternal.StringComparison))
                throw new IOException("Extracting Zip entry would have resulted in a file outside the specified destination directory.");

            if (Path.GetFileName(fileDestinationPath).Length != 0)
            {
                // If it is a file:
                // Create containing directory:
                Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath));
                return source.ExtractToFileAsync(fileDestinationPath, overwrite: overwrite, cancellation);
            }

            // If it is a directory:
            if (source.Length != 0)
                throw new IOException("Zip entry name ends in directory separator character but contains data.");

            Directory.CreateDirectory(fileDestinationPath);
            return Task.CompletedTask;
        }
    }
}

