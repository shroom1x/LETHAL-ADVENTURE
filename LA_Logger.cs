using System;
using System.IO;
using System.Reflection;

namespace LA_Changeloger
{
    public static class LA_Logger
    {
        private static string logFilePath;

        static LA_Logger()
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                string logsFolderPath = Path.Combine(assemblyDir, "logs");

                if (!Directory.Exists(logsFolderPath))
                {
                    Directory.CreateDirectory(logsFolderPath);
                }

                logFilePath = Path.Combine(logsFolderPath, "log.txt");

                File.WriteAllText(logFilePath, string.Empty);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LALogger] Не удалось создать папку или файл лога: {ex.Message}");
            }
        }

        public static void Initialize() { }
    }
}