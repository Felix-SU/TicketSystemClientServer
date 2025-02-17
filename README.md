# Клиент-Серверная Система Управления Заявками  
Клиент-серверное приложение для управления заявками, использующее TCP/IP на C# (.NET Framework 4.8).

<br>  
<b>Требования</b>  
<ul>
  <li>Visual Studio 2022</li>
  <li>.NET Framework 4.8</li>
  <li>SQL Server 2022</li>
  <li>Рекомендуемое разрешение экрана: 1920x1080</li>
</ul>  

<br>  
<b>Установка и запуск</b>  
<ul>
  <li>Импортировать базу данных из <code>Database/TicketSystem.bak</code></li>
  <li>Настроить подключение к базе данных в <code>App.config</code> (или в коде)</li>
  <li>Скомпилировать серверную часть <code>TicketSystemServer.sln</code></li>
  <li>Скомпилировать клиентскую часть <code>TicketSystemClient.sln</code></li>
</ul>  
<b>Превью</b>  
<p align="center">
    <img src="Screenshots/server.png" height="50%" width="80%" /><br>
    <b>Сервер</b>
</p>

<p align="center">
    <img src="Screenshots/clientauth.png" height="50%" width="80%" /><br>
    <b>Клиент: Авторизация</b>
</p>

<p align="center">
    <img src="Screenshots/sendticket.png" height="50%" width="80%" /><br>
    <b>Клиент: Отправка заявки</b>
</p>

<p align="center">
    <img src="Screenshots/markticket.png" height="50%" width="80%" /><br>
    <b>Клиент: Просмотр заявок</b>
</p>  

<br>  
<b>Примечание</b>  
Строка подключения к базе данных (по умолчанию в коде):  
<br>  
<code>Data Source=localhost;Initial Catalog=TicketSystem;Integrated Security=True;</code>  
