using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicketSystemClient
{
    public partial class LoginForm : Form
    {
        private CancellationTokenSource _cancellationTokenSource;
        private TextBox UserIdTextBox; // Поле для ввода логина пользователя
        private TextBox PasswordTextBox; // Поле для ввода пароля
        private TextBox ipTextBox; // Поле для ввода IP-адреса сервера
        private TextBox portTextBox; // Поле для ввода порта сервера
        private Button LoginButton; // Кнопка для выполнения входа
        private Button ExitButton; // Кнопка для выхода из программы
        private Label statusLabel; // Метка для отображения статуса приложения
        private bool isServAv = false;
        public static bool IsAuthenticated { get; private set; } = false; // Флаг, указывающий, выполнена ли аутентификация пользователя
        public LoginForm()
        {
            InitializeComponent(); // Инициализация стандартных компонентов формы
            InitializeCustomComponents(); // Инициализация пользовательских компонентов
        }
        /// <summary>
        /// Метод для инициализации пользовательских компонентов формы.
        /// Настраивает внешний вид формы и добавляет основные элементы управления, такие как текстовые поля, кнопки и метки.
        /// </summary>
        private void InitializeCustomComponents()
        {
            // Установка свойств формы
            this.MaximizeBox = false;
            this.Text = "Авторизация";
            this.Size = new System.Drawing.Size(400, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            // Заголовок формы
            var titleLabel = new Label
            {
                Text = "Авторизация",
                Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(50, 50, 50),
                Location = new System.Drawing.Point(100, 20),
                AutoSize = true
            };
            Controls.Add(titleLabel);
            // Label для User ID
            var userIdLabel = new Label
            {
                Text = "Логин",
                Location = new System.Drawing.Point(50, 80),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black,
                AutoSize = true
            };
            Controls.Add(userIdLabel);
            // TextBox для User ID
            UserIdTextBox = new TextBox
            {
                Location = new System.Drawing.Point(50, 110),
                Size = new System.Drawing.Size(300, 30),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Gray,
                Text = "Введите Логин",
                BorderStyle = BorderStyle.FixedSingle
            };
            UserIdTextBox.Enter += (s, e) => RemovePlaceholder(UserIdTextBox, "Введите Логин");
            UserIdTextBox.Leave += (s, e) => AddPlaceholder(UserIdTextBox, "Введите Логин");
            Controls.Add(UserIdTextBox);
            UserIdTextBox.KeyPress += TextBoxUserId_KeyPress;
            // Label для Password
            var passwordLabel = new Label
            {
                Text = "Пароль",
                Location = new System.Drawing.Point(50, 160),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black,
                AutoSize = true
            };
            Controls.Add(passwordLabel);
            // TextBox для Password
            PasswordTextBox = new TextBox
            {
                Location = new System.Drawing.Point(50, 190),
                Size = new System.Drawing.Size(300, 30),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Gray,
                Text = "Введите пароль",
                UseSystemPasswordChar = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            PasswordTextBox.Enter += (s, e) => RemovePlaceholder(PasswordTextBox, "Введите пароль");
            PasswordTextBox.Leave += (s, e) => AddPlaceholder(PasswordTextBox, "Введите пароль");
            Controls.Add(PasswordTextBox);
            // Label для IP
            var ipLabel = new Label
            {
                Text = "IP адрес",
                Location = new System.Drawing.Point(50, 240),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black,
                AutoSize = true
            };
            Controls.Add(ipLabel);
            // TextBox для IP
            ipTextBox = new TextBox
            {
                Location = new System.Drawing.Point(50, 270),
                Size = new System.Drawing.Size(300, 30),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black,
                Text = "Введите IP адрес сервера",
                BorderStyle = BorderStyle.FixedSingle
            };
            ipTextBox.Enter += (s, e) => RemovePlaceholder(ipTextBox, "Введите IP адрес сервера");
            ipTextBox.Leave += (s, e) => AddPlaceholder(ipTextBox, "Введите IP адрес сервера");
            Controls.Add(ipTextBox);
            // Label для Port
            var portLabel = new Label
            {
                Text = "Порт",
                Location = new System.Drawing.Point(50, 320),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black,
                AutoSize = true
            };
            Controls.Add(portLabel);
            // TextBox для Port
             portTextBox = new TextBox
             {
                Location = new System.Drawing.Point(50, 350),
                Size = new System.Drawing.Size(300, 30),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black,
                Text = "Введите порт сервера",
                BorderStyle = BorderStyle.FixedSingle
             };
            portTextBox.Enter += (s, e) => RemovePlaceholder(portTextBox, "Введите порт сервера");
            portTextBox.Leave += (s, e) => AddPlaceholder(portTextBox, "Введите порт сервера");
            Controls.Add(portTextBox);
            // Button для входа
            LoginButton = new Button
            {
                Text = "Войти",
                Location = new System.Drawing.Point(50, 400),
                Size = new System.Drawing.Size(130, 40),
                BackColor = System.Drawing.Color.FromArgb(33, 150, 243),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            LoginButton.FlatAppearance.BorderSize = 0;
            LoginButton.Click += LoginButton_Click;
            Controls.Add(LoginButton);
            // Button для выхода
            ExitButton = new Button
            {
                Text = "Выход",
                Location = new System.Drawing.Point(220, 400),
                Size = new System.Drawing.Size(130, 40),
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            ExitButton.FlatAppearance.BorderSize = 0;
            ExitButton.Click += ExitButton_Click;
            Controls.Add(ExitButton);
            // Статусный лейбл
            statusLabel = new Label
            {
                Text = "",
                Location = new System.Drawing.Point(50, 450),
                Size = new System.Drawing.Size(300, 40),
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = System.Drawing.Color.Transparent,
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Padding = new Padding(10)
            };
            Controls.Add(statusLabel);
        }
        private void UpdateStatusLabel(string message, Color textColor)
        {
            // Обновляем текст и цвет при каждом вызове
            statusLabel.Text = message;
            statusLabel.ForeColor = textColor;
            // Отменяем предыдущую задачу, если она существует
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            // Запускаем задачу для очистки текста
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(3000, token); // Ждём 4 секунды
                    if (!token.IsCancellationRequested)
                    {
                        statusLabel.Invoke(new Action(() => statusLabel.Text = string.Empty));
                    }
                }
                catch (TaskCanceledException)
                {
                    // Игнорируем отмену
                }
            });
        }
        private void TextBoxUserId_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Разрешаем ввод латинских букв и цифр, а также клавишу Backspace
            if ((e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z') || (e.KeyChar >= '0' && e.KeyChar <= '9') || e.KeyChar == (char)Keys.Back)
            {
                // Если символ в верхнем регистре, меняем его на нижний
                if (e.KeyChar >= 'A' && e.KeyChar <= 'Z')
                {
                    e.KeyChar = Char.ToLower(e.KeyChar); // Преобразуем в нижний регистр
                }
            }
            else
            {
                // Блокируем ввод, если символ не является латинской буквой, цифрой или Backspace
                e.Handled = true;
                UpdateStatusLabel("Только цифры и латинские буквы!", Color.Red);
            }
        }
        /// <summary>
        /// Отправляет запрос на сервер и возвращает ответ.
        /// Использует указанные или сохраненные IP-адрес и порт для подключения.
        /// </summary>
        private string SendRequestToServer(string request)
        {
            string ip = string.IsNullOrWhiteSpace(ipTextBox.Text.Trim()) ? SessionManager.LoadedIp : ipTextBox.Text.Trim();
            string portText = string.IsNullOrWhiteSpace(portTextBox.Text.Trim()) ? SessionManager.LoadedPort : portTextBox.Text.Trim();
            // Проверяем корректность IP и порта
            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(portText))
            {
            UpdateStatusLabel("Введите корректный IP-адрес и порт или убедитесь, что данные сохранены.!", Color.Red);
            return null;
            }
            if (!int.TryParse(portText, out int port))
            {
            UpdateStatusLabel("Порт должен быть числом!", Color.Red);
                return null;
            }
            try
            {
                // Используем указанные или сохраненные IP и порт для подключения
                using (var tcpClient = new TcpClient(ip, port))
                using (var networkStream = tcpClient.GetStream())
                using (var writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true })
                using (var reader = new StreamReader(networkStream, Encoding.UTF8))
                {
                    writer.WriteLine(request); // Отправляем запрос
                    return reader.ReadLine(); // Читаем ответ
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к серверу: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
        // Устанавливает плейсхолдер в текстовом поле, если оно пустое, 
        private void AddPlaceholder(TextBox textBox, string placeholder)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.ForeColor = System.Drawing.Color.Gray;
                if (textBox == PasswordTextBox)
                {
                    PasswordTextBox.UseSystemPasswordChar = false;
                }
            }
        }
        // Удаляет плейсхолдер из текстового поля, если он установлен, 
        private void RemovePlaceholder(TextBox textBox, string placeholder)
        {
            if (textBox.Text == placeholder && textBox.ForeColor == System.Drawing.Color.Gray)
            {
                textBox.Text = "";
                textBox.ForeColor = System.Drawing.Color.Black;
                if (textBox == PasswordTextBox)
                {
                    PasswordTextBox.UseSystemPasswordChar = true;
                }
            }
        }
        /// <summary>
        /// Обрабатывает событие нажатия кнопки "Войти". 
        /// Проверяет корректность введенных данных, отправляет запрос на сервер
        /// и выполняет действия в зависимости от ответа сервера.
        /// </summary>
        private void LoginButton_Click(object sender, EventArgs e)
        {
            string userId = UserIdTextBox.Text.Trim();
            string password = PasswordTextBox.Text.Trim();
            string serverIp = ipTextBox.Text.Trim();
            string serverPort = portTextBox.Text.Trim();
            // Проверка на пустые поля
            if (userId == "Введите User ID" || password == "Введите пароль" ||
                string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(serverIp) || string.IsNullOrWhiteSpace(serverPort))
            {
                UpdateStatusLabel("Введите корректные данные!", Color.Red);
                return;
            }
            // Проверка IP-адреса
            if (!IsValidIpAddress(serverIp))
            {
                UpdateStatusLabel("Некорректный IP-адрес!", Color.Red);
                return;
            }
            // Проверка порта
            if (!IsValidPort(serverPort))
            {
                UpdateStatusLabel("порт (1024-49151)!", Color.Red);
                return;
            }
            // Показываем статус "Идет вход в аккаунт, пожалуйста, подождите"
            UpdateStatusLabel("Идет вход в аккаунт!", Color.DarkGreen);
            string request = $"login,{userId},{password}";
            string response = null;

            try
            {
                // Отправка запроса на сервер
                response = SendRequestToServer(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке запроса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (response == null)
            {
                UpdateStatusLabel("Не удалось подключиться к серверу!", Color.Red);
                return;
            }

            if (response.StartsWith("OK"))
            {
                // Сохраняем данные в сессию
                SessionManager.SaveSession(userId, password, serverIp, serverPort);
                // Загружаем сессию после записи
                SessionManager.LoadSession();
                // Проверка роли пользователя и открытие соответствующей формы
                if (response.Contains("admin"))
                {
                   // MessageBox.Show("Добро пожаловать, Администратор!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AdminForm adminForm = new AdminForm();
                    adminForm.Show();
                }
                else
                {
                  //  MessageBox.Show("Вы вошли как пользователь.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UserForm userForm = new UserForm();
                    userForm.Show();
                }
                this.Hide(); // Скрываем форму входа
            }
            else
            {
                SessionManager.ClearSession();
                UpdateStatusLabel("Неверный логин или пароль", Color.Red);
            }
        }
        // Метод для проверки корректности IP-адреса
        private bool IsValidIpAddress(string ipAddress)
        {
            if (System.Net.IPAddress.TryParse(ipAddress, out _))
            {
                return true;
            }
            return false;
        }
        // Метод для проверки корректности порта
        private bool IsValidPort(string port)
        {
            // Проверяем, что порт является числом и в диапазоне от 1024 до 49151
            if (int.TryParse(port, out int portNumber) && portNumber >= 1024 && portNumber <= 49151)
            {
                return true;
            }
            else
            {
                // Если порт некорректен, выводим сообщение об ошибке
                UpdateStatusLabel("порт (1024-49151)!", Color.Red);
                return false;
            }
        }
        //Кнопка выхода из приложения
        private void ExitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        // Асинхронный метод для проверки доступности сервера
        private async Task<bool> IsServerAvailableAsync(string ip, string port)
        {
            try
            {
                // Преобразуем порт в число
                int portNumber = int.Parse(port);

                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(ip, portNumber);
                    if (await Task.WhenAny(connectTask, Task.Delay(2000)) == connectTask)
                    {
                        return true; // Если подключение успешно
                    }
                    else
                    {
                        return false; // Если не удалось подключиться за 2000 миллисекунд
                    }
                }
            }
            catch (Exception ex)
            {
                // Если ошибка соединения
                MessageBox.Show($"Ошибка при подключении к серверу: {ex.Message}");
                return false; // Если не удалось подключиться
            }
        }
        private async void LoginForm_Load(object sender, EventArgs e)
        {
            // Загружаем сохраненные данные сессии
            SessionManager.LoadSession();
            // Проверяем, существует ли сохраненная сессия и заполнены ли данные
            if (SessionManager.HasSavedCredentials() &&
                !string.IsNullOrWhiteSpace(SessionManager.LoadedUserId) &&
                !string.IsNullOrWhiteSpace(SessionManager.LoadedPassword) &&
                !string.IsNullOrWhiteSpace(SessionManager.LoadedIp) &&
                !string.IsNullOrWhiteSpace(SessionManager.LoadedPort))
            {
                string userId = SessionManager.LoadedUserId;
                string password = SessionManager.LoadedPassword;
                string ip = SessionManager.LoadedIp;
                string port = SessionManager.LoadedPort;
                bool serverAvailable = await IsServerAvailableAsync(ip, port);
                if (!serverAvailable)
                {
                    // Диалоговое окно, если сервер не доступен
                    var dialogResult = MessageBox.Show(
                        "Не удалось подключиться к серверу. Сервер отключен или возможно изменился IP адрес. Хотите ввести данные вручную (Да), попробовать заново (Нет) или выйти из программы?(Отмена)",
                        "Ошибка подключения",
                        MessageBoxButtons.YesNoCancel, // 3 кнопки: Ввести вручную (Yes), Попробовать снова (No), Выйти (Cancel)
                        MessageBoxIcon.Error
                    );
                    if (dialogResult == DialogResult.Yes)
                    {
                        UserIdTextBox.Text= userId;
                        isServAv = true;
                        PasswordTextBox.UseSystemPasswordChar = true;
                        PasswordTextBox.Text= password;
                        this.Show();
                        // Переходим к вводу данных вручную
                        UserIdTextBox.Focus();
                        ipTextBox.Text = ip;
                        portTextBox.Text = port;
                        return; // Прерываем выполнение метода
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        // Пытаемся снова подключиться к серверу
                        await TryToConnectAsync(userId, password, ip, port);
                        return;
                    }
                    else if (dialogResult == DialogResult.Cancel)
                    {
                        // Закрываем программу
                        Application.Exit();
                        return;
                    }
                }
                // Если сервер доступен, продолжаем процесс авторизации
                string request = $"login,{userId},{password}";
                string response = null;
                try
                {
                    // Устанавливаем сохраненные IP и порт
                    ipTextBox.Text = ip;
                    portTextBox.Text = port;

                    response = SendRequestToServer(request);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отправке запроса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (response != null && response.StartsWith("OK"))
                {
                    if (response.Contains("admin"))
                    {
                        AdminForm adminForm = new AdminForm();
                        adminForm.Show();
                    }
                    else
                    {
                        UserForm userForm = new UserForm();
                        userForm.Show();
                    }

                    IsAuthenticated = true;
                    // Скрываем форму входа после успешной авторизации
                    this.Hide();
                }
                else
                {
                    IsAuthenticated = false;
                    MessageBox.Show($"Не удалось автоматически войти. Ответ сервера: {response}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                IsAuthenticated = false;
                // Если сессия не сохранена, показываем форму для ввода данных вручную
                MessageBox.Show("Нет сохраненных данных для входа. Пожалуйста, войдите вручную.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Устанавливаем фокус на поле ввода User ID
                UserIdTextBox.Focus();
                ipTextBox.Text = "127.0.0.1";
                portTextBox.Text = "5500";
                isServAv = true;
                this.Show();
            }
          
        }
        // Асинхронная попытка повторного подключения
        private async Task TryToConnectAsync(string userId, string password, string ip, string port)
        {
            bool serverAvailable = await IsServerAvailableAsync(ip, port);

            if (serverAvailable)
            {
                // Если сервер доступен, продолжаем процесс авторизации
                string request = $"login,{userId},{password}";
                string response = null;
                try
                {
                    // Устанавливаем сохраненные IP и порт
                    ipTextBox.Text = ip;
                    portTextBox.Text = port;

                    response = SendRequestToServer(request);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отправке запроса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (response != null && response.StartsWith("OK"))
                {
                    if (response.Contains("admin"))
                    {
                        AdminForm adminForm = new AdminForm();
                        adminForm.Show();
                    }
                    else
                    {
                        UserForm userForm = new UserForm();
                        userForm.Show();
                    }

                    IsAuthenticated = true;
                    // Скрываем форму входа после успешной авторизации
                    this.Hide();
                }
                else
                {
                    IsAuthenticated = false;
                    MessageBox.Show($"Не удалось автоматически войти. Ответ сервера: {response}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // Если сервер не доступен после попытки, показываем сообщение
                MessageBox.Show("Сервер по-прежнему недоступен. Попробуйте позже.", "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
        // Проверяет, запущено ли приложение с правами администратора, и при необходимости перезапускает его с повышенными правами.
        private void LoginForm_Shown(object sender, EventArgs e)
        {
            this.Hide();
            // Проверяем, прошла ли авторизация
            if (IsAuthenticated)
            {
                this.Hide(); // Скрываем форму, если авторизация прошла
            }
            else
            {
                if (isServAv == true)
                {
                    this.Show();
                
                }
            }
        }
    }
}
