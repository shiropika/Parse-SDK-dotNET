using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Internal.Utilities
{
    /// <summary>
    /// A collection of utility methods and properties for writing to the app-specific persistent storage folder.
    /// </summary>
    internal static class StorageManager
    {
        static StorageManager() => AppDomain.CurrentDomain.ProcessExit += (_, __) => { if (new FileInfo(FallbackPersistentStorageFilePath) is FileInfo file && file.Exists) file.Delete(); };

        /// <summary>
        /// The path to a persistent user-specific storage location specific to the final client assembly of the Parse library.
        /// </summary>
        public static string PersistentStorageFilePath => Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ParseClient.CurrentConfiguration.StorageConfiguration?.RelativeStorageFilePath ?? FallbackPersistentStorageFilePath));

        /// <summary>
        /// Gets the calculated persistent storage file fallback path for this app execution.
        /// </summary>
        public static string FallbackPersistentStorageFilePath { get; } = ParseClient.Configuration.IdentifierBasedStorageConfiguration.Fallback.RelativeStorageFilePath;

        /// <summary>
        /// Asynchronously writes the provided little-endian 16-bit character string <paramref name="content"/> to the file wrapped by the provided <see cref="FileInfo"/> instance.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> instance wrapping the target file that is to be written to</param>
        /// <param name="content">The little-endian 16-bit Unicode character string (UTF-16) that is to be written to the <paramref name="file"/></param>
        /// <returns>A task that completes once the write operation to the <paramref name="file"/> completes</returns>
        public static async Task WriteToAsync(this FileInfo file, string content)
        {
            using (FileStream stream = new FileStream(Path.GetFullPath(file.FullName), FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous))
                await stream.WriteAsync(Encoding.Unicode.GetBytes(content), 0, content.Length * 2 /* UTF-16, so two bytes per character of length. */);
        }

        /// <summary>
        /// Asynchronously read all of the little-endian 16-bit character units (UTF-16) contained within the file wrapped by the provided <see cref="FileInfo"/> instance.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> instance wrapping the target file that string content is to be read from</param>
        /// <returns>A task that should contain the little-endian 16-bit character string (UTF-16) extracted from the <paramref name="file"/> if the read completes successfully</returns>
        public static async Task<string> ReadAllTextAsync(this FileInfo file)
        {
            using (StreamReader reader = file.OpenText())
                return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Gets or creates the file pointed to by <see cref="PersistentStorageFilePath"/> and returns it's wrapper as a <see cref="FileInfo"/> instance.
        /// </summary>
        public static FileInfo PersistentStorageFileWrapper
        {
            get
            {
                Console.WriteLine("PersistentStorageFilePath=" + PersistentStorageFilePath);
                string dir = PersistentStorageFilePath.Substring(0, PersistentStorageFilePath.LastIndexOf(Path.DirectorySeparatorChar) + 1);

                Console.WriteLine("PersistentStorageFilePath dir=" + dir);
                if (!Directory.Exists(dir))
                {
                    Console.WriteLine("PersistentStorageFilePath CreateDirectory=" + dir);
                    Directory.CreateDirectory(dir);
                }

                FileInfo file = new FileInfo(PersistentStorageFilePath);
                if (!file.Exists)
                    using (file.Create())
#pragma warning disable CS0642 // Possible mistaken empty statement
                        ; // Hopefully the JIT doesn't no-op this. The behaviour of the "using" clause should dictate how the stream is closed, to make sure it happens properly.
#pragma warning restore CS0642 // Possible mistaken empty statement

                return file;
            }
        }

        /// <summary>
        /// Gets the file wrapper for the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The relative path to the target file</param>
        /// <returns>An instance of <see cref="FileInfo"/> wrapping the the <paramref name="path"/> value</returns>
        public static FileInfo GetWrapperForRelativePersistentStorageFilePath(string path)
        {
            Console.WriteLine("GetWrapperForRelativePersistentStorageFilePath path=" + path);

            path = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path));

            Console.WriteLine("GetWrapperForRelativePersistentStorageFilePath path2=" + path);

            //Directory.CreateDirectory(path.Substring(0, path.LastIndexOf(Path.VolumeSeparatorChar)));
            path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar) + 1);

            Console.WriteLine("GetWrapperForRelativePersistentStorageFilePath path3=" + path);

            if (!Directory.Exists(path))
            {
                Console.WriteLine("GetWrapperForRelativePersistentStorageFilePath CreateDirectory=" + path);
                Directory.CreateDirectory(path);
            }

            return new FileInfo(path);
        }

        public static async Task TransferAsync(string originFilePath, string targetFilePath)
        {
            if (!String.IsNullOrWhiteSpace(originFilePath) && !String.IsNullOrWhiteSpace(targetFilePath) && new FileInfo(originFilePath) is FileInfo originFile && originFile.Exists && new FileInfo(targetFilePath) is FileInfo targetFile)
                using (StreamWriter writer = targetFile.CreateText())
                using (StreamReader reader = originFile.OpenText())
                    await writer.WriteAsync(await reader.ReadToEndAsync());
        }
    }
}
