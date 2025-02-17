using System;
using System.IO;

public class SessionManager
{
    private readonly string sessionFilePath;

    // Конструктор, который вычисляет путь, если параметр не передан
    public SessionManager(string filePath = null)
    {
        // Если путь не передан, используем стандартную папку LocalApplicationData (AppData)
        if (filePath == null)
        {
            sessionFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TicketSystemServer", "settings.txt");
            // Убедимся, что папка существует
            Directory.CreateDirectory(Path.GetDirectoryName(sessionFilePath)); // Создаём папку, если она не существует
        }
        else
        {
            sessionFilePath = filePath;
        }
    }

    // Сохранение данных сессии
    public void SaveSession(string ip, string port)
    {
        try
        {
            File.WriteAllText(sessionFilePath, $"{ip}:{port}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при сохранении сессии: {ex.Message}");
        }
    }

    // Загрузка данных сессии
    public (string ip, string port)? LoadSession()
    {
        try
        {
            if (File.Exists(sessionFilePath))
            {
                string[] sessionData = File.ReadAllText(sessionFilePath).Split(':');
                if (sessionData.Length == 2)
                {
                    return (sessionData[0], sessionData[1]);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при загрузке сессии: {ex.Message}");
        }
        return null; // Если сессия не существует или произошла ошибка
    }

    // Очистка данных сессии
    public void ClearSession()
    {
        try
        {
            if (File.Exists(sessionFilePath))
            {
                File.Delete(sessionFilePath);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при очистке сессии: {ex.Message}");
        }
    }
}
