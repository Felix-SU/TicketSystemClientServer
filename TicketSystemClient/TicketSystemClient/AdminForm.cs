using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace TicketSystemClient
{
    public partial class AdminForm : Form
    {
        // Объявляем компоненты
        private ListBox WorkingTicketsListBox; // Список тикетов, которые находятся в работе
        private ListBox CompletedTicketsListBox; // Список завершенных тикетов
        private Button MarkAsDoneButton; // Кнопка для завершения текущего тикета
        private Button UpdateTickets; // Кнопка для завершения текущего тикета
        private RichTextBox TicketDetailsRichTextBox; // Поле для отображения подробной информации о тикете
        private bool isLogout = false; // Флаг, указывающий, был ли выполнен выход из системы
        private string ServerIp; // IP-адрес сервера для подключения
        private int ServerPort; // Порт сервера для подключения
        private float newFontSize = 10; // Новый размер шрифта для отображения текста
        private System.Windows.Forms.Timer ticketUpdateTimer;
        private CancellationTokenSource _cancellationTokenSource; // Метод для обновления текста статусного лейбла
        private bool isSelectionChanging = false;
        private bool isExiting = false; // Флаг для предотвращения повторного срабатывания
        private Label statusLabel;
        // Конструктор формы
        public AdminForm()
        {
            InitializeComponent(); // Инициализация стандартных компонентов интерфейса
            InitializeCustomComponents(); // Инициализация пользовательских компонентов
            ticketUpdateTimer = new System.Windows.Forms.Timer();
            ticketUpdateTimer.Interval = 30 * 60 * 1000; // Запрашиваем каждые 5 секунд
            ticketUpdateTimer.Tick += TicketUpdateTimer_Tick;
            ticketUpdateTimer.Start();
        }
        private void TicketUpdateTimer_Tick(object sender, EventArgs e)
        {
            // Периодически загружаем тикеты
            LoadAllTickets();
        }
        /// <summary>
        /// Метод для инициализации пользовательских компонентов формы.
        /// Настраивает внешний вид формы и добавляет основные элементы управления, такие как текстовые поля, кнопки и метки.
        /// </summary>
        private void InitializeCustomComponents()
        {
            // Установка свойств формы
            this.MaximizeBox = true; 
            this.ClientSize = new System.Drawing.Size(800, 650);
            this.Text = "Получение Заявок";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            // Заголовок формы
            Label headerLabel = new Label
            {
                Text = "Прием Заявок",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(33, 150, 243),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = System.Drawing.Color.White
            };
            Controls.Add(headerLabel);
            // Панель для списков тикетов
            var ticketsPanel = new Panel
            {
                Location = new System.Drawing.Point(20, 70),
                Size = new System.Drawing.Size(760, 300),
                BackColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(ticketsPanel);
            // Заголовок для тикетов в работе
            Label workingTicketsLabel = new Label
            {
                Text = "Заявки в работе:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(150, 20)
            };
            ticketsPanel.Controls.Add(workingTicketsLabel);
            // Список тикетов в работе
            WorkingTicketsListBox = new ListBox
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size((ticketsPanel.Width - 60) / 2, 220),
                Font = new Font("Arial", 10),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            WorkingTicketsListBox.SelectedIndexChanged += WorkingTicketsListBox_SelectedIndexChanged;
            ticketsPanel.Controls.Add(WorkingTicketsListBox);
            // Заголовок для завершенных тикетов
            Label completedTicketsLabel = new Label
            {
                Text = "Завершенные заявки:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new System.Drawing.Point(ticketsPanel.Width / 2 + 20, 20),
                Size = new System.Drawing.Size(200, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ticketsPanel.Controls.Add(completedTicketsLabel);
            // Список завершенных тикетов
            CompletedTicketsListBox = new ListBox
            {
                Location = new System.Drawing.Point(ticketsPanel.Width / 2 + 20, 50),
                Size = new System.Drawing.Size((ticketsPanel.Width - 60) / 2, 220),
                Font = new Font("Arial", 10),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            CompletedTicketsListBox.SelectedIndexChanged += CompletedTicketsListBox_SelectedIndexChanged;
            ticketsPanel.Controls.Add(CompletedTicketsListBox);
            // Поле для деталей тикета
            TicketDetailsRichTextBox = new RichTextBox
            {
                Location = new System.Drawing.Point(20, 390),
                Size = new System.Drawing.Size(760, 150),
                Font = new Font("Arial", 10), // Начальный размер шрифта
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.FromArgb(245, 245, 245),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(TicketDetailsRichTextBox);
            // Кнопка "Пометить как готово"
            MarkAsDoneButton = new Button
            {
                Text = "Пометить как готово",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new System.Drawing.Point(20, 560),
                Size = new System.Drawing.Size(150, 40),
                BackColor = System.Drawing.Color.FromArgb(76, 175, 80),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            MarkAsDoneButton.FlatAppearance.BorderSize = 0;
            MarkAsDoneButton.Click += MarkAsDoneButton_Click;
            Controls.Add(MarkAsDoneButton);
            UpdateTickets = new Button
            {
                Text = "Обновить Заявки",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new System.Drawing.Point(200, 560),
                Size = new System.Drawing.Size(150, 40),
                BackColor = System.Drawing.Color.FromArgb(76, 175, 80),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            UpdateTickets.FlatAppearance.BorderSize = 0;
            UpdateTickets.Click += UpdateTickets_Click;
            Controls.Add(UpdateTickets);
            // Кнопка "Выход из аккаунта"
            Button logoutButton = new Button
            {
                Text = "Выход из аккаунта",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new System.Drawing.Point(630, 560),
                Size = new System.Drawing.Size(150, 40),
                BackColor = System.Drawing.Color.FromArgb(244, 67, 54),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.Click += LogoutButton_Click;
            Controls.Add(logoutButton);
            // Создание statusLabel для отображения статуса в левом углу
            statusLabel = new Label
            {
                Text = "", // Начальный текст
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new System.Drawing.Point(20, this.ClientSize.Height - 40), // Расположить в левом нижнем углу
                Size = new System.Drawing.Size(760, 30),
                TextAlign = ContentAlignment.MiddleLeft, // Выравнивание по левому краю
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(statusLabel);
            // Обработчик изменения размеров формы
            float baseFontSize = 10; // Начальный размер шрифта
            float maxFontSize = 20; // Максимальный размер шрифта
            this.Resize += (s, e) =>
            {
                // Обновление размеров списка заявок
                WorkingTicketsListBox.Size = new System.Drawing.Size((ticketsPanel.Width - 60) / 2, 220);
                CompletedTicketsListBox.Location = new System.Drawing.Point(ticketsPanel.Width / 2 + 20, 50);
                CompletedTicketsListBox.Size = new System.Drawing.Size((ticketsPanel.Width - 60) / 2, 220);
                completedTicketsLabel.Location = new System.Drawing.Point(ticketsPanel.Width / 2 + 20, 20);
                // Обновление шрифта в поле TicketDetailsRichTextBox
                newFontSize = baseFontSize + (this.ClientSize.Width - 800) / 200.0f; // Меньший коэффициент (200 вместо 100)
                newFontSize = Math.Max(baseFontSize, Math.Min(newFontSize, maxFontSize)); // Ограничиваем диапазон шрифта
                TicketDetailsRichTextBox.Font = new Font("Arial", newFontSize);
                this.Invalidate();
                this.Update();
            };
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
        // Обработчик кнопки "Выход из аккаунта"
        private void UpdateTickets_Click(object sender, EventArgs e)
        {
            UpdateStatusLabel("Заявки обновлены", Color.Green);
            LoadAllTickets();
        }
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
        // Словарь для хранения подробностей тикетов по ID
        private Dictionary<int, string> ticketDetails = new Dictionary<int, string>();
        // Метод для загрузки всех тикетов
        private void LoadAllTickets()
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
                // Подключаемся к серверу
                using (TcpClient client = new TcpClient(ServerIp, ServerPort))
                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Отправляем запрос на сервер с текущим UserId
                    string request = $"loadtickets,{currentUserId}";
                    writer.WriteLine(request);
                    // Чтение ответа от сервера
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string response = reader.ReadLine();
                        if (response.StartsWith("OK"))
                        {
                            string ticketData = response.Substring(3).Trim();
                            if (string.IsNullOrEmpty(ticketData))
                            {
                                //MessageBox.Show("Нет доступных тикетов для загрузки.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                            // Разделяем тикеты на основе символа ";"
                            string[] tickets = ticketData.Split(';');
                            // Очистка UI
                            ClearListBoxSafely(WorkingTicketsListBox);
                            ClearListBoxSafely(CompletedTicketsListBox);
                            // Листы для временного хранения тикетов
                            var inProgressTickets = new List<(DateTime createdAt, string displayText)>();
                            var completedTickets = new List<(DateTime closedAt, string displayText)>();
                            foreach (var ticket in tickets)
                            {
                                if (string.IsNullOrWhiteSpace(ticket)) continue;
                                // Разделяем каждый тикет на его части
                                string[] parts = ticket.Split(',');
                                if (parts.Length >= 7)
                                {
                                    string[] idTitleParts = parts[0].Split(':');
                                    if (idTitleParts.Length == 2)
                                    {
                                        if (!int.TryParse(idTitleParts[0], out int ticketId))
                                        {
                                            MessageBox.Show($"Ошибка парсинга ID заявки: {idTitleParts[0]}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            continue;
                                        }
                                        string issueTitle = idTitleParts[1];
                                        string status = parts[1];
                                        string description = parts[2];
                                        string createdAt = parts[3];
                                        string recipientUserId = parts[4];
                                        string userName = parts[5];
                                        string closedAt = !string.IsNullOrWhiteSpace(parts[6]) ? parts[6] : null;
                                        // Проверяем корректность дат
                                        if (!DateTime.TryParse(createdAt, out DateTime createdAtDate))
                                        {
                                            MessageBox.Show($"Ошибка преобразования даты создания: {createdAt}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            continue;
                                        }
                                        DateTime? closedAtDate = null;
                                        if (!string.IsNullOrWhiteSpace(closedAt) && DateTime.TryParse(closedAt, out DateTime parsedClosedAt))
                                        {
                                            closedAtDate = parsedClosedAt;
                                        }
                                        // Форматируем текст
                                        string descriptionFormated = description.Replace("<newline>", " ");
                                        string formattedTitle = issueTitle.Length > 30 ? issueTitle.Substring(0, 30) + "..." : issueTitle;
                                        string formattedDescription = descriptionFormated.Length > 35 ? descriptionFormated.Substring(0, 35) + "..." : descriptionFormated;
                                        string displayText = $"{ticketId}:📅: {createdAt}\n 📋: {formattedTitle}\n📊: {formattedDescription}";
                                        if (closedAtDate.HasValue && status.Equals("готово", StringComparison.OrdinalIgnoreCase))
                                        {
                                            displayText += $"\n📅Закрыт: {closedAt}";
                                        }
                                        // Сохраняем тикет в локальный словарь
                                        ticketDetails[ticketId] = $" Заголовок: {issueTitle}\n Описание: {description}\n Дата создания: {createdAt}\n Статус: {status}\n Кем отправлен: {userName}\n Дата закрытия: {(closedAtDate.HasValue ? closedAtDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Не закрыта")}";
                                        // Добавляем тикеты в соответствующие временные списки
                                        if (string.Equals(status, "В работе", StringComparison.OrdinalIgnoreCase)) { 
                                        
                                            inProgressTickets.Add((createdAtDate, displayText));
                                        }
                                        else if (string.Equals(status, "готово", StringComparison.OrdinalIgnoreCase) && closedAtDate.HasValue)
                                        {
                                            
                                            completedTickets.Add((closedAtDate.Value, displayText));
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Ошибка формата тикета: {ticket}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show($"Недостаточно данных для тикета: {ticket}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            // Сортируем тикеты
                            inProgressTickets = inProgressTickets.OrderByDescending(t => t.createdAt).ToList();
                            completedTickets = completedTickets.OrderByDescending(t => t.closedAt).ToList();
                            // Добавляем отсортированные тикеты в ListBox
                            foreach (var ticket in inProgressTickets)
                            {
                                AddToListBoxSafely(WorkingTicketsListBox, ticket.displayText);
                            }
                            foreach (var ticket in completedTickets)
                            {
                                AddToListBoxSafely(CompletedTicketsListBox, ticket.displayText);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка при загрузке тикетов: {response}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatusLabel("Ошибка соединения с сервером", Color.Red);
            }
        }
        private void WorkingTicketsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isSelectionChanging) return; // Пропускаем, если выбор уже меняется
            DisplayTicketInfo(WorkingTicketsListBox);
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
            // Сбрасываем выбор только в WorkingTicketsListBox
            WorkingTicketsListBox.ClearSelected();
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
                    // Пример данных тикета: Заголовок, описание, статус, дата
                    string issueTitle = ticketParts.Length > 0 ? ticketParts[0].Replace("Заголовок:", "").Trim() : "";
                    string description = ticketParts.Length > 1 ? ticketParts[1].Replace("Описание:", "").Trim() : "";
                    string createdAt = ticketParts.Length > 2 ? ticketParts[2].Replace("Дата создания:", "").Trim() : "";
                    string status = ticketParts.Length > 3 ? ticketParts[3].Replace("Статус:", "").Trim() : "";
                    string username = ticketParts.Length > 4 ? ticketParts[4].Replace("Кем отправлен:", "").Trim() : "";
                    string closedAt = ticketParts.Length > 5 ? ticketParts[5].Replace("Дата закрытия:", "").Trim() : "Не закрыт";
                    // Настроим RichTextBox
                    TicketDetailsRichTextBox.Clear();  // Очистить перед новым выводом
                    TicketDetailsRichTextBox.WordWrap = true;  // Включаем перенос слов
                    TicketDetailsRichTextBox.ScrollBars = RichTextBoxScrollBars.Vertical; // Вертикальная прокрутка
                    // Разделители и заголовок тикета
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", newFontSize, FontStyle.Bold);
                    TicketDetailsRichTextBox.SelectionColor = Color.Black;
                    TicketDetailsRichTextBox.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                    TicketDetailsRichTextBox.AppendText("              🎫 **Детали Заявки** 🎫      \n");
                    TicketDetailsRichTextBox.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n");
                    // Кому отправлен
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", newFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = Color.Black;
                    TicketDetailsRichTextBox.AppendText($"📌 Кем Отправлена: {username}\n\n");
                    // Тема тикета
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", newFontSize, FontStyle.Bold);
                    TicketDetailsRichTextBox.SelectionColor = Color.Black;
                    TicketDetailsRichTextBox.AppendText($"📋 Тема: {issueTitle}\n\n");
                    // Описание
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", newFontSize, FontStyle.Italic);
                    TicketDetailsRichTextBox.SelectionColor = Color.DimGray;
                    string descriptionFormate = description.Replace("<newline>", Environment.NewLine); 
                    TicketDetailsRichTextBox.AppendText($"📝 Описание:{descriptionFormate}\n\n");
                    // Статус
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", newFontSize, FontStyle.Regular);
                    if (string.Equals(status, "В работе", StringComparison.OrdinalIgnoreCase))
                    {
                        TicketDetailsRichTextBox.SelectionColor = Color.Red; // Красный для статуса "В работе"
                    }
                    else if (string.Equals(status, "готово", StringComparison.OrdinalIgnoreCase))
                    {
                        TicketDetailsRichTextBox.SelectionColor = Color.Green; // Зеленый для статуса "Готово"
                    }
                    else
                    {
                        TicketDetailsRichTextBox.SelectionColor = Color.Black; // Черный для других статусов
                    }
                    TicketDetailsRichTextBox.AppendText($"📊 Состояние Заявки: {status}\n\n");
                    // Дата создания
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", newFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = Color.Green;
                    TicketDetailsRichTextBox.AppendText($"📅 Дата создания: {createdAt}\n");
                    // Дата закрытия
                    TicketDetailsRichTextBox.SelectionFont = new Font("Arial", newFontSize, FontStyle.Regular);
                    TicketDetailsRichTextBox.SelectionColor = Color.Red;
                    TicketDetailsRichTextBox.AppendText($"\n📅 Дата закрытия: {closedAt}\n");
                    // Граница
                    TicketDetailsRichTextBox.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                }
                else
                {
                    MessageBox.Show("Информация о тикете не найдена.");
                }
            }
        }
        // Метод для извлечения ID тикета из строки текста
        private int ExtractTicketId(string ticketText)
        {
            var parts = ticketText.Split(':');
            if (parts.Length > 1 && int.TryParse(parts[0].Trim(), out int ticketId))
            {
                return ticketId;
            }
            return -1; // Возвращаем -1, если ID не найден
        }
        // Метод для безопасного очищения списка в ListBox
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
                formattedDetails.AppendLine("📋 **Детали тикета**");
                formattedDetails.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                // Разделяем данные на строки
                string[] lines = details.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                // Храним поля для корректировки порядка
                string recipientUserIdLine = null;
                string issueTitleLine = null;
                string descriptionLine = null;
                string statusLine = null;
                string usernameLine = null;
                string createdAtLine = null;
                string closedAtLine = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("RecipientUserId:", StringComparison.OrdinalIgnoreCase))
                    {
                        recipientUserIdLine = line.Trim();
                    }
                    else if (line.StartsWith("IssueTitle:", StringComparison.OrdinalIgnoreCase))
                    {
                        issueTitleLine = line.Trim();
                    }
                    else if (line.StartsWith("Description:", StringComparison.OrdinalIgnoreCase))
                    {
                        descriptionLine = line.Trim();
                    }
                    else if (line.StartsWith("Status:", StringComparison.OrdinalIgnoreCase))
                    {
                        statusLine = line.Trim();
                    }
                    else if (line.StartsWith("Username:", StringComparison.OrdinalIgnoreCase))
                    {
                        usernameLine = line.Trim();
                    }
                    else if (line.StartsWith("CreatedAt:", StringComparison.OrdinalIgnoreCase))
                    {
                        createdAtLine = line.Trim();
                    }
                    else if (line.StartsWith("ClosedAt:", StringComparison.OrdinalIgnoreCase))
                    {
                        closedAtLine = line.Trim();
                    }
                }
                // Форматируем вывод
                if (!string.IsNullOrEmpty(issueTitleLine))
                {
                    formattedDetails.AppendLine($"📋 {issueTitleLine}");
                }
                if (!string.IsNullOrEmpty(descriptionLine))
                {
                    formattedDetails.AppendLine($"📝 {descriptionLine}");
                }
                if (!string.IsNullOrEmpty(statusLine))
                {
                    formattedDetails.AppendLine($"📊 {statusLine}");
                }
                if (!string.IsNullOrEmpty(usernameLine))
                {
                    formattedDetails.AppendLine($"👤 {usernameLine}");
                }
                if (!string.IsNullOrEmpty(createdAtLine))
                {
                    formattedDetails.AppendLine($"🕒 Дата создания: {createdAtLine}");
                }
                if (!string.IsNullOrEmpty(closedAtLine))
                {
                    formattedDetails.AppendLine($"🕒 Дата закрытия: {closedAtLine}");
                }
                if (!string.IsNullOrEmpty(recipientUserIdLine))
                {
                    formattedDetails.AppendLine($"📌 {recipientUserIdLine}");
                }

                formattedDetails.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
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
        // Событие при нажатии кнопки "Пометить как готово"
        private void MarkAsDoneButton_Click(object sender, EventArgs e)
        {
            if (WorkingTicketsListBox.SelectedItem == null)
            {
                UpdateStatusLabel("Выберите заявку из заявок в работе", Color.Red);
                return;
            }
            string selectedTicket = WorkingTicketsListBox.SelectedItem.ToString();
            // Разделяем строку по двоеточию ':', чтобы извлечь ID тикета
            string[] parts = selectedTicket.Split(':');
            // Если есть хотя бы одна часть и эта часть — число
            if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int ticketId))
            {
                try
                {
                    // Отправка запроса на сервер для обновления статуса тикета
                    using (TcpClient client = new TcpClient(ServerIp, ServerPort)) // Подключаемся к серверу
                    using (NetworkStream stream = client.GetStream())
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        string request = $"markasdone,{ticketId}"; // Формат запроса
                        writer.WriteLine(request);
                        // Чтение ответа от сервера
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string response = reader.ReadLine();
                            if (response.StartsWith("OK"))
                            {
                                UpdateStatusLabel("Заявка помеченна как готовая", Color.Green);
                                LoadAllTickets(); // Перезагружаем список тикетов
                            }
                            else
                            {
                                MessageBox.Show($"Ошибка при обновлении статуса тикета: {response}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatusLabel("Сервер не доступен", Color.Red);
                }
            }
            else
            {
                // Если не удалось извлечь ID
                MessageBox.Show("Неверный формат ID тикета.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void AdminForm_FormClosing(object sender, FormClosingEventArgs e)
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
        private void AdminForm_Load(object sender, EventArgs e)
        {
            // Загружаем сохраненные данные сессии
            SessionManager.LoadSession();
            // Проверяем наличие и валидность IP и порта
            if (!string.IsNullOrWhiteSpace(SessionManager.LoadedIp) && !string.IsNullOrWhiteSpace(SessionManager.LoadedPort))
            {
                // Сохраняем в глобальные переменные
                ServerIp = SessionManager.LoadedIp;
                ServerPort = int.Parse(SessionManager.LoadedPort);
                LoadAllTickets(); // Загружаем все заявки при открытии формы
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
    }
}

