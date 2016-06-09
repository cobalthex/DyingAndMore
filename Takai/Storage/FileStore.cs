using Microsoft.Xna.Framework;
using System.IO;
#if !ZUNE
using System.IO.IsolatedStorage;
#endif

namespace Takai.Storage
{
    /// <summary>
    /// Where the file is saved to/read from
    /// </summary>
    public enum FileLocation
    {
        /// <summary>
        /// The application folder (generally read-only)
        /// </summary>
        Title,
        /// <summary>
        /// User storage 
        /// *Windows Phone & Windows revert to isolated storage
        /// </summary>
        User,
        /// <summary>
        /// Isolated storage (or sometimes user storage)
        /// </summary>
        Isolated
    }

    /// <summary>
    /// An abstracted file io system
    /// </summary>
    public static class FileStore
    {
#if !ZUNE
        static IsolatedStorageFile isoStore;
#endif

        static void Init()
        {
#if WINDOWS
            isoStore = IsolatedStorageFile.GetUserStoreForAssembly();
#elif !ZUNE
            isoStore = IsolatedStorageFile.GetUserStoreForApplication();
#endif
        }

        public static StreamReader ReadFile(string file, FileLocation location)
        {
#if ZUNE
            if (location == FileLocation.Isolated)
                location = FileLocation.User;
#endif

            try
            {
                if (location == FileLocation.Title)
                    return new StreamReader(TitleContainer.OpenStream(file));
                else if (location == FileLocation.User)
                {
#if WINDOWS_PHONE || WINDOWS
                    if (isoStore == null)
                        Init();

                    return new StreamReader(new IsolatedStorageFileStream(file, FileMode.Open, isoStore));
#elif XBOX

#elif ZUNE
            
#endif
                }
                else if (location == FileLocation.Isolated)
                {
#if !ZUNE
                    if (isoStore == null)
                        Init();

                    return new StreamReader(new IsolatedStorageFileStream(file, FileMode.Open, isoStore));
#endif
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Open a file for writing
        /// </summary>
        /// <param name="file">The file to write to</param>
        /// <param name="location">The location of the file</param>
        /// <param name="append">Should the file be appended to?</param>
        /// <returns>A stream for writing to</returns>
        public static StreamWriter WriteFile(string file, FileLocation location, bool append)
        {
#if ZUNE
            if (location == FileLocation.Isolated)
                location = FileLocation.User;
#endif

            try
            {
                if (location == FileLocation.Title)
                {
#if !WINDOWS_PHONE
                    StreamWriter sw = new StreamWriter(TitleContainer.OpenStream(file));
                    if (!append)
                        sw.BaseStream.Seek(0, SeekOrigin.Begin);
                    return sw;
#endif
                }
                else if (location == FileLocation.User)
                {
#if WINDOWS

#elif WINDOWS_PHONE
                    if (isoStore == null)
                        Init();

                    return new StreamWriter(new IsolatedStorageFileStream(file, append ? FileMode.Append : FileMode.Create, isoStore));
#elif XBOX

#elif ZUNE
            
#endif
                }
                else if (location == FileLocation.Isolated)
                {
#if !ZUNE
                    if (isoStore == null)
                        Init();

                    return new StreamWriter(new IsolatedStorageFileStream(file, append ? FileMode.Append : FileMode.Create, isoStore));
#endif
                }
            }
            catch { throw; }

            return null;
        }

        public static Stream OpenStream(string file, FileLocation location)
        {
#if ZUNE
            if (location == FileLocation.Isolated)
                location = FileLocation.User;
#endif

            try
            {
                if (location == FileLocation.Title)
                {
#if !WINDOWS_PHONE
                    return TitleContainer.OpenStream(file);
#endif
                }
                else if (location == FileLocation.User)
                {
#if WINDOWS

#elif WINDOWS_PHONE
                    if (isoStore == null)
                        Init();

                    return new IsolatedStorageFileStream(file, FileMode.OpenOrCreate, isoStore);
#elif XBOX

#elif ZUNE
            
#endif
                }
                else if (location == FileLocation.Isolated)
                {
#if !ZUNE
                    if (isoStore == null)
                        Init();

                    return new IsolatedStorageFileStream(file, FileMode.OpenOrCreate, isoStore);
#endif
                }
            }
            catch { throw; }

            return null;
        }

        /// <summary>
        /// Does a file exist at a specific location?
        /// </summary>
        /// <param name="file">The file to check</param>
        /// <param name="location">The location to check</param>
        /// <returns>True if the file exists</returns>
        public static bool FileExists(string file, FileLocation location)
        {
#if ZUNE
            if (location == FileLocation.Isolated)
                location = FileLocation.User;
#endif
            try
            {
                if (location == FileLocation.Title)
                    return File.Exists(file);
                else if (location == FileLocation.User)
#if WINDOWS
                    return File.Exists(System.Environment.SpecialFolder.ApplicationData + file);
#else
                    return false;
#endif
                else if (location == FileLocation.Isolated)
                {
#if !ZUNE
                    if (isoStore == null)
                        Init();

                    return isoStore.FileExists(file);
#endif
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="location"></param>
        /// <returns>True if successful in deleting the file, false if it does not exist or an error occurs</returns>
        public static bool DeleteFile(string file, FileLocation location)
        {
#if ZUNE
            if (location == FileLocation.Isolated)
                location = FileLocation.User;
#endif
            try
            {
                if (location == FileLocation.Title)
                    File.Delete(file);
                else if (location == FileLocation.User)
#if WINDOWS
                    File.Delete(System.Environment.SpecialFolder.ApplicationData + file);
#else
                    ;
#endif
#if !ZUNE
                else if (location == FileLocation.Isolated)
                    isoStore.DeleteFile(file);
#endif

                return true;
            }
            catch { }

            return false;
        }
    }
}
