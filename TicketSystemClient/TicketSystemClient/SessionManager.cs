using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace TicketSystemClient
{
    public static class SessionManager
    {
        private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TicketSystemClient",
            "session.txt"
        );

        // Свойства для хранения загруженных данных
        public static string LoadedUserId { get; private set; }
        public static string LoadedPasswordHash { get; private set; }
        public static string LoadedPassword { get; private set; }
        public static string LoadedIp { get; private set; }
        public static string LoadedPort { get; private set; }

        private static readonly object SessionFileLock = new object();

        /// <summary>
        /// Сохраняет данные пользователя в файл.
        /// </summary>
        public static void SaveSession(string userId, string password, string ip, string port)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(SessionFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string hashedPassword = HashPassword(password);

                lock (SessionFileLock)
                {
                    using (FileStream fs = new FileStream(SessionFilePath, FileMode.Create, FileAccess.Write))
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"{userId},{hashedPassword},{password},{ip},{port}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения сессии: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Загружает данные сессии из файла.
        /// </summary>
        public static void LoadSession()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    lock (SessionFileLock)
                    {
                        string[] sessionData = File.ReadAllText(SessionFilePath).Split(',');

                        if (sessionData.Length == 5)
                        {
                            LoadedUserId = sessionData[0];
                            LoadedPasswordHash = sessionData[1];
                            LoadedPassword = sessionData[2];
                            LoadedIp = sessionData[3];
                            LoadedPort = sessionData[4];
                        }
                        else
                        {
                            ClearSession();
                        }
                    }
                }
                else
                {
                    ClearSession();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке сессии: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearSession();
            }
        }

        /// <summary>
        /// Проверяет, есть ли сохраненные данные пользователя.
        /// </summary>
        public static bool HasSavedCredentials()
        {
            return !string.IsNullOrWhiteSpace(LoadedUserId) &&
                   !string.IsNullOrWhiteSpace(LoadedPassword) &&
                   !string.IsNullOrWhiteSpace(LoadedIp) &&
                   !string.IsNullOrWhiteSpace(LoadedPort);
        }

        /// <summary>
        /// Удаляет файл сессии.
        /// </summary>
        public static void ClearSession()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                }
                LoadedUserId = null;
                LoadedPasswordHash = null;
                LoadedPassword = null;
                LoadedIp = null;
                LoadedPort = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка очистки сессии: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Хеширует пароль с помощью SHA256.
        /// </summary>
        private static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
