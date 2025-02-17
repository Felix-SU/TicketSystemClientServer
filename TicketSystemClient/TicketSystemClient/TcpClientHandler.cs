using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;

public class TcpClientHandler
{
    private readonly string _ip;
    private readonly int _port;
    private TcpClient _client;
    private NetworkStream _stream;

    public TcpClientHandler(string ip, int port)
    {
        _ip = ip;
        _port = port;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_ip, _port);
            _stream = _client.GetStream();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    public async Task<string> SendRequestAsync(string request)
    {
        try
        {
            if (_client == null || !_client.Connected)
                throw new InvalidOperationException("Нет подключения к серверу.");

            // Отправляем запрос
            byte[] data = Encoding.UTF8.GetBytes(request);
            await _stream.WriteAsync(data, 0, data.Length);

            // Получаем ответ
            byte[] buffer = new byte[1024];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при отправке запроса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
    }

    public void Disconnect()
    {
        _stream?.Close();
        _client?.Close();
    }
}
