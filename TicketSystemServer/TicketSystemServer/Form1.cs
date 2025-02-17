using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicketSystemServer
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ComboBox comboBoxChangeUserId; // Поле для выбора ID пользователя для изменения.
        private ListBox listBoxLogs; // Поле для отображения логов приложения.
        private TextBox textBoxUserId; // Поле для ввода логина пользователя.
        private TextBox textBoxPassword; // Поле для ввода пароля пользователя.
        private TextBox textBoxUserName; // Поле для ввода имени пользователя.
        private ComboBox comboBoxRole; // Поле для выбора роли пользователя (например, "user" или "admin").
        private Button buttonAddUser; // Кнопка для добавления нового пользователя.
        private ComboBox comboBoxChangeRole; // Поле для выбора новой роли пользователя.
        private SessionManager sessionManager; // Менеджер сессий для работы с сохранёнными данными.
        private Button buttonChangeRole; // Кнопка для изменения роли пользователя.
        private string connectionString = "Data Source=localhost;Initial Catalog=TicketSystem;Integrated Security=True;"; // Строка подключения к базе данных.
        private TcpListener tcpListener; // Слушатель TCP соединений для работы сервера.
        private TextBox textBoxIp; // Поле для ввода IP-адреса сервера.
        private TextBox textBoxPort; // Поле для ввода порта сервера.
        private Label statusLabel; // Метка для отображения текущего статуса сервера.
        private NotifyIcon trayIcon; // Иконка для отображения статуса приложения в системном трее.
        private ContextMenuStrip trayContextMenu; // Контекстное меню для иконки в трее.
        private CheckBox checkBoxTray; // Флажок для управления отображением приложения в трее.
        public Form1()
        {
            sessionManager = new SessionManager();
            this.Load += Form1_Load;
        }

        /// <summary>
        /// Инициализирует и настраивает элементы управления формы, включая параметры формы,
        /// элементы интерфейса для работы с пользователями, управление сервером и настройками.
        /// </summary>
        private void SetupForm()
        {
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Size = new System.Drawing.Size(795, 600);
            this.Text = "Управление Заявками Сервер";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ShowInTaskbar = true;
            // Если иконка уже существует, удалим ее
            if (trayIcon == null)
            {
                // Инициализация NotifyIcon только один раз
                trayIcon = new NotifyIcon
                {

                    Icon = this.Icon,  // Иконка формы
                    Visible = false,    // Изначально скрыта
                    BalloonTipTitle = "Сервер",
                    BalloonTipText = "Сервер работает в фоновом режиме",
                    BalloonTipIcon = ToolTipIcon.Info
                };
            }
            // Устанавливаем текст
            trayIcon.Text = "Управление Заявками Сервер";  // Устанавливаем новый текст
            // Показать подсказку
            trayIcon.DoubleClick += TrayIcon_DoubleClick;
            // Создание контекстного меню для иконки в трее
            trayContextMenu = new ContextMenuStrip();
            trayContextMenu.Items.Add("Показать", null, ShowFormFromTray);
            trayContextMenu.Items.Add("Закрыть", null, CloseAppFromTray);
            trayIcon.ContextMenuStrip = trayContextMenu;
            // Логи
            listBoxLogs = new ListBox
            {
                Location = new System.Drawing.Point(20, 20), // Позиция на форме
                Size = new System.Drawing.Size(740, 250), // Размер (ширина и высота)
                Font = new System.Drawing.Font("Segoe UI", 10), // Шрифт текста
                ForeColor = System.Drawing.Color.Black, // Цвет текста
                BackColor = System.Drawing.Color.FromArgb(245, 245, 245), // Цвет фона
                // Включить горизонтальную прокрутку (если нужно)
                ScrollAlwaysVisible = false,
                // Прокрутка отображается только при необходимости
                IntegralHeight = false // Для корректной работы прокрутки

            };
            // Добавляем ListBox на форму
            Controls.Add(listBoxLogs);
            // Поля для User ID, User Name, Password и Role
            CreateInputField(ref textBoxUserId, "Логин", new System.Drawing.Point(20, 300));
            textBoxUserId.KeyPress += textBoxUserId_KeyPress;
            textBoxUserId.Leave += (s, e) => AddPlaceholder(textBoxUserId, "Введите Логин");
            textBoxUserId.Enter += (s, e) => RemovePlaceholder(textBoxUserId, "Введите Логин");
            CreateInputField(ref textBoxUserName, "Имя Пользователя", new System.Drawing.Point(180, 300));
            textBoxUserName.Leave += (s, e) => AddPlaceholder(textBoxUserName, "Введите Имя");
            textBoxUserName.Enter += (s, e) => RemovePlaceholder(textBoxUserName, "Введите Имя");
            CreatePasswordField(ref textBoxPassword, "Пароль", new System.Drawing.Point(340, 300));
            textBoxPassword.Enter += (s, e) => RemovePlaceholder(textBoxPassword, "Введите пароль");
            textBoxPassword.Leave += (s, e) => AddPlaceholder(textBoxPassword, "Введите пароль");
            textBoxPassword.KeyPress += textBoxPassword_KeyPress;
            CreateComboBox(ref comboBoxRole, "Роль", new System.Drawing.Point(500, 300), new string[] { "user", "admin" });
            // Кнопка добавления пользователя
            buttonAddUser = new Button
            {
                Location = new System.Drawing.Point(660, 300),
                Size = new System.Drawing.Size(95, 40),
                Text = "Добавить",
                BackColor = System.Drawing.Color.SteelBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            buttonAddUser.FlatAppearance.BorderSize = 0;
            buttonAddUser.Click += ButtonAddUser_Click;
            Controls.Add(buttonAddUser);
            // Создание лейбла для логина
            Label labelLogin = new Label
            {
                Text = "Логин", // Текст лейбла
                Location = new System.Drawing.Point(20, 355), // Позиция над ComboBox
                AutoSize = true, // Автоматический размер по тексту
                Font = new System.Drawing.Font("Segoe UI", 10) // Настройка шрифта
            };
            Controls.Add(labelLogin);
            comboBoxChangeUserId = new ComboBox
            {
                Location = new System.Drawing.Point(20, 380), // Позиция на форме
                Size = new System.Drawing.Size(150, 30), // Размер комбобокса

                DropDownStyle = ComboBoxStyle.DropDown, // Позволяет вводить текст вручную
                AutoCompleteMode = AutoCompleteMode.SuggestAppend, // Режим автодополнения
                AutoCompleteSource = AutoCompleteSource.ListItems, // Источник автодополнения
                Font = new System.Drawing.Font("Segoe UI", 10), // Шрифт текста
                ForeColor = System.Drawing.Color.Gray, // Цвет текста в комбобоксе (для начального текста)
                Text = "Выберите ID", // Текст-плейсхолдер
                MaxDropDownItems = 9, // Максимальное количество элементов в выпадающем списке
                IntegralHeight = false // Отключение ограничения высоты списка
            };
            Controls.Add(comboBoxChangeUserId);
            SetupAutoCompleteTextBox(comboBoxChangeUserId, "Выберите ID");
            // Поле для выбора новой роли
            CreateComboBox(ref comboBoxChangeRole, "Новая роль", new System.Drawing.Point(180, 380), new string[] { "user", "admin" });
            // Кнопка изменения роли
            buttonChangeRole = new Button
            {
                Location = new System.Drawing.Point(360, 380),
                Size = new System.Drawing.Size(160, 40),
                Text = "Изменить роль",
                BackColor = System.Drawing.Color.SteelBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            buttonChangeRole.FlatAppearance.BorderSize = 0;
            buttonChangeRole.Click += ButtonChangeRole_Click;
            Controls.Add(buttonChangeRole);
            // Создаем CheckBox для запуска в трее
            checkBoxTray = new CheckBox
            {
                Location = new System.Drawing.Point(535, 393),
                // Убираем текст у CheckBox
                Text = "", // Убираем текст
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.White, // Белый цвет для галочки
                BackColor = System.Drawing.Color.Transparent,
                AutoSize = true
            };
            // Создаем Label с текстом, который будет отображаться рядом с CheckBox
            Label labelTray = new Label
            {
                Location = new System.Drawing.Point(550, 390),
                Text = "Добавить в трей",
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black, // Белый текст
                BackColor = System.Drawing.Color.Transparent,
                AutoSize = true
            };
            // Добавляем элементы в Controls
            Controls.Add(checkBoxTray);
            Controls.Add(labelTray);
            // Обработчик клика на CheckBox
            checkBoxTray.Click += checkBoxTray_Click;
            // Лейбл для IP
            Label labelIp = new Label
            {
                Text = "IP-адрес",
                Location = new System.Drawing.Point(20, 435),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black
            };
            Controls.Add(labelIp);
            // Поле для ввода IP
            textBoxIp = new TextBox
            {
                Location = new System.Drawing.Point(20, 460),
                Size = new System.Drawing.Size(300, 30),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black,
                Text = ""
            };
            AddPlaceholder(textBoxIp, "Введите IP адрес");
            textBoxIp.Leave += (s, e) => AddPlaceholder(textBoxIp, "Введите IP адрес");
            textBoxIp.Enter += (s, e) => RemovePlaceholder(textBoxIp, "Введите IP адрес");
            Controls.Add(textBoxIp);
            // Лейбл для порта
            Label labelPort = new Label
            {
                Text = "Порт",
                Location = new System.Drawing.Point(340, 435),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black
            };
            Controls.Add(labelPort);
            // Поле для ввода порта
            textBoxPort = new TextBox
            {
                Location = new System.Drawing.Point(340, 460),
                Size = new System.Drawing.Size(120, 30),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.Black,
                Text = ""
            };
            AddPlaceholder(textBoxPort, "Введите порт");
            textBoxPort.Leave += (s, e) => AddPlaceholder(textBoxPort, "Введите порт");
            textBoxPort.Enter += (s, e) => RemovePlaceholder(textBoxPort, "Введите порт");
            Controls.Add(textBoxPort);
            // Кнопка подключения
            Button buttonConnect = new Button
            {
                Location = new System.Drawing.Point(490, 460),
                Size = new System.Drawing.Size(120, 40),
                Text = "Запустить сервер",
                BackColor = System.Drawing.Color.SteelBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            buttonConnect.FlatAppearance.BorderSize = 0;
            buttonConnect.Click += (sender, e) => StartTcpServerWithInput(textBoxIp.Text, textBoxPort.Text);
            Controls.Add(buttonConnect);
            // Кнопка остановки сервера
            Button buttonStopServer = new Button
            {
                Location = new System.Drawing.Point(630, 460), // Расположено справа от кнопки "Запустить сервер"
                Size = new System.Drawing.Size(120, 40),
                Text = "Остановить сервер",
                BackColor = System.Drawing.Color.IndianRed,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            buttonStopServer.FlatAppearance.BorderSize = 0;
            buttonStopServer.Click += ButtonStopServer_Click;
            Controls.Add(buttonStopServer);
            // Статусный лейбл
            statusLabel = new Label
            {
                Location = new System.Drawing.Point(20, 520),
                Size = new System.Drawing.Size(500, 30),
                Font = new System.Drawing.Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkGreen,
                BackColor = System.Drawing.Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Сервер готов к запуску."
            };
            Controls.Add(statusLabel);
            LoadSettings();
        }
        // Создает текстовое поле (TextBox) с меткой на форме.
        private void CreateInputField(ref TextBox textBox, string labelText, System.Drawing.Point location)
        {
            textBox = new TextBox
            {
                Location = location,
                Size = new System.Drawing.Size(150, 30),
                Font = new System.Drawing.Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(new Label
            {
                Text = labelText,
                Location = new System.Drawing.Point(location.X, location.Y - 20),
                Size = new System.Drawing.Size(120, 20),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.DarkSlateGray
            });
            if (textBox == textBoxUserName)
            {
                AddPlaceholder(textBox, "Введите Имя");
            }
            else if (textBox == textBoxUserId)
            {
                AddPlaceholder(textBox, "Введите Логин");
            }
            Controls.Add(textBox);
        }
        // Создает текстовое поле (TextBox) для ввода пароля с меткой на форме.
        private void CreatePasswordField(ref TextBox textBox, string labelText, System.Drawing.Point location)
        {
            textBox = new TextBox
            {
                Location = location,
                Size = new System.Drawing.Size(150, 30),
                UseSystemPasswordChar = true,
                Font = new System.Drawing.Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(new Label
            {
                Text = labelText,
                Location = new System.Drawing.Point(location.X, location.Y - 20),
                Size = new System.Drawing.Size(120, 20),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.DarkSlateGray
            });
            // Изначально добавляем плейсхолдер
            AddPlaceholder(textBox, "Введите пароль");
            // Обработчик для удаления плейсхолдера при фокусе
            Controls.Add(textBox);
        }
        // Создает выпадающий список (ComboBox) с меткой на форме и заданными элементами.
        private void CreateComboBox(ref ComboBox comboBox, string labelText, System.Drawing.Point location, string[] items)
        {
            comboBox = new ComboBox
            {
                Location = location,
                Size = new System.Drawing.Size(150, 30),
                Font = new System.Drawing.Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = System.Drawing.Color.WhiteSmoke
            };
            comboBox.Items.AddRange(items);
            comboBox.SelectedIndex = 0;
            Controls.Add(new Label
            {
                Text = labelText,
                Location = new System.Drawing.Point(location.X, location.Y - 20),
                Size = new System.Drawing.Size(120, 20),
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.DarkSlateGray
            });

            Controls.Add(comboBox);
        }
        // Загружает настройки из файла и применяет их к состоянию приложения.
        private string checkBoxStateFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TicketSystemServer", "checkBoxState.txt");
        private void SaveSettings()
        {
            try
            {
                // Проверка на существование директории
                string directoryPath = Path.GetDirectoryName(checkBoxStateFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath); // Если папка не существует, создаем её
                }

                // Запись состояния чекбокса в новый файл
                using (StreamWriter writer = new StreamWriter(checkBoxStateFilePath))
                {
                    writer.WriteLine(checkBoxTray.Checked.ToString()); // Записываем состояние чекбокса в файл
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении состояния чекбокса: " + ex.Message);
            }
        }
        private void LoadSettings()
        {
            try
            {
                // Проверяем, существует ли папка для файла настроек состояния чекбокса
                string directoryPath = Path.GetDirectoryName(checkBoxStateFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    // Если папка не существует, создаем её
                    Directory.CreateDirectory(directoryPath);
                }
                // Проверяем, существует ли файл состояния чекбокса
                if (File.Exists(checkBoxStateFilePath))
                {
                    using (StreamReader reader = new StreamReader(checkBoxStateFilePath))
                    {
                        string savedState = reader.ReadLine();
                        checkBoxTray.Checked = savedState == "True";  // Устанавливаем состояние в соответствии с сохраненным значением
                    }
                    // После загрузки состояния чекбокса, если он активен, показываем иконку в трее
                    if (checkBoxTray.Checked)
                    {
                        trayIcon.Visible = true;
                    }
                }
                else
                {
                    MessageBox.Show("Файл состояния чекбокса не найден. Будет использовано значение по умолчанию.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке состояния чекбокса: " + ex.Message);
            }
        }
        // Показывает форму, если она скрыта, при вызове из трея.
        private void ShowFormFromTray(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        // Закрывает приложение из трея, освобождая ресурсы.
        private void CloseAppFromTray(object sender, EventArgs e)
        {
            // Если приложение работает в трее, нужно завершить его
            isExiting = true;  // Устанавливаем флаг, что приложение закрывается
            // Очищаем иконку из трея
            if (trayIcon != null)
            {
                trayIcon.Visible = false;  // Скрываем иконку в трее
                trayIcon.Dispose();        // Освобождаем ресурсы, связанные с иконкой
            }
            // Закрываем приложение
            Application.Exit();
        }
        // Обрабатывает клик по чекбоксу для управления отображением иконки в трее.
        private void checkBoxTray_Click(object sender, EventArgs e)
        {
            SaveSettings(); // Сохраняем состояние чекбокса
            // Убедимся, что иконка и контекстное меню инициализированы
            if (trayIcon != null)
            {
                if (((CheckBox)sender).Checked)
                {
                    trayIcon.Visible = true;  // Показываем иконку в трее
                    //trayIcon.ShowBalloonTip(3000, "Сервер", "Сервер работает в фоновом режиме", ToolTipIcon.Info); // Показываем уведомление
                }
                else
                {
                    trayIcon.Visible = false; // Скрываем иконку из трея
                }
            }
        }
        // Обрабатывает событие двойного клика на иконке в трее, чтобы отобразить форму.
        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            // Показываем форму при двойном клике
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Show();
            this.BringToFront();
        }
        // Метод для обновления текста статусного лейбла
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
        // Останавливает сервер при нажатии кнопки "Stop".
        private void ButtonStopServer_Click(object sender, EventArgs e)
        {
            StopTcpServer();
        }
        // Добавляет логику placeholder для текстового поля.
        private void AddPlaceholder(TextBox textBox, string placeholder)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.ForeColor = System.Drawing.Color.Gray;
                if (textBox == textBoxPassword)
                {
                    textBoxPassword.UseSystemPasswordChar = false;
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
                if (textBox == textBoxPassword)
                {
                    textBoxPassword.UseSystemPasswordChar = true;
                }
            }
        }
        /// <summary>
        /// Метод для запуска TCP-сервера с указанными IP-адресом и портом
        /// </summary>
        private void StartTcpServerWithInput(string ip, string port)
        {
            try
            {
                // Проверяем, запущен ли сервер
                if (tcpListener != null && tcpListener.Server.IsBound)
                {
                    // Если сервер уже запущен, отображаем сообщение об ошибке
                    UpdateStatusLabel("Перед подключением остановите текущее соединение!", Color.Red);
                    return;
                }
                // Проверяем корректность введенных IP-адреса и порта
                if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(port))
                {
                    UpdateStatusLabel("Введите корректные IP-адрес и порт!", Color.Red);
                    return;
                }

                // Проверяем, является ли порт числом и находится ли в допустимом диапазоне (1024-49151)
                if (!int.TryParse(port, out int portNumber) || portNumber < 1024 || portNumber > 49151)
                {
                    UpdateStatusLabel("Укажите корректный порт (1024-49151)!", Color.Red);
                    return;
                }
                IPAddress ipAddress = IPAddress.Parse(ip); // Преобразуем строку IP-адреса в объект IPAddress
                tcpListener = new TcpListener(ipAddress, portNumber); // Инициализируем и запускаем TcpListener для указанного IP-адреса и порта
                tcpListener.Start();
                statusLabel.Text = "Сервер успешно запущен";
                statusLabel.ForeColor = Color.Green;
                //UpdateStatusLabel("Сервер успешно запущен", Color.Green); // Обновляем статус и логируем запуск сервера
                Log($"TCP сервер запущен на {ip}:{port} и ожидает подключения...");
                // Сохранение данных в сессии
                sessionManager.SaveSession(ip, port); // Сохраняем данные текущей сессии
                // Запускаем поток для обработки входящих подключений
                Thread listenerThread = new Thread(new ThreadStart(AcceptTcpClients))
                {
                    IsBackground = true
                };
                listenerThread.Start();
            }
            catch (FormatException)
            {
                UpdateStatusLabel("IP-адрес или порт указаны некорректно!", Color.Red);
            }
            catch (SocketException ex)
            {
                UpdateStatusLabel($"Ошибка при запуске сервера: {ex.Message}", Color.Red);
                Log($"Ошибка при запуске сервера: {ex.Message}");
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Ошибка при запуске сервера: {ex.Message}", Color.Red);
                Log($"Ошибка при запуске сервера: {ex.Message}");
            }
        }
        //Останавливаем сервер
        private void StopTcpServer()
        {
            try
            {
                if (tcpListener != null)
                {
                    tcpListener.Stop();
                    tcpListener = null; // Обнуляем объект, чтобы избежать повторных вызовов.
                    sessionManager.ClearSession();
                    textBoxIp.Text = "";
                    textBoxPort.Text = "";
                    Log("TCP сервер остановлен.");
                    statusLabel.Text = "Сервер остановлен";
                    statusLabel.ForeColor = Color.Red;
                }
                else
                {
                    Log("TCP сервер уже остановлен.");
                    UpdateStatusLabel("TCP сервер уже остановлен.", Color.Red);
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при остановке сервера: {ex.Message}");
                UpdateStatusLabel("Ошибка при остановке сервера.", Color.Red);
            }
        }
        /// <summary>
        /// Метод для обработки входящих подключений клиентов к TCP-серверу.
        /// Запускает цикл, в котором принимает подключения, распределяет их
        /// на обработку в пул потоков и обрабатывает ошибки подключения.
        /// </summary>
        private void AcceptTcpClients()
        {
            try
            {
                while (tcpListener != null) // Проверяем, что сервер запущен
                {
                    try
                    {
                        // Проверяем доступность TcpListener
                        if (tcpListener.Pending())
                        {
                            TcpClient tcpClient = tcpListener.AcceptTcpClient();
                            Log("Клиент подключился.");
                            ThreadPool.QueueUserWorkItem(new WaitCallback(HandleTcpClient), tcpClient);
                        }
                        else
                        {
                            Thread.Sleep(100); // Небольшая пауза, чтобы избежать перегрузки процессора
                        }
                    }
                    catch (SocketException ex)
                    {
                        Log($"Ошибка сокета: {ex.Message}");
                        break; // Прерываем цикл в случае ошибки
                    }
                    catch (Exception ex)
                    {
                        Log($"Ошибка при принятии подключения: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Общая ошибка в процессе принятия подключений: {ex.Message}");
            }
            finally
            {
                Log("Цикл обработки подключений завершён.");
            }
        }
        /// <summary>
        /// Метод для обработки запросов от TCP-клиента
        /// </summary>
        private void HandleTcpClient(object obj)
        {
            // Преобразуем входящий объект в TcpClient
            TcpClient tcpClient = obj as TcpClient;
            if (tcpClient == null) return;
            // Используем блоки using для управления ресурсами сети
            using (NetworkStream stream = tcpClient.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                try
                {
                    // Читаем запрос от клиента
                    string request = reader.ReadLine();
                    Log($"Получен запрос: {request}");
                    // Проверяем, что запрос не пустой
                    if (string.IsNullOrEmpty(request))
                        return;
                    // Разбиваем запрос на части
                    string[] parts = request.Split(',');

                    // Обрабатываем тип запроса
                    switch (parts[0])
                    {
                        case "login":
                            // Обработка логина с паролем или без
                            if (parts.Length == 3)
                            {
                                string userId = parts[1];
                                string password = parts[2];
                                string role = AuthenticateUser(userId, password);  // Аутентификация по паролю

                                if (role != null)
                                {
                                    writer.WriteLine($"OK,{role}");
                                    Log($"Успешная аутентификация: {userId}, роль: {role}");
                                }
                                else
                                {
                                    writer.WriteLine("ERROR,Invalid credentials"); // Ошибка, если пароль неверный
                                    Log($"Неуспешная аутентификация: {userId}");
                                }
                            }
                            else if (parts.Length == 2) // Только userId, без пароля (для случая с сессией)
                            {
                                string userId = parts[1];
                                string role = AuthenticateUser(userId); // Аутентификация без пароля

                                if (role != null)
                                {
                                    writer.WriteLine($"OK,{role}");
                                    Log($"Успешная аутентификация без пароля: {userId}, роль: {role}");
                                }
                                else
                                {
                                    writer.WriteLine("ERROR,Invalid credentials"); // Ошибка, если пользователь не найден
                                    Log($"Неуспешная аутентификация без пароля: {userId}");
                                }
                            }
                            else
                            {
                                writer.WriteLine("ERROR,Invalid request format"); // Ошибка, если запрос не соответствует формату
                                Log("Неверный формат запроса для login");
                            }
                            break;
                        case "sendticket":
                            // Создание тикета с указанием получателя
                            if (parts.Length == 5)  // Добавляем еще один параметр для получателя
                            {
                                string userId = parts[1];
                                string issueTitle = parts[2];
                                string issueDescription = parts[3];
                                string recipientUserId = parts[4]; // Получатель
                                // Проводим аутентификацию
                                string role = AuthenticateUser(userId);
                                if (role != null)
                                {
                                    bool ticketAdded = AddTicket(userId, issueTitle, issueDescription, recipientUserId); // Изменяем на новый метод с получателем
                                    if (ticketAdded)
                                    {
                                        writer.WriteLine("OK, ticket created"); // Успешно создали тикет
                                        Log($"Тикет успешно создан: {issueTitle}");
                                    }
                                    else
                                    {
                                        writer.WriteLine("ERROR, failed to create ticket"); // Ошибка при создании тикета
                                        Log($"Не удалось создать тикет: {issueTitle}");
                                    }
                                }
                                else
                                {
                                    writer.WriteLine("ERROR, Invalid user"); // Неудачная аутентификация
                                    Log($"Неудачная попытка создания заявки, пользователь не авторизован: {userId}");
                                }
                            }
                            else
                            {
                                writer.WriteLine("ERROR, Invalid request format"); // Неверный формат запроса
                                Log("Неверный формат запроса для sendticket");
                            }
                            break;
                        case "markasdone":
                            // Обработка запроса на пометку тикета как выполненного
                            if (parts.Length == 2)
                            {
                                string ticketIdStr = parts[1];
                                if (int.TryParse(ticketIdStr, out int ticketId))
                                {
                                    bool ticketUpdated = MarkTicketAsDone(ticketId);
                                    if (ticketUpdated)
                                    {
                                        writer.WriteLine("OK, ticket marked as done");
                                        Log($"Тикет {ticketId} помечен как готово.");
                                    }
                                    else
                                    {
                                        writer.WriteLine("ERROR, failed to mark ticket as done");
                                        Log($"Не удалось пометить тикет {ticketId} как готово.");
                                    }
                                }
                                else
                                {
                                    writer.WriteLine("ERROR, invalid ticket ID");
                                    Log($"Неверный ID заявки: {ticketIdStr}");
                                }
                            }
                            else
                            {
                                writer.WriteLine("ERROR,Invalid request format");
                                Log("Неверный формат запроса для markasdone");
                            }
                            break;

                        // Старая обработка для администратора - загрузка всех тикетов
                        case "loadtickets":
                            // Загрузка тикетов для администратора
                            if (parts.Length == 2)
                            {
                                string userId = parts[1];
                                string tickets = GetTicketsForRecipient(userId); // Используем обновленный метод
                                if (tickets != null)
                                {
                                    writer.WriteLine($"OK,{tickets}");
                                    Log($"Тикеты для пользователя {userId} успешно загружены.");
                                }
                                else
                                {
                                    writer.WriteLine("ERROR, failed to load tickets");
                                    Log($"Не удалось загрузить тикеты для пользователя {userId}.");
                                }
                            }
                            else
                            {
                                writer.WriteLine("ERROR,Invalid request format");
                                Log("Неверный формат запроса для loadtickets");
                            }
                            break;
                        // Новый запрос для обычного пользователя - загрузка только своих тикетов
                        case "loadticketsForUser":
                            if (parts.Length == 2)
                            {
                                string userId = parts[1];
                                string tickets = GetTicketsForUser(userId);  // Новый метод для пользователя
                                if (tickets != null)
                                {
                                    writer.WriteLine($"OK,{tickets}");
                                    Log($"Тикеты для пользователя {userId} успешно загружены.");
                                }
                                else
                                {
                                    writer.WriteLine("ERROR, failed to load tickets");
                                    Log($"Не удалось загрузить тикеты для пользователя {userId}.");
                                }
                            }
                            else
                            {
                                writer.WriteLine("ERROR, Invalid request format");
                                Log("Неверный формат запроса для loadticketsForUser");
                            }
                            break;
                        case "loadusers":
                            // Загрузка списка пользователе
                            if (parts.Length == 1)
                            {
                                string usersList = GetUsersList(); // Получаем список администраторов
                                if (usersList != null)
                                {
                                    writer.WriteLine($"OK,{usersList}"); // Отправляем список пользователей клиенту
                                    Log("Список пользователей успешно отправлен.");
                                }
                                else
                                {
                                    writer.WriteLine("ERROR, failed to load users");
                                    Log("Не удалось загрузить пользователей.");
                                }
                            }
                            else
                            {
                                writer.WriteLine("ERROR, Invalid request format");
                                Log("Неверный формат запроса для loadusers");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Логирование ошибки при обработке клиента
                    //Log($"Ошибка при обработке клиента: {ex.Message}");
                }
                finally
                {
                    // Закрываем соединение с клиентом
                    tcpClient.Close();
                }
            }
        }
        // Получает список пользователей с ролью "admin" из базы данных.
        private string GetUsersList()
        {
            List<string> users = new List<string>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Запрос для получения пользователей с ролью "admin"
                    var command = new SqlCommand("SELECT UserName FROM Users WHERE Role = 'admin'", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string userName = reader["UserName"].ToString();
                            users.Add(userName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при извлечении пользователей: {ex.Message}");
                return null;
            }

            return string.Join(";", users); // Формируем строку с разделителем ";"
        }
        // Метод для получения тикетов только для пользователя
        private string GetTicketsForUser(string userId)
        {
            List<string> tickets = new List<string>();
            try
            {
                // Подключение к базе данных
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL запрос для получения тикетов, включая дату закрытия
                    var command = new SqlCommand(
                        "SELECT Id, UserName, Description, Status, CreatedAt, ClosedAt, UserId, IssueTitle, RecipientUserId " +
                        "FROM Tickets WHERE UserId = @UserId",
                        connection
                    );
                    command.Parameters.AddWithValue("@UserId", userId);

                    // Выполнение запроса
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Извлечение данных из базы
                            string ticketId = reader["Id"].ToString();
                            string userName = reader["UserName"].ToString();
                            string description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "";
                            string status = reader["Status"].ToString();
                            string recipientUserId = reader["RecipientUserId"] != DBNull.Value ? reader["RecipientUserId"].ToString() : "";
                            string createdAt = reader["CreatedAt"] != DBNull.Value
                                ? Convert.ToDateTime(reader["CreatedAt"]).ToString("yyyy-MM-dd HH:mm:ss")
                                : "";
                            string closedAt = reader["ClosedAt"] != DBNull.Value
                                ? Convert.ToDateTime(reader["ClosedAt"]).ToString("yyyy-MM-dd HH:mm:ss")
                                : "Не закрыт";
                            string issueTitle = reader["IssueTitle"] != DBNull.Value ? reader["IssueTitle"].ToString() : "";

                            // Формирование строки тикета
                            string ticketInfo = $"{ticketId}:{issueTitle},{status},{description},{createdAt},{closedAt},{recipientUserId},{userName}";
                            tickets.Add(ticketInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при извлечении тикетов для пользователя: {ex.Message}");
                return null;
            }

            // Объединяем все тикеты в одну строку, разделяя их точкой с запятой
            return string.Join(";", tickets);
        }
        //Получение списка заявок для администраторов
        private string GetTicketsForRecipient(string recipientUserId)
        {
            List<string> tickets = new List<string>();
            try
            {
                // Получаем UserName по UserId
                string userName = GetUserNameById1(recipientUserId);
                if (string.IsNullOrEmpty(userName))
                {
                    Log($"Не удалось найти имя пользователя для UserId {recipientUserId}");
                    return null;
                }
                Log($"Получено имя пользователя: {userName}");

                // Используем userName для получения тикетов
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Log("Соединение с базой данных установлено.");

                    var command = new SqlCommand(
                        "SELECT Id, UserName, Description, Status, CreatedAt, ClosedAt, UserId, RecipientUserId, IssueTitle " +
                        "FROM Tickets WHERE RecipientUserId = @RecipientUserName", // Используем UserName для фильтрации
                        connection
                    );
                    command.Parameters.AddWithValue("@RecipientUserName", userName);
                    Log($"Выполняем запрос с параметром RecipientUserName = {userName}");

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Log("Нет тикетов для данного пользователя.");
                        }

                        while (reader.Read())
                        {
                            // Извлечение данных из базы
                            string ticketId = reader["Id"].ToString();
                            string issueTitle = reader["IssueTitle"].ToString();
                            string description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "";
                            string status = reader["Status"].ToString();
                            string userName1 = reader["UserName"].ToString();
                            string createdAt = reader["CreatedAt"] != DBNull.Value
                                ? Convert.ToDateTime(reader["CreatedAt"]).ToString("yyyy-MM-dd HH:mm:ss")
                                : "";
                            string closedAt = reader["ClosedAt"] != DBNull.Value
                                ? Convert.ToDateTime(reader["ClosedAt"]).ToString("yyyy-MM-dd HH:mm:ss")
                                : ""; // Новый параметр
                            string recipientUserIdFromDb = reader["RecipientUserId"].ToString();

                            // Формируем строку тикета
                            string ticketInfo = $"{ticketId}:{issueTitle},{status},{description},{createdAt},{recipientUserIdFromDb},{userName1},{closedAt}";
                            tickets.Add(ticketInfo);

                            // Логируем информацию о каждом тикете
                            Log($"Загружен тикет: {ticketInfo}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при извлечении тикетов для получателя: {ex.Message}");
                return null;
            }

            // Объединяем тикеты в одну строку, разделенную ";"
            return string.Join(";", tickets);
        }
        // Метод для получения UserName по UserId
        private string GetUserNameById1(string userId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "SELECT UserName FROM Users WHERE UserId = @UserId", connection);
                    command.Parameters.AddWithValue("@UserId", userId);

                    object result = command.ExecuteScalar();
                    return result?.ToString();
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при получении UserName для UserId {userId}: {ex.Message}");
                return null;
            }
        }
        // Добавляет новую заявку (тикет) в базу данных.
        private bool AddTicket(string userId, string issueTitle, string issueDescription, string recipientUserId)
        {
            try
            {
                // Проверка на пустое значение заголовка и описания
                if (string.IsNullOrWhiteSpace(issueTitle) || string.IsNullOrWhiteSpace(issueDescription))
                {
                    Log("Ошибка: Заголовок или описание не может быть пустым.");
                    return false;
                }

                // Получаем имя пользователя
                string userName = GetUserNameById(userId);
                if (string.IsNullOrWhiteSpace(userName))
                {
                    Log("Ошибка: Имя пользователя не найдено.");
                    return false;
                }

                // Экранирование переносов строк в описании
                string escapedDescription = EscapeDescription(issueDescription);

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Запрос на добавление тикета с указанием всех параметров
                    var command = new SqlCommand(
                        "INSERT INTO Tickets (UserId, UserName, IssueTitle, Description, Status, CreatedAt, RecipientUserId) " +
                        "VALUES (@UserId, @UserName, @IssueTitle, @Description, 'В работе', @CreatedAt, @RecipientUserId)",
                        connection
                    );

                    // Передаем параметры в запрос
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@IssueTitle", issueTitle);
                    command.Parameters.AddWithValue("@Description", escapedDescription); // Используем экранированное описание
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now); // Устанавливаем текущую дату
                    command.Parameters.AddWithValue("@RecipientUserId", recipientUserId); // Передаем получателя тикета

                    // Выполняем запрос
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при добавлении заявки: {ex.Message}");
                return false;
            }
        }
        // Метод для экранирования переносов строк
        private string EscapeDescription(string description)
        {
            return description.Replace("\r\n", "<newline>").Replace("\n", "<newline>");
        }
        // Получает имя пользователя по его идентификатору.
        private string GetUserNameById(string userId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT UserName FROM Users WHERE UserId = @UserId", connection);
                    command.Parameters.AddWithValue("@UserId", userId);

                    var result = command.ExecuteScalar();
                    return result?.ToString(); // Если пользователь найден, возвращаем его имя
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при получении имени пользователя: {ex.Message}");
                return null; // Если произошла ошибка, возвращаем null
            }
        }
        // Помечает заявку как завершенную, обновляя ее статус в базе данных.
        private bool MarkTicketAsDone(int ticketId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "UPDATE Tickets SET Status = 'готово', ClosedAt = @ClosedAt WHERE Id = @Id AND Status = 'В работе'",
                        connection);
                    command.Parameters.AddWithValue("@Id", ticketId);
                    command.Parameters.AddWithValue("@ClosedAt", DateTime.Now);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при изменении статуса заявки: {ex.Message}");
                return false;
            }
        }
        // Аутентифицирует пользователя по идентификатору и, при необходимости, паролю.
        private string AuthenticateUser(string userId, string password = null)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command;

                    // Если передан пароль, выполняем полную аутентификацию
                    if (password != null)
                    {
                        command = new SqlCommand("SELECT Role FROM Users WHERE UserId = @UserId AND Password = @Password", connection);
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@Password", password);
                    }
                    else
                    {
                        // Если пароль не передан, проверяем только наличие пользователя
                        command = new SqlCommand("SELECT Role FROM Users WHERE UserId = @UserId", connection);
                        command.Parameters.AddWithValue("@UserId", userId);
                    }

                    // Логируем запрос для отладки
                    Log($"Выполнение запроса: {command.CommandText}");

                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        Log($"Роль пользователя {userId}: {result}");
                        return result.ToString();
                    }
                    else
                    {
                        Log($"Не найден пользователь с ID: {userId}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при аутентификации: {ex.Message}");
                return null;
            }
        }
        // Обработчик события нажатия клавиш в текстовом поле для UserId.
        private void textBoxUserId_KeyPress(object sender, KeyPressEventArgs e)
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
                UpdateStatusLabel("Логин может состоять только из латинских букв и цифр!", Color.Red);
            }
        }
        private void textBoxPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Разрешаем управляющие символы (например, Backspace, Delete)
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            // Проверяем, является ли символ английской буквой (латиницей)
            bool isEnglishLetter = (e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z');

            // Проверяем, является ли символ допустимым символом (например, цифры, знаки препинания, символы)
            bool isAllowedSymbol = char.IsDigit(e.KeyChar) || char.IsPunctuation(e.KeyChar) || char.IsSymbol(e.KeyChar);

            // Если символ не является английской буквой или допустимым символом, блокируем ввод
            if (!isEnglishLetter && !isAllowedSymbol)
            {
                e.Handled = true;
                UpdateStatusLabel("Разрешенны  только латинские буквы,цифры и символы!", Color.Red);
            }
            else
            {
                UpdateStatusLabel("", Color.Red);
            }
        }
        // Обработчик события для кнопки добавления пользователя.
        private void ButtonAddUser_Click(object sender, EventArgs e)
        {
            string userId = textBoxUserId.Text;
            string userName = textBoxUserName.Text;
            string password = textBoxPassword.Text;
            string role = comboBoxRole.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(userName))
            {
                UpdateStatusLabel("Логин, Имя Пользователя и пароль не могут быть пустыми!", Color.Red);
                return;
            }
            if (userId == "Введите Логин") { UpdateStatusLabel("Введите логин!", Color.Red); return; }
            if (userName == "Введите Имя") { UpdateStatusLabel("Введите имя!", Color.Red); return; }
            if (password == "Введите пароль") { UpdateStatusLabel("Введите пароль!", Color.Red); return; }
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Проверка, существует ли пользователь
                    var checkCommand = new SqlCommand("SELECT COUNT(*) FROM Users WHERE UserId = @UserId", connection);
                    checkCommand.Parameters.AddWithValue("@UserId", userId);

                    int userExists = (int)checkCommand.ExecuteScalar();
                    if (userExists > 0)
                    {
                        UpdateStatusLabel("Пользователь с таким логином уже существует!", Color.Red);
                        return;
                    }
                    // Если пользователя нет, добавляем его
                    var insertCommand = new SqlCommand("INSERT INTO Users (UserId, UserName, Password, Role) VALUES (@UserId, @UserName, @Password, @Role)", connection);
                    insertCommand.Parameters.AddWithValue("@UserId", userId);
                    insertCommand.Parameters.AddWithValue("@UserName", userName);
                    insertCommand.Parameters.AddWithValue("@Password", password);
                    insertCommand.Parameters.AddWithValue("@Role", role);
                    insertCommand.ExecuteNonQuery();
                }
                Log($"Пользователь {userId} добавлен с именем {userName} и ролью {role}");
                UpdateStatusLabel("Пользователь успешно добавлен!", Color.Green);
            }
            catch (Exception ex)
            {
                Log($"Ошибка при добавлении пользователя: {ex.Message}");
                UpdateStatusLabel("Ошибка при добавлении пользователя!", Color.Red);
            }
        }
        // Настраивает автозаполнение для указанного ComboBox и добавляет функционал Placeholder текста.
        private void SetupAutoCompleteTextBox(ComboBox comboBox, string placeholderText)
        {
            // Загрузка списка пользователей из базы данных
            var userList = GetUserListFromDatabase();
            // Добавляем список пользователей
            if (userList != null && userList.Count > 0)
            {
                comboBox.Items.AddRange(userList.ToArray());
            }
            // Логика для Placeholder
            comboBox.GotFocus += (sender, e) =>
            {
                if (comboBox.Text == placeholderText)
                {
                    comboBox.Text = "";
                    comboBox.ForeColor = System.Drawing.Color.Black;
                }
            };
            comboBox.LostFocus += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(comboBox.Text))
                {
                    comboBox.Text = placeholderText;
                    comboBox.ForeColor = System.Drawing.Color.Gray;
                }
            };
        }
        // Метод для загрузки списка пользователей из базы данных
        private List<string> GetUserListFromDatabase()
        {
            var userList = new List<string>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT UserId FROM Users", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            userList.Add(reader["UserId"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при загрузке пользователей из базы: {ex.Message}");
            }
            return userList;
        }
        // Обработчик кнопки для изменения роли пользователя.
        private void ButtonChangeRole_Click(object sender, EventArgs e)
        {
            string userId = comboBoxChangeUserId.Text; // Получаем значение из ComboBox
            string newRole = comboBoxChangeRole.SelectedItem?.ToString();
            var userList = GetUserListFromDatabase();

            if (userList == null || !userList.Contains(userId))
            {
                UpdateStatusLabel("Выберите пользователя из списка!", Color.Red);
                return;
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                UpdateStatusLabel("Логин не может быть пустым!", Color.Red);
                return;
            }
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("UPDATE Users SET Role = @Role WHERE UserId = @UserId", connection);
                    command.Parameters.AddWithValue("@Role", newRole);
                    command.Parameters.AddWithValue("@UserId", userId);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Log($"Роль пользователя {userId} изменена на {newRole}");
                        UpdateStatusLabel("Роль успешно изменена!", Color.Green);
                    }
                    else
                    {
                        UpdateStatusLabel("Пользователь не найден!", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при изменении роли: {ex.Message}");
                UpdateStatusLabel("Ошибка при изменении роли!", Color.Red);
            }
        }
        // Логирует сообщение в список логов и выводит его в консоль.
        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), message);
                return;
            }

            listBoxLogs.Items.Add($"{DateTime.Now}: {message}");
            Console.WriteLine($"{DateTime.Now}: {message}");
        }
        private bool isExiting = false; // Флаг для отслеживания состояния выхода из программы.
        private void Form1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal; // Убедитесь, что форма не свернута
            this.Visible = true;
            InitializeComponent();
            SetupForm();
            try
            {
                // Загружаем данные сессии
                var sessionData = sessionManager.LoadSession();
                if (sessionData.HasValue)
                {
                    string ip = sessionData.Value.ip;
                    string port = sessionData.Value.port;
                    if (!string.IsNullOrWhiteSpace(ip) && !string.IsNullOrWhiteSpace(port))
                    {
                        // Заполняем поля IP и порта
                        textBoxIp.Text = ip;
                        textBoxPort.Text = port;
                        // Проверяем, запущен ли уже сервер
                        if (tcpListener != null && tcpListener.Server.IsBound)
                        {
                            UpdateStatusLabel("Сервер уже запущен. Остановите текущее соединение перед повторным запуском.", Color.Red);
                            return;
                        }
                        StartTcpServerWithInput(ip, port);
                        textBoxPort.ForeColor = Color.Black;
                        textBoxIp.ForeColor = Color.Black;
                        statusLabel.Text = "Сервер запущен";
                        statusLabel.ForeColor = Color.Green;
                    }
                }
                else
                {
                    // Если сессия пустая, задаём стандартные значения
                    string defaultIp = "127.0.0.1";
                    string defaultPort = "5500";

                    // Предлагаем автоматически запустить сервер с дефолтными настройками
                    DialogResult result = MessageBox.Show(
                        $"Сессия не найдена.\nХотите запустить сервер с настройками по умолчанию?\nIP: {defaultIp}\nPort: {defaultPort}",
                        "Запуск сервера",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        textBoxIp.Text = defaultIp;
                        textBoxPort.Text = defaultPort;
                        textBoxPort.ForeColor = Color.Black;
                        textBoxIp.ForeColor = Color.Black;
                        StartTcpServerWithInput(defaultIp, defaultPort);
                        statusLabel.Text = "Сервер запущен";
                        statusLabel.ForeColor = Color.Green;
                    }
                    else if (result == DialogResult.No)
                    {

                        UpdateStatusLabel("Сервер готов к запуску", Color.Green);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при загрузке данных сессии: {ex.Message}");
                textBoxIp.Text = "";
                textBoxPort.Text = "";
            }
        }
        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Если приложение уже завершает работу, не даем закрывать форму повторно
            if (isExiting)
            {
                return;
            }

            // Подтверждение выхода
            var dialogResult = MessageBox.Show(
                "Вы уверены, что хотите завершить работу сервера?",
                "Подтверждение выхода",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dialogResult == DialogResult.No)
            {
                e.Cancel = true; // Отменяем закрытие формы
            }
            else
            {
                e.Cancel = true;
                // Обновляем статус в UI через Invoke (чтобы убедиться, что обновляем в UI-потоке)
                this.BeginInvoke((Action)(() =>
                {
                    UpdateStatusLabel("Сервер остановлен, приложение закрывается", Color.Red);
                }));
                // Даем время на обновление UI
                await Task.Delay(2000); // Задержка на 1 секунду, не блокируя UI
                // Устанавливаем флаг завершения работы
                isExiting = true;
                // Завершаем работу приложения
                Application.Exit();
            }
        }
    }
}