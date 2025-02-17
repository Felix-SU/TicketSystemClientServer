using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicketSystemClient
{
    public partial class UserForm : Form
    {
        private bool isExiting = false; // Флаг для предотвращения повторного срабатывания
        private Dictionary<int, string> ticketDetails = new Dictionary<int, string>(); // Словарь для хранения подробностей тикетов по ID
        private TextBox IssueTitleTextBox; // Поле для ввода заголовка проблемы
        private TextBox IssueDescriptionTextBox; // Поле для ввода описания проблемы
        private Button SendTicketButton; // Кнопка отправки тикета
        private ListBox InProgressTicketsListBox; // Список тикетов в работе
        private ListBox CompletedTicketsListBox; // Список завершенных тикетов
        private ComboBox RecipientComboBox; // Комбо для выбора получателя тикета
        private Label IssueTitleLabel; // Лейбл для заголовка проблемы
        private Label IssueDescriptionLabel; // Лейбл для текста проблемы
        private Label InProgressLabel; // Лейбл для тикетов в работе
        private Label CompletedLabel; // Лейбл для завершенных тикетов
        private Label RecipientLabel; // Лейбл для выбора получателя тикета
        private Label statusLabel; // Лейбл для выбора получателя тикета
        private float updatedFontSize = 10; // Лейбл для выбора получателя тикета
        private RichTextBox TicketDetailsRichTextBox; // Поле для отображения деталей тикета
        private string ServerIp;  // IP-адрес сервера для соединения
        private int ServerPort; // Номер порта сервера для соединения
        private bool isLogout = false; // Флаг состояния выхода пользователя из системы
        private System.Windows.Forms.Timer ticketUpdateTimer;
        private bool isSelectionChanging = false;
        private CancellationTokenSource _cancellationTokenSource; // Метод для обновления текста статусного лейбла
        public UserForm()
        {
            InitializeComponent(); // Инициализация стандартных компонентов интерфейса
            InitializeCustomComponents();  // Инициализация дополнительных пользовательских компонентов
            ticketUpdateTimer = new System.Windows.Forms.Timer();
            ticketUpdateTimer.Interval = 60000; // Запрашиваем каждые 5 секунд
            ticketUpdateTimer.Tick += TicketUpdateTimer_Tick;
            ticketUpdateTimer.Start();
            // Загружаем сохраненные данные сессии
            SessionManager.LoadSession();
            // Проверяем наличие и валидность IP и порта
            if (!string.IsNullOrWhiteSpace(SessionManager.LoadedIp) && !string.IsNullOrWhiteSpace(SessionManager.LoadedPort))
            {
                // Сохраняем в глобальные переменные
                ServerIp = SessionManager.LoadedIp;
                ServerPort = int.Parse(SessionManager.LoadedPort);
                LoadTickets(); // Загружаем заявки при старте формы
                LoadUsers(); // Загружаем пользователей для выбора получателя
                // Проверяем корректность порта перед преобразованием
                if (!int.TryParse(SessionManager.LoadedPort, out ServerPort))
                {
                    MessageBox.Show("Сохраненный порт имеет некорректный формат.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Проверка корректности IP-адреса
                if (!System.Net.IPAddress.TryParse(ServerIp, out _))
                {
                    MessageBox.Show("Сохраненный IP-адрес некорректен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                try
                {
                    // Используем глобальные переменные для подключения
                    using (TcpClient client = new TcpClient(ServerIp, ServerPort))
                    {
                        //MessageBox.Show("Подключение успешно установлено.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось подключиться к серверу: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Сохраненные данные IP и порта отсутствуют. Укажите их вручную.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void TicketUpdateTimer_Tick(object sender, EventArgs e)
        {
            // Периодически загружаем тикеты
            LoadTickets();
        }
        /// <summary>
        /// Метод для инициализации пользовательских компонентов формы.
        /// Настраивает внешний вид формы и добавляет основные элементы управления, такие как текстовые поля, кнопки и метки.
        /// </summary>
        private void InitializeCustomComponents()
        {
            // Установка свойств формы
            this.ClientSize = new System.Drawing.Size(800, 800);
            this.Text = "Управление Заявками";
            this.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            // Заголовок формы
            Label headerLabel = new Label
            {
                Text = "Отправка Заявок",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(33, 150, 243),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = System.Drawing.Color.White
            };
            Controls.Add(headerLabel);
            // Панель для списков тикетов
            var ticketPanel = new Panel
            {
                Location = new System.Drawing.Point(20, 70),
                Size = new System.Drawing.Size(760, 180),
                BackColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(ticketPanel);
            // Создание и настройка метки для заголовка тикета
            IssueTitleLabel = new Label
            {
                Text = "Заголовок:",
                Font = new Font("Arial", 10, FontStyle.Regular),
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(100, 20),
            };
            ticketPanel.Controls.Add(IssueTitleLabel);
            // Создание и настройка текстового поля для ввода заголовка тикета
            IssueTitleTextBox = new TextBox
            {
                Location = new System.Drawing.Point(130, 20),
                Size = new System.Drawing.Size(600, 25),
                Font = new Font("Arial", 10),
                BorderStyle = BorderStyle.FixedSingle,
                MaximumSize = new Size(800, 45),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            ticketPanel.Controls.Add(IssueTitleTextBox);
            IssueTitleTextBox.TextChanged += IssueTitleTextBox_TextChanged;
            // Создание и настройка метки для описания тикета
            IssueDescriptionLabel = new Label
            {
                Text = "Описание:",
                Font = new Font("Arial", 10, FontStyle.Regular),
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(100, 20)
            };
            ticketPanel.Controls.Add(IssueDescriptionLabel);
            // Создание и настройка текстового поля для ввода описания тикета
            IssueDescriptionTextBox = new TextBox
            {
                Location = new System.Drawing.Point(130, 60),
                Size = new System.Drawing.Size(600, 60),
                Font = new Font("Arial", 10),
                Multiline = true,
                MaximumSize = new Size(800, 140),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ScrollBars = ScrollBars.Vertical | ScrollBars.None
            };
            ticketPanel.Controls.Add(IssueDescriptionTextBox);
            IssueDescriptionTextBox.TextChanged += IssueDescriptionTextBox_TextChanged;
            // Создание и настройка метки для выбора получателя тикета
            RecipientLabel = new Label
            {
                Text = "Получатель: ",
                Font = new Font("Arial", 10, FontStyle.Regular),
                Location = new System.Drawing.Point(20, 130),
                Size = new System.Drawing.Size(100, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            ticketPanel.Controls.Add(RecipientLabel);
            // Создание и настройка комбинированного списка для выбора получателя тикета
            RecipientComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(130, 130),
                Size = new System.Drawing.Size(300, 25),
                Font = new Font("Arial", 10),
                DropDownStyle = ComboBoxStyle.DropDownList,
                MaximumSize = new Size(600, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            ticketPanel.Controls.Add(RecipientComboBox);
            // Создание и настройка кнопки для отправки тикета
            SendTicketButton = new Button
            {
                Text = "Отправить",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new System.Drawing.Point(RecipientComboBox.Right + 10, RecipientComboBox.Top),
                Size = new System.Drawing.Size(100, 30),
                BackColor = System.Drawing.Color.FromArgb(76, 175, 80),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            SendTicketButton.FlatAppearance.BorderSize = 0;
            SendTicketButton.Click += SendTicketButton_Click;
            ticketPanel.Controls.Add(SendTicketButton);
            const int spacing = 10;
            // Обработчик события изменения размеров окна
            this.Resize += (s, e) =>
            {
                // Вычисляем новое положение кнопки
                int newLeftPosition = RecipientComboBox.Right + spacing;
                // Если новое положение кнопки меньше, чем минимальное значение (например, 10 пикселей от левого края окна), то фиксируем
                if (newLeftPosition < spacing)
                {
                    newLeftPosition = spacing;
                }
                // Обновляем положение кнопки
                SendTicketButton.Location = new System.Drawing.Point(
                    newLeftPosition,
                    RecipientComboBox.Top // Держим кнопку на одной линии с комбобоксом
                );
            };
            // Панель для отображения тикетов
            var ticketsPanel = new Panel
            {
                Location = new System.Drawing.Point(20, 270),
                Size = new System.Drawing.Size(760, 300),
                BackColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(ticketsPanel);
            // Создание и настройка метки для "Заявки в работе"
            InProgressLabel = new Label
            {
                Text = "Заявки в работе:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(150, 20)
            };
            ticketsPanel.Controls.Add(InProgressLabel);
            // Создание и настройка списка для заявок в работе
            InProgressTicketsListBox = new ListBox
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size((ticketsPanel.Width - 60) / 2, 220),
                Font = new Font("Arial", 10),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            InProgressTicketsListBox.SelectedIndexChanged += InProgressTicketsListBox_SelectedIndexChanged;
            ticketsPanel.Controls.Add(InProgressTicketsListBox);
            // Создание и настройка метки для "Завершенные заявки"
            CompletedLabel = new Label
            {
                Text = "Завершенные заявки:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new System.Drawing.Point(ticketsPanel.Width / 2 + 20, 20),
                Size = new System.Drawing.Size(200, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ticketsPanel.Controls.Add(CompletedLabel);
            // Создание и настройка списка для завершенных заявок
            CompletedTicketsListBox = new ListBox
            {
                Location = new System.Drawing.Point(ticketsPanel.Width / 2 + 20, 50),
                Size = new System.Drawing.Size((ticketsPanel.Width - 60) / 2, 220),
                Font = new Font("Arial", 10),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            CompletedTicketsListBox.SelectedIndexChanged += CompletedTicketsListBox_SelectedIndexChanged;
            ticketsPanel.Controls.Add(CompletedTicketsListBox);
            // Создание и настройка RichTextBox для отображения деталей тикета
            TicketDetailsRichTextBox = new RichTextBox
            {
                Location = new System.Drawing.Point(20, 580),
                Size = new System.Drawing.Size(760, 150),
                Font = new Font("Arial", 10),
                ReadOnly = true, // Только для чтения
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.FromArgb(245, 245, 245),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                WordWrap = true, // Перенос слов
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Controls.Add(TicketDetailsRichTextBox);
            Button logoutButton = new Button
            {
                Text = "Выйти из аккаунта",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Size = new System.Drawing.Size(200, 30),
                BackColor = System.Drawing.Color.FromArgb(244, 67, 54),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Location = new System.Drawing.Point(580, 760),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.Click += LogoutButton_Click;
            Controls.Add(logoutButton);
            // Создание и настройка метки для статуса
            statusLabel = new Label
            {
                Text = "", // Начальный текст
                Font = new Font("Arial", 11, FontStyle.Bold),
                Size = new System.Drawing.Size(450, 30), // Размер метки
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new System.Drawing.Point(20, this.ClientSize.Height - 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(statusLabel);
            float baseFontSize = 10; // Начальный размер шрифта
            float maxFontSize = 20; // Максимальный размер шрифта
            // Обработчик изменения размера формы
            this.Resize += (s, e) =>
            {
                // Обновление размеров списка заявок
                InProgressTicketsListBox.Size = new System.Drawing.Size((ticketsPanel.Width - 60) / 2, 220);
                CompletedTicketsListBox.Location = new System.Drawing.Point(ticketsPanel.Width / 2 + 20, 50);
                CompletedTicketsListBox.Size = new System.Drawing.Size((ticketsPanel.Width - 60) / 2, 220);
                CompletedLabel.Location = new System.Drawing.Point(ticketsPanel.Width / 2 + 20, 20);
                updatedFontSize = baseFontSize + (this.ClientSize.Width - 800) / 200.0f; // Меньший коэффициент (200 вместо 100)
                updatedFontSize = Math.Max(baseFontSize, Math.Min(updatedFontSize, maxFontSize)); // Ограничиваем диапазон шрифта
                TicketDetailsRichTextBox.Font = new Font("Arial", updatedFontSize);
                statusLabel.Font = new Font("Arial", updatedFontSize, FontStyle.Bold);
                // Перерисовка формы, чтобы избежать графических глюков
                this.Invalidate();
                this.Update();
            };
            // Создание и настройка кнопки для выхода
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
        // Обработчик изменения текста в поле "Заголовок проблемы"
        private void IssueTitleTextBox_TextChanged(object sender, EventArgs e)
        {
            string enteredText = IssueTitleTextBox.Text;
            // Проверяем длину текста
            if (enteredText.Length > 100)
            {
                // Если длина текста больше 200 символов, обрезаем его
                IssueTitleTextBox.Text = enteredText.Substring(0, 100);
                IssueTitleTextBox.SelectionStart = IssueTitleTextBox.Text.Length;  
                UpdateStatusLabel("Заголовок не должен быть больше 100 символов.", Color.Red);
                // Запрещаем дальнейший ввод
                return;
            }
            else
            {
                // Если длина текста в пределах 100 символов, очищаем сообщение об ошибке
                statusLabel.Text = "";
            }
        }
        // Обработчик изменения текста в поле "Описание проблемы"
        private void IssueDescriptionTextBox_TextChanged(object sender, EventArgs e)
        {
            // Получаем текущий текст из TextBox
            string enteredText = IssueDescriptionTextBox.Text;
            // Проверяем, если текст превышает лимит
            if (enteredText.Length > 1000)
            {
                // Запрещаем дальнейший ввод текста
                IssueDescriptionTextBox.Text = enteredText.Substring(0, 1000); // Ограничиваем до 2000 символов
                IssueDescriptionTextBox.SelectionStart = 1000; // Устанавливаем курсор в конец текста
                // Отображаем сообщение в statusLabel
                UpdateStatusLabel("Описание не должно превышать 1000 символов", Color.Red);
            }
            else
            {
                // Если текст в пределах лимита, очищаем сообщение
                statusLabel.Text = "";
            }
        }
        // Обработчик нажатия кнопки "Выйти из аккаунта"
        private void LogoutButton_Click(object sender, EventArgs e)
        {
            isLogout = true;
            // Очистка данных сессии
            SessionManager.ClearSession();
            // Переход на форму входа
            foreach (Form form in Application.OpenForms)
            {
                if (form is LoginForm loginForm)
                {
                    loginForm.Show(); // Показываем LoginForm
                    break;
                }
            }
            // Закрываем текущую форму
            this.Close();
        }
        // Загружаем список пользователей для выбора получателя
        private void LoadUsers()
        {
            try
            {
                // Получаем текущий UserId из сессии
                string currentUserId = SessionManager.LoadedUserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    MessageBox.Show("Ошибка: пользователь не найден в сессии.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                using (TcpClient client = new TcpClient(ServerIp,ServerPort))
                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Отправляем запрос на сервер для загрузки списка пользователей
                    writer.WriteLine("loadusers");
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string response = reader.ReadLine();
                        if (response.StartsWith("OK"))
                        {
                            // Отрезаем префикс "OK," и получаем список пользователей
                            string usersData = response.Substring(3).Trim();
                            if (string.IsNullOrEmpty(usersData))
                            {
                                //MessageBox.Show("Нет доступных пользователей для загрузки.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                            string[] users = usersData.Split(';');
                            // Очистка ComboBox перед добавлением новых пользователей
                            RecipientComboBox.Items.Clear();
                            // Добавляем пользователей в ComboBox
                            foreach (var user in users)
                            {
                                if (!string.IsNullOrWhiteSpace(user))
                                {
                                    RecipientComboBox.Items.Add(user);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка при загрузке пользователей: {response}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Отправка заявки в базу данных
        private void SendTicketButton_Click(object sender, EventArgs e)
        {
            string issueTitle = IssueTitleTextBox.Text.Trim(); // Заголовок проблемы
            string issueDescription = IssueDescriptionTextBox.Text.Trim(); // Описание проблемы
            string recipientUserId = RecipientComboBox.SelectedItem?.ToString(); // Получатель заявки
            // Заменяем переносы строк специальным маркером
            issueDescription = issueDescription.Replace("\r\n", "<newline>").Replace("\n", "<newline>");
            // Получаем userId из сессии
            string userId = SessionManager.LoadedUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                MessageBox.Show("Ошибка: пользователь не найден в сессии.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Проверка, чтобы текст проблемы не был пустым
            if (string.IsNullOrWhiteSpace(issueTitle) || string.IsNullOrWhiteSpace(issueDescription))
            {
                UpdateStatusLabel("Введите заголовок и описание проблемы перед отправкой", Color.Red);
                return;
            }
            // Проверка, чтобы был выбран получатель
            if (string.IsNullOrWhiteSpace(recipientUserId))
            {
                UpdateStatusLabel("Выберите получателя заявки", Color.Red);
                return;
            }
            try
            {
                using (TcpClient client = new TcpClient(ServerIp, ServerPort)) // Подключаемся к серверу
                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Отправляем запрос для создания тикета
                    string request = $"sendticket,{userId},{issueTitle},{issueDescription},{recipientUserId}";
                    writer.WriteLine(request);
                    // Чтение ответа от сервера
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string response = reader.ReadLine();
                        if (response.StartsWith("OK"))
                        {
                            LoadTickets(); // Обновляем список тикетов
                            IssueTitleTextBox.Clear(); // Очистка поля ввода заголовка
                            IssueDescriptionTextBox.Clear(); // Очистка поля ввода описания
                            UpdateStatusLabel("Заявка отправлена!", Color.Green);
                        }
                        else
                        {
                            UpdateStatusLabel("Ошибка при отправке тикета", Color.Red);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Ошибка при отправке тикета: {ex.Message}", Color.Red);
            }
        }
        // Метод для загрузки для администратора 
        private void LoadTickets()
        {
            try
            {
                // Получаем текущий UserId из сессии
                string currentUserId = SessionManager.LoadedUserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    UpdateStatusLabel("Ошибка: пользователь не найден в сессии", Color.Red);
                    //essageBox.Show("Ошибка: пользователь не найден в сессии.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                using (TcpClient client = new TcpClient(ServerIp, ServerPort))
                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Отправляем запрос на сервер с текущим UserId
                    string request = $"loadticketsForUser,{currentUserId}";
                    writer.WriteLine(request);
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string response = reader.ReadLine();
                        if (response.StartsWith("OK"))
                        {
                            // Отрезаем префикс "OK," и получаем список тикетов
                            string ticketData = response.Substring(3).Trim();
                            string[] tickets = ticketData.Split(';');
                            // Очистка UI
                            ClearListBoxSafely(InProgressTicketsListBox);
                            ClearListBoxSafely(CompletedTicketsListBox);
                            // Списки для сортировки
                            var inProgressTickets = new List<(DateTime CreatedAt, string DisplayText)>();
                            var completedTickets = new List<(DateTime ClosedAt, string DisplayText)>();
                            foreach (var ticket in tickets)
                            {
                                if (string.IsNullOrWhiteSpace(ticket)) continue;
                                // Формат строки: "id:заголовок,статус,описание,дата создания,дата закрытия,UserId"
                                string[] parts = ticket.Split(',');
                                if (parts.Length >= 7)
                                {
                                    // Разделение первого элемента на id и заголовок
                                    string[] idTitleParts = parts[0].Split(':');
                                    if (idTitleParts.Length == 2)
                                    {
                                        // Пробуем преобразовать ticketId
                                        if (!int.TryParse(idTitleParts[0], out int ticketId))
                                        {
                                            UpdateStatusLabel("Ошибка парсинга ID тикета:", Color.Red);
                                            //MessageBox.Show($"Ошибка парсинга ID тикета: {idTitleParts[0]}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            continue;
                                        }
                                        string issueTitle = idTitleParts[1];
                                        string status = parts[1];
                                        string description = parts[2];
                                        string createdAt = parts[3];
                                        string closedAt = !string.IsNullOrWhiteSpace(parts[4]) ? parts[4] : null; // Проверяем, если дата закрытия пуста
                                        string recipientUserId = parts[5];
                                        string userName = parts[6];
                                        // Пробуем преобразовать даты
                                        if (!DateTime.TryParse(createdAt, out DateTime createdAtDate))
                                        {
                                            UpdateStatusLabel("Ошибка преобразования даты создания", Color.Red);
                                            //MessageBox.Show($"Ошибка преобразования даты создания: {createdAt}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            continue;
                                        }
                                        DateTime? closedAtDate = null;
                                        if (!string.IsNullOrWhiteSpace(closedAt) && DateTime.TryParse(closedAt, out DateTime parsedClosedAt))
                                        {
                                            closedAtDate = parsedClosedAt;
                                        }
                                        string formattedTitle = issueTitle.Length > 20 ? issueTitle.Substring(0, 20) + "..." : issueTitle;
                                        string formattedDescription = description.Length > 25 ? description.Substring(0, 25) + "..." : description;
                                        // Формируем строку для отображения
                                        string displayText = $"{ticketId}: 📌: {createdAt}\n : 📋: {issueTitle}\n : 📊: {description}\n : Закрыт: {(closedAtDate.HasValue ? closedAtDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Не закрыт")}";
                                        // Сохраняем тикет в локальный словарь
                                        ticketDetails[ticketId] = $"Заголовок: {issueTitle}\nОписание: {description}\nДата создания: {createdAt}\nДата закрытия: {(closedAtDate.HasValue ? closedAtDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Не закрыт")}\nСтатус: {status}\nКому отправлено: {recipientUserId}\nКем отправлено: {userName}";
                                        // Добавляем тикет в соответствующий список
                                        if (status == "В работе")
                                        {
                                            formattedTitle = issueTitle.Length > 30 ? issueTitle.Substring(0, 30) + "..." : issueTitle;
                                            string decodedDescription = description.Replace("<newline>", " ");
                                            formattedDescription = decodedDescription.Length > 30 ? decodedDescription.Substring(0, 30) + "..." : decodedDescription;
                                            displayText = $"{ticketId}: 📌: {createdAt}\n : 📋: {formattedTitle}\n : 📊: {formattedDescription}\n ";
                                            inProgressTickets.Add((createdAtDate, displayText));
                                        }
                                        else if (status == "готово" && closedAtDate.HasValue)
                                        {
                                            formattedTitle = issueTitle.Length > 30 ? issueTitle.Substring(0, 30) + "..." : issueTitle;
                                            string decodedDescription = description.Replace("<newline>", " ");
                                            formattedDescription = decodedDescription.Length > 35 ? decodedDescription.Substring(0, 35) + "..." : decodedDescription;
                                            displayText = $"{ticketId}: 📌: {createdAt}\n : 📋: {formattedTitle}\n : 📊: {formattedDescription}\n : Закрыт: {(closedAtDate.HasValue ? closedAtDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Не закрыт")}";
                                            completedTickets.Add((closedAtDate.Value, displayText));
                                        }
                                    }
                                }
                            }
                            // Сортировка
                            inProgressTickets.Sort((x, y) => y.CreatedAt.CompareTo(x.CreatedAt)); // Сортируем по дате создания (убывание)
                            completedTickets.Sort((x, y) => y.ClosedAt.CompareTo(x.ClosedAt));   // Сортируем по дате закрытия (убывание)
                            // Обновление ListBox'ов
                            foreach (var ticket in inProgressTickets)
                            {
                                AddToListBoxSafely(InProgressTicketsListBox, ticket.DisplayText);
                            }

                            foreach (var ticket in completedTickets)
                            {
                                AddToListBoxSafely(CompletedTicketsListBox, ticket.DisplayText);
                            }
                        }
                        else
                        {
                            UpdateStatusLabel("Ошибка при загрузке тикетов", Color.Red);
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                UpdateStatusLabel("Ошибка соединения с сервером", Color.Red);
            }
            catch (IOException ex)
            {
                UpdateStatusLabel("Ошибка ввода-вывода", Color.Red);
            }
            catch (Exception ex)
            {
                UpdateStatusLabel("Неизвестная ошибка", Color.Red);
            }
        }
        private void InProgressTicketsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isSelectionChanging) return; // Пропускаем, если выбор уже меняется
            DisplayTicketInfo(InProgressTicketsListBox);
            // Устанавливаем флаг, чтобы избежать зацикливания
            isSelectionChanging = true;
            // Сбрасываем выбор только в CompletedTicketsListBox
            CompletedTicketsListBox.ClearSelected();
            // Сбрасываем флаг
            isSelectionChanging = false;
        }
        private void CompletedTicketsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isSelectionChanging) return; // Пропускаем, если выбор уже меняется
            DisplayTicketInfo(CompletedTicketsListBox);
            // Устанавливаем флаг, чтобы избежать зацикливания
            isSelectionChanging = true;
            // Сбрасываем выбор только в InProgressTicketsListBox
            InProgressTicketsListBox.ClearSelected();
            // Сбрасываем флаг
            isSelectionChanging = false;
        }
        // Метод для отображения информации о выбранной заявке из любого списка
        private void DisplayTicketInfo(ListBox listBox)
        {
            // Проверяем, что выбранный элемент существует
            if (listBox.SelectedItem != null)
            {
                // Извлекаем ID тикета из текста выбранного элемента
                string selectedTicketText = listBox.SelectedItem.ToString();
                int ticketId = ExtractTicketId(selectedTicketText);
                // Проверяем, есть ли информация о тикете в словаре
                if (ticketDetails.ContainsKey(ticketId))
                {
                    // Получаем строку с данными тикета из словаря
                    string ticketInfo = ticketDetails[ticketId];
                    // Разделяем строку на отдельные данные
                    string[] ticketParts = ticketInfo.Split('\n');
                    // Пример данных тикета
                    string issueTitle = ticketParts.Length > 0 ? ticketParts[0].Replace("Заголовок:", "").Trim() : "";
                    string description = ticketParts.Length > 1 ? ticketParts[1].Replace("Описание:", "").Trim() : "";
                    string createdAt = ticketParts.Length > 2 ? ticketParts[2].Replace("Дата создания:", "").Trim() : "";
                    string closedAt = ticketParts.Length > 3 ? ticketParts[3].Replace("Дата закрытия:", "").Trim() : "Не закрыт";
                    string status = ticketParts.Length > 4 ? ticketParts[4].Replace("Статус:", "").Trim() : "";
                    string recipientUserId = ticketParts.Length > 5 ? ticketParts[5].Replace("Кому отправлено:", "").Trim() : "";
                    string username = ticketParts.Length > 6 ? ticketParts[6].Replace("Кем отправлено:", "").Trim() : ""; // Заглушка, можно заменить на реальное имя пользователя
                    // Настройка RichTextBox
                    TicketDetailsRichTextBox.Clear();
                    TicketDetailsRichTextBox.WordWrap = true;
                    TicketDetailsRichTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
                    // Заголовок
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", updatedFontSize, FontStyle.Bold);
                    TicketDetailsRichTextBox.SelectionColor = Color.Black;
                    TicketDetailsRichTextBox.SelectionCharOffset = 0;
                    TicketDetailsRichTextBox.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                    TicketDetailsRichTextBox.AppendText("              🎫 **Детали Заявки** 🎫\n");
                    TicketDetailsRichTextBox.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n");
                    // Форматирование текста (75% от ширины RichTextBox)
                    string formattedTitle = FormatText(issueTitle, TicketDetailsRichTextBox);
                    string formattedDescription = FormatText(description, TicketDetailsRichTextBox);
                    // Тема
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", updatedFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = Color.Black;
                    TicketDetailsRichTextBox.AppendText($"📋 Тема: {formattedTitle}\n\n");
                    string descriptionFormate = description.Replace("<newline>", Environment.NewLine);
                    // Описание
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", updatedFontSize, FontStyle.Italic);
                    TicketDetailsRichTextBox.SelectionColor = Color.Black;
                    TicketDetailsRichTextBox.AppendText($"📝 Описание: {descriptionFormate}\n\n");
                    // Кому отправлено
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", updatedFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = Color.Black;
                    TicketDetailsRichTextBox.AppendText($"📌 Кому отправлено: {recipientUserId}\n\n");
                    // Статус заявки
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", updatedFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = status == "В работе" ? Color.Red
                        : status == "готово" ? Color.Green
                        : Color.Black;
                    TicketDetailsRichTextBox.AppendText($"📊 Состояние заявки: {status}\n\n");
                    // Кем отправлено
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", updatedFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = Color.Black;
                    TicketDetailsRichTextBox.AppendText($"👤 Кем отправлено: {username}\n\n");
                    // Дата создания
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", updatedFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = Color.Green;
                    TicketDetailsRichTextBox.AppendText($"📅 Дата создания: {createdAt}\n\n");
                    // Дата закрытия
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", updatedFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = Color.Red;
                    TicketDetailsRichTextBox.AppendText($"📅 Дата закрытия: {closedAt}\n");
                    // Разделитель в конце
                    TicketDetailsRichTextBox.AppendText("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                }
                else
                {
                    UpdateStatusLabel("Ошибка парсинга ID тикета", Color.Red);
                }
            }
            else
            {
                // Очистить текст, если тикет не выбран
                TicketDetailsRichTextBox.Clear();
            }
            TicketDetailsRichTextBox.Invalidate();
        }
        // Форматирование текста с учетом ширины RichTextBox
        private string FormatText(string text, RichTextBox richTextBox)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;
            // Вычисляем максимальное количество символов для 75% ширины RichTextBox
            int maxWidth = (int)(richTextBox.Width * 0.75); // 75% от ширины в пикселях
            using (Graphics g = richTextBox.CreateGraphics())
            {
                SizeF size = g.MeasureString("W", richTextBox.Font); // Ширина одного символа
                int maxLength = Math.Max(1, (int)(maxWidth / size.Width)); // Кол-во символов в 75% ширины
                StringBuilder formattedText = new StringBuilder();
                int currentIndex = 0;
                while (currentIndex < text.Length)
                {
                    // Берём подстроку длиной maxLength, либо остаток строки
                    int length = Math.Min(maxLength, text.Length - currentIndex);
                    // Добавляем подстроку
                    formattedText.AppendLine(text.Substring(currentIndex, length).Trim());
                    // Переходим к следующему блоку текста
                    currentIndex += length;
                }
                return formattedText.ToString();
            }
        }
        //Метод извлекает ID тикета из строки, разделяя её по двоеточию и возвращая числовой ID.
        private int ExtractTicketId(string ticketText)
        {
            // Пример строки: "1234: 2024-12-20\nЗаголовок: Проблема с сервером\nОписание: ...\nСтатус: В работе"
            var parts = ticketText.Split(':');
            if (parts.Length > 0 && int.TryParse(parts[0], out int ticketId))
            {
                return ticketId;
            }
            return -1;  // Возвращаем -1, если ID не найден
        }
        // Метод для безопасной очистки ListBox
        private void ClearListBoxSafely(ListBox listBox)
        {
            if (listBox.InvokeRequired)
            {
                listBox.Invoke(new Action(() => listBox.Items.Clear()));
            }
            else
            {
                listBox.Items.Clear();
            }
        }
        // Метод для безопасного добавления элемента в ListBox
        private void AddToListBoxSafely(ListBox listBox, string item)
        {
            if (listBox.InvokeRequired)
            {
                listBox.Invoke(new Action(() => listBox.Items.Add(item)));
            }
            else
            {
                listBox.Items.Add(item);
            }
        }
        // Метод для форматирования деталей тикета
        private string FormatTicketDetails(string details)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(details))
                {
                    return "❌ Детали тикета отсутствуют.";
                }
                var formattedDetails = new StringBuilder();
                formattedDetails.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                formattedDetails.AppendLine("              🎫 **Детали Заявки** 🎫      ");
                formattedDetails.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                // Разделяем данные на строки
                string[] lines = details.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                // Храним поля для корректировки порядка
                string recipientUserIdLine = null;
                string issueTitleLine = null;
                string descriptionLine = null;
                string statusLine = null;
                string createdAtLine = null;
                string closedAtLine = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("Кому отправлено:", StringComparison.OrdinalIgnoreCase))
                    {
                        recipientUserIdLine = line.Trim();
                    }
                    else if (line.StartsWith("Заголовок:", StringComparison.OrdinalIgnoreCase))
                    {
                        issueTitleLine = line.Trim();
                    }
                    else if (line.StartsWith("Описание:", StringComparison.OrdinalIgnoreCase))
                    {
                        descriptionLine = line.Trim();
                    }
                    else if (line.StartsWith("Статус:", StringComparison.OrdinalIgnoreCase))
                    {
                        statusLine = line.Trim();
                    }
                    else if (line.StartsWith("Дата создания:", StringComparison.OrdinalIgnoreCase))
                    {
                        createdAtLine = line.Trim();
                    }
                    else if (line.StartsWith("Дата закрытия:", StringComparison.OrdinalIgnoreCase))
                    {
                        closedAtLine = line.Trim();
                    }
                }
                // Форматируем вывод
                if (!string.IsNullOrEmpty(recipientUserIdLine))
                {
                    formattedDetails.AppendLine($"📌 Кому Отправлена: {recipientUserIdLine.Replace("Кому отправлено:", "").Trim()}\n");
                }
                if (!string.IsNullOrEmpty(issueTitleLine))
                {
                    formattedDetails.AppendLine($"📋 Тема: {issueTitleLine.Replace("Заголовок:", "").Trim()}\n");
                }
                if (!string.IsNullOrEmpty(descriptionLine))
                {
                    formattedDetails.AppendLine($"📝 Описание:\n{descriptionLine.Replace("Описание:", "").Trim()}\n");
                }
                if (!string.IsNullOrEmpty(statusLine))
                {
                    formattedDetails.AppendLine($"📊 Состояние Заявки: {statusLine.Replace("Статус:", "").Trim()}\n");
                }
                if (!string.IsNullOrEmpty(createdAtLine))
                {
                    formattedDetails.AppendLine($"📅 Дата Создания: {createdAtLine.Replace("Дата создания:", "").Trim()}\n");
                }
                if (!string.IsNullOrEmpty(closedAtLine))
                {
                    formattedDetails.AppendLine($"📅 Дата Закрытия: {closedAtLine.Replace("Дата закрытия:", "").Trim()}\n");
                }
                formattedDetails.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                return formattedDetails.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при форматировании данных: {ex.Message}";
            }
        }
        // Событие при изменении выбора в Ticket ListBox
        private void TicketListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                string selectedTicket = listBox.SelectedItem.ToString();
                // Разделяем строку, чтобы извлечь ID тикета (например, "1: Internet Connection Problem")
                string[] parts = selectedTicket.Split(':');
                if (parts.Length > 1 && int.TryParse(parts[0].Trim(), out int ticketId))
                {
                    // Получаем данные тикета из словаря (или другого источника)
                    if (ticketDetails.TryGetValue(ticketId, out string details))
                    {
                        TicketDetailsRichTextBox.Text = FormatTicketDetails(details);
                    }
                    else
                    {
                        TicketDetailsRichTextBox.Text = "❌ Детали тикета не найдены.";
                    }
                }
                else
                {
                    TicketDetailsRichTextBox.Text = "❌ Неверный формат тикета.";
                }
            }
            else
            {
                TicketDetailsRichTextBox.Text = string.Empty;
            }
        }
        private void UserForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isExiting)
            {
                return; // Если приложение уже завершает работу, пропускаем обработку
            }
            if (!isLogout)
            {
                // Показываем подтверждение выхода
                var dialogResult = MessageBox.Show(
                    "Вы уверены, что хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (dialogResult == DialogResult.No)
                {
                    e.Cancel = true; // Отменяет закрытие формы
                }
                else
                {
                    isExiting = true; // Устанавливаем флаг
                    Application.Exit(); // Завершаем работу приложения
                }
            }
        }
    }
}
