using System;
using System.IO;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    /// <summary>
    /// Central location for all app data files.
    /// Uses %APPDATA%\HomeTVStation so data survives installs/updates
    /// and is never blocked by UAC in Program Files.
    /// </summary>
    public static class DataFolder
    {
        private static string _path;

        public static string Path
        {
            get
            {
                if (_path != null) return _path;

                _path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HomeTVStation");

                // Create the folder if it doesn't exist yet
                Directory.CreateDirectory(_path);
                return _path;
            }
        }

        /// <summary>Returns the full path for a data file inside the app data folder.</summary>
        public static string File(string fileName) =>
            System.IO.Path.Combine(Path, fileName);
    }
}
