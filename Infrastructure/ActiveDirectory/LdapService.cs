using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Infrastructure.ActiveDirectory
{
    public class LdapService : ILdapService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LdapService> _logger;
        private LdapConnection _connection;
        private readonly object _lock = new object();
        private bool _disposed = false;

        // Конфигурационные параметры
        private readonly string _server;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _searchBase;
        private readonly int _timeoutMs = 30000;

        public LdapService(IConfiguration configuration, ILogger<LdapService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Читаем конфигурацию один раз
            _server = _configuration["Ldap:Server"] ?? "10.51.4.18";
            _port = int.Parse(_configuration["Ldap:Port"] ?? "389");
            _username = _configuration["Ldap:Username"] ?? "STUD\\Administrator";
            _password = _configuration["Ldap:Password"] ?? "hf8-5Bu3YMy):{uP;x";
            _searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";

            _logger.LogInformation("LDAP конфигурация: Server={Server}, Port={Port}, Username={Username}, SearchBase={SearchBase}",
                _server, _port, _username, _searchBase);
        }

        private LdapConnection CreateConnection()
        {
            var connection = new LdapConnection
            {
                ConnectionTimeout = _timeoutMs,
                Constraints = new LdapSearchConstraints
                {
                    ReferralFollowing = true,
                    BatchSize = 1000
                }
            };

            return connection;
        }

        private LdapConnection GetOrCreateConnection()
        {
            lock (_lock)
            {
                if (_connection != null && _connection.Connected)
                {
                    _logger.LogDebug("Используется существующее подключение");
                    return _connection;
                }

                // Закрываем старое соединение
                if (_connection != null)
                {
                    try
                    {
                        if (_connection.Connected)
                        {
                            _connection.Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при закрытии соединения");
                    }
                    _connection.Dispose();
                    _connection = null;
                }

                // Создаем новое соединение
                _connection = CreateConnection();
                return _connection;
            }
        }

        private void Connect(LdapConnection connection)
        {
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogInformation("Подключение к LDAP {Server}:{Port}...", _server, _port);

                    // Подключаемся
                    connection.ConnectAsync(_server, _port);

                    // Авторизуемся
                    _logger.LogDebug("Аутентификация пользователя {Username}", _username);
                    connection.BindAsync(_username, _password);

                    _logger.LogInformation("Успешное подключение к LDAP");
                    return;
                }
                catch (LdapException ex) when (ex.ResultCode == 91 && retryCount < maxRetries - 1)
                {
                    // Код 91: Can't connect to server
                    retryCount++;
                    _logger.LogWarning("Не удалось подключиться к серверу, попытка {Retry}/{MaxRetries}...",
                        retryCount, maxRetries);
                    Task.Delay(2000).Wait();
                }
                catch (LdapException ex)
                {
                    _logger.LogError(ex, "Ошибка LDAP: Код={ResultCode}, Сообщение={Message}",
                        ex.ResultCode, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Неизвестная ошибка при подключении к LDAP");
                    throw;
                }
            }
        }

        public async Task<User> GetUserBySamAccountNameAsync(string samAccountName)
        {
            LdapConnection connection = null;

            try
            {
                connection = GetOrCreateConnection();

                // Если соединение не активно, подключаемся
                if (!connection.Connected)
                {
                    Connect(connection);
                }

                var escapedName = EscapeLdapFilter(samAccountName);
                var filter = $"(sAMAccountName={escapedName})";

                _logger.LogDebug("Поиск пользователя: {Filter}", filter);

                var attributes = new[] {
                    "sAMAccountName", "displayName", "mail", "title", "department",
                    "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
                    "givenName", "sn", "initials", "whenCreated", "employeeID",
                    "distinguishedName", "objectGUID", "userAccountControl",
                    "company", "description", "officePhone", "mobile", "streetAddress",
                    "postalCode", "co", "userPrincipalName", "memberOf"
                };

                var taskRes = connection.SearchAsync(
                    _searchBase,
                    LdapConnection.ScopeSub,
                    filter,
                    attributes,
                    false
                );

                var results = await taskRes;

                

                try
                {
                    if (results.HasMoreAsync().Result)
                    {
                        var entry = results.NextAsync().Result;
                        var user = MapLdapEntryToUser(entry);

                        if (user != null)
                        {
                            _logger.LogInformation("Найден пользователь: {SamAccountName}", samAccountName);
                            return user;
                        }
                    }

                    _logger.LogDebug("Пользователь {SamAccountName} не найден", samAccountName);
                    return null;
                }
                finally
                {
                    taskRes.Dispose();
                }
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP ошибка при поиске пользователя {SamAccountName}: {ResultCode}",
                    samAccountName, ex.ResultCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске пользователя {SamAccountName}", samAccountName);
                return null;
            }
        }

        public async Task<List<User>> GetUsersFromActiveDirectoryAsync()
        {
            var users = new List<User>();
            LdapConnection connection = null;

            try
            {
                _logger.LogInformation("Начало массовой синхронизации пользователей из Active Directory");

                _logger.LogDebug("Попытка получить или создать LDAP-соединение");
                connection = GetOrCreateConnection();

                if (connection == null)
                {
                    _logger.LogError("GetOrCreateConnection вернул null");
                    throw new InvalidOperationException("Не удалось создать соединение с LDAP-сервером.");
                }

                _logger.LogDebug("Состояние соединения до подключения: Connected={Connected}, Bound={Bound}",
                    connection.Connected, connection.Bound);

                if (!connection.Connected)
                {
                    _logger.LogInformation("Соединение с LDAP не установлено. Выполняется подключение...");
                    Connect(connection);
                    _logger.LogInformation("Подключение к LDAP успешно выполнено");
                }
                else
                {
                    _logger.LogDebug("Используется существующее соединение");
                }

                var filter = "(&(objectClass=user)(objectCategory=person))";

                _logger.LogInformation("Начало массовой синхронизации пользователей");
                _logger.LogDebug("Применяемый фильтр поиска: {Filter}", filter);
                _logger.LogDebug("База поиска: {SearchBase}", _searchBase);

                var attributes = new[] {
            "sAMAccountName", "displayName", "mail", "title", "department",
            "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
            "givenName", "sn", "initials", "whenCreated", "employeeID",
            "distinguishedName", "objectGUID", "userAccountControl",
            "company", "description", "officePhone", "mobile"
        };

                _logger.LogDebug("Запрашиваемые атрибуты: {Attributes}", string.Join(", ", attributes));

                _logger.LogInformation("Выполняется асинхронный запрос к LDAP: SearchAsync");
                var taskRes = connection.SearchAsync(
                    _searchBase,
                    LdapConnection.ScopeSub,
                    filter,
                    attributes,
                    false,
                    new LdapSearchConstraints
                    {
                        BatchSize = 1000,
                        ServerTimeLimit = 0
                    });

                _logger.LogDebug("SearchAsync вызван. Ожидание завершения задачи...");
                var results = await taskRes;
                _logger.LogInformation("Получен результат поиска. Начало обработки записей");

                int processed = 0, skipped = 0;
                int entryIndex = 0;

                try
                {
                    while (results.HasMoreAsync().Result)
                    {
                        entryIndex++;
                        _logger.LogDebug("Обработка записи #{EntryIndex}", entryIndex);

                        try
                        {
                            _logger.LogDebug("Чтение следующей записи через NextAsync()");
                            var entry = results.NextAsync().Result;

                            if (entry == null)
                            {
                                _logger.LogWarning("Получена null-запись при чтении записи #{EntryIndex}", entryIndex);
                                skipped++;
                                continue;
                            }

                            _logger.LogDebug("Получена запись: DN={DN}", entry.Dn);

                            var user = MapLdapEntryToUser(entry);

                            if (user != null)
                            {
                                users.Add(user);
                                processed++;

                                if (processed % 1000 == 0)
                                {
                                    _logger.LogInformation("Обработано {Processed} пользователей...", processed);
                                }
                            }
                            else
                            {
                                skipped++;
                                _logger.LogDebug("Пользователь пропущен (не прошёл маппинг): DN={DN}", entry.Dn);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при обработке записи #{EntryIndex}", entryIndex);
                            skipped++;
                        }
                    }

                    _logger.LogInformation("Завершено чтение всех доступных записей. Всего обработано записей: {EntryIndex}", entryIndex);
                }
                finally
                {
                    _logger.LogDebug("Освобождение ресурсов: вызов Dispose() для результата поиска");
                    try
                    {
                        taskRes.Dispose();
                        _logger.LogDebug("Ресурсы поиска успешно освобождены");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при освобождении ресурсов поиска");
                    }
                }

                _logger.LogInformation(
                    "Синхронизация завершена: {Processed} пользователей, {Skipped} пропущено",
                    processed, skipped);

                return users;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP ошибка при массовом получении пользователей: {ResultCode} — {Message}",
                    ex.ResultCode, ex.Message);
                throw;
            }
            catch (AggregateException aggEx)
            {
                _logger.LogError(aggEx, "AggregateException при выполнении асинхронного LDAP-запроса");
                foreach (var inner in aggEx.InnerExceptions)
                {
                    _logger.LogError(inner, "Внутреннее исключение в AggregateException");
                }
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Задача LDAP-поиска была отменена (таймаут или отмена)");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при получении пользователей из LDAP");
                throw;
            }
        }



        public async Task<LdapHierarchyResponse> GetLdapHierarchyAsync()
        {
            LdapConnection connection = null;

            try
            {
                connection = GetOrCreateConnection();

                if (!connection.Connected)
                {
                    Connect(connection);
                }

                _logger.LogInformation("Получение иерархии LDAP");

                var response = new LdapHierarchyResponse();

                var taskRes = connection.SearchAsync(
                    _searchBase,
                    LdapConnection.ScopeSub,
                    "(objectClass=organizationalUnit)",
                    new[] { "ou", "description", "distinguishedName" },
                    false
                );

                // Получаем OU
                var ouResults = taskRes.Result;

                try
                {
                    while (ouResults.HasMoreAsync().Result)
                    {
                        try
                        {
                            var entry = ouResults.NextAsync().Result;
                            var attributes = entry.GetAttributeSet();

                            response.OrganizationalUnits.Add(new LdapOrganizationalUnit
                            {
                                Name = GetAttributeValue(attributes, "ou"),
                                Description = GetAttributeValue(attributes, "description"),
                                DistinguishedName = GetAttributeValue(attributes, "distinguishedName")
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при обработке OU");
                        }
                    }
                }
                finally
                {
                    taskRes.Dispose();
                }

                var taskUsers = connection.SearchAsync(
                    _searchBase,
                    LdapConnection.ScopeSub,
                    "(&(objectClass=user)(objectCategory=person))",
                    new[] {
                        "sAMAccountName", "displayName", "title", "department",
                        "manager", "distinguishedName", "userAccountControl",
                        "givenName", "sn", "mail", "telephoneNumber", "physicalDeliveryOfficeName"
                    },
                    false
                );
                // Получаем пользователей
                var userResults = taskUsers.Result;

                int inactiveSkipped = 0;

                try
                {
                    while (userResults.HasMoreAsync().Result)
                    {
                        try
                        {
                            var entry = userResults.NextAsync().Result
                                ;
                            var attributes = entry.GetAttributeSet();

                            var userAccountControl = GetAttributeValue(attributes, "userAccountControl");
                            if (!IsUserActive(userAccountControl))
                            {
                                inactiveSkipped++;
                                continue;
                            }

                            response.Users.Add(new LdapUserInfo
                            {
                                SamAccountName = GetAttributeValue(attributes, "sAMAccountName"),
                                DisplayName = GetAttributeValue(attributes, "displayName"),
                                FirstName = GetAttributeValue(attributes, "givenName"),
                                LastName = GetAttributeValue(attributes, "sn"),
                                Title = GetAttributeValue(attributes, "title"),
                                Department = GetAttributeValue(attributes, "department"),
                                Manager = GetAttributeValue(attributes, "manager"),
                                Email = GetAttributeValue(attributes, "mail"),
                                Phone = GetAttributeValue(attributes, "telephoneNumber"),
                                Office = GetAttributeValue(attributes, "physicalDeliveryOfficeName"),
                                DistinguishedName = GetAttributeValue(attributes, "distinguishedName")
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при обработке пользователя");
                        }
                    }
                }
                finally
                {
                    taskUsers.Dispose();
                }

                response.TotalUsers = response.Users.Count;
                response.TotalOUs = response.OrganizationalUnits.Count;

                _logger.LogInformation(
                    "Иерархия собрана: {Users} пользователей, {OUs} OU",
                    response.TotalUsers, response.TotalOUs);

                return response;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP ошибка при получении иерархии: {ResultCode}", ex.ResultCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении иерархии LDAP");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Тест подключения к LDAP: {Server}:{Port}", _server, _port);

                using (var testConnection = CreateConnection())
                {
                    await testConnection.ConnectAsync(_server, _port);
                    await testConnection.BindAsync(_username, _password);
                    var taskRes = testConnection.SearchAsync(
                        _searchBase,
                        LdapConnection.ScopeBase,
                        "(objectClass=*)",
                        new[] { "distinguishedName" },
                        false
                    );
                    // Пробуем выполнить простой запрос
                    var results = taskRes.Result;

                    try
                    {
                        if (results.HasMoreAsync().Result)
                        {
                            await results.NextAsync();
                        }
                    }
                    finally
                    {
                        taskRes.Dispose();
                    }

                    testConnection.Disconnect();
                }

                _logger.LogInformation("Тест подключения к LDAP: УСПЕШНО");
                return true;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "Тест подключения к LDAP: НЕУДАЧА. Код ошибки: {ResultCode}", ex.ResultCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Тест подключения к LDAP: НЕУДАЧА");
                return false;
            }
        }

        public async Task<bool> TestCredentialsAsync(string username, string password)
        {
            try
            {
                using (var testConnection = CreateConnection())
                {
                    await testConnection.ConnectAsync(_server, _port);
                    await testConnection.BindAsync(username, password);
                    testConnection.Disconnect();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private User MapLdapEntryToUser(LdapEntry entry)
        {
            try
            {
                var attributes = entry.GetAttributeSet();
                var samAccountName = GetAttributeValue(attributes, "sAMAccountName");

                if (string.IsNullOrEmpty(samAccountName))
                {
                    return null;
                }

                var userAccountControl = GetAttributeValue(attributes, "userAccountControl");
                if (!IsUserActive(userAccountControl))
                {
                    return null;
                }

                var whenCreated = ParseLdapDate(GetAttributeValue(attributes, "whenCreated"));

                return new User
                {
                    SamAccountName = samAccountName,
                    Email = GetAttributeValue(attributes, "mail") ?? GetAttributeValue(attributes, "userPrincipalName"),
                    Login = samAccountName,
                    Password = "LDAP_SYNCED_USER",
                    IsActive = true,
                    LastAdSync = DateTime.UtcNow,
                    PersonalInfo = new PersonalInfo
                    {
                        Last_name = GetAttributeValue(attributes, "sn") ?? "",
                        First_name = GetAttributeValue(attributes, "givenName") ??
                                    GetAttributeValue(attributes, "displayName")?.Split(' ')[0] ?? "",
                        Patronymic = GetAttributeValue(attributes, "initials") ?? "",
                        Birth_date = whenCreated?.AddYears(-25) ?? DateTime.UtcNow.AddYears(-25),
                        Interests = GetAttributeValue(attributes, "description") ?? ""
                    },
                    WorkInfo = new WorkInfo
                    {
                        Position = GetAttributeValue(attributes, "title") ?? "Employee",
                        Department = GetAttributeValue(attributes, "department") ?? "General",
                        Work_exp = whenCreated ?? DateTime.UtcNow.AddYears(-1)
                    },
                    ContactInfo = new ContactInfo
                    {
                        Phone = GetAttributeValue(attributes, "telephoneNumber") ??
                               GetAttributeValue(attributes, "officePhone") ?? "",
                        City = GetAttributeValue(attributes, "l") ??
                              GetAttributeValue(attributes, "co") ??
                              GetAttributeValue(attributes, "physicalDeliveryOfficeName") ?? ""
                    },
                    Created_at = DateTime.UtcNow,
                    Updated_at = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ошибка при маппинге пользователя");
                return null;
            }
        }

        #region Helper Methods

        private string EscapeLdapFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return string.Empty;

            return filter
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }

        private string GetAttributeValue(LdapAttributeSet attributes, string attributeName)
        {
            try
            {
                var attribute = attributes.GetAttribute(attributeName);
                if (attribute != null && attribute.StringValue != null)
                {
                    return attribute.StringValue;
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
            return null;
        }

        private bool IsUserActive(string userAccountControl)
        {
            if (string.IsNullOrEmpty(userAccountControl))
                return false;

            if (int.TryParse(userAccountControl, out int uac))
            {
                const int disabledFlag = 2;
                const int lockedFlag = 16;

                return (uac & disabledFlag) == 0 &&
                       (uac & lockedFlag) == 0;
            }

            return false;
        }

        private DateTime? ParseLdapDate(string ldapDate)
        {
            if (string.IsNullOrEmpty(ldapDate) || ldapDate.Length < 14)
                return null;

            try
            {
                var year = int.Parse(ldapDate.Substring(0, 4));
                var month = int.Parse(ldapDate.Substring(4, 2));
                var day = int.Parse(ldapDate.Substring(6, 2));
                var hour = int.Parse(ldapDate.Substring(8, 2));
                var minute = int.Parse(ldapDate.Substring(10, 2));
                var second = int.Parse(ldapDate.Substring(12, 2));

                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        try
                        {
                            if (_connection.Connected)
                            {
                                _connection.Disconnect();
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки
                        }
                        _connection.Dispose();
                        _connection = null;
                    }
                }
                _disposed = true;
            }
        }

        #endregion
    }
}