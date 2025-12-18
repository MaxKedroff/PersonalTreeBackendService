using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.ActiveDirectory
{
    public class LdapService : ILdapService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LdapService> _logger;
        private LdapConnection _connection;
        private bool _isInitialized = false;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public LdapService(IConfiguration configuration, ILogger<LdapService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<LdapConnection> GetConnectionAsync()
        {
            // Используем двойную проверку для потокобезопасности
            if (_connection != null && _isInitialized)
            {
                return _connection;
            }

            lock (_lock)
            {
                if (_connection != null && _isInitialized)
                {
                    return _connection;
                }

                try
                {
                    // Закрываем старое соединение если есть
                    if (_connection != null)
                    {
                        _connection.Dispose();
                    }

                    var server = _configuration["Ldap:Server"] ?? "10.51.4.18";
                    var port = int.Parse(_configuration["Ldap:Port"] ?? "389");
                    var username = _configuration["Ldap:Username"] ?? "STUD\\Administrator";
                    var password = _configuration["Ldap:Password"] ?? "hf8-5Bu3YMy):{uP;x";

                    _logger.LogInformation(
                        "Создание LDAP подключения к {Server}:{Port} как {Username}",
                        server, port, username);

                    // Создаем идентификатор сервера
                    var identifier = new LdapDirectoryIdentifier(server, port);

                    // Создаем соединение
                    _connection = new LdapConnection(identifier)
                    {
                        Timeout = TimeSpan.FromSeconds(30),
                        AutoBind = false
                    };

                    // Настраиваем параметры сессии
                    _connection.SessionOptions.ProtocolVersion = 3;
                    _connection.SessionOptions.SecureSocketLayer = false;

                    // Отключаем проверку сертификатов если используем LDAPS
                    _connection.SessionOptions.VerifyServerCertificate = (conn, cert) => true;

                    // Настраиваем кэширование
                    _connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
                    _connection.SessionOptions.ReferralHopLimit = 1;

                    // Создаем учетные данные
                    var credentials = new NetworkCredential(username, password);

                    // Подключаемся и аутентифицируемся
                    _connection.Bind(credentials);

                    _isInitialized = true;
                    _logger.LogInformation("LDAP подключение успешно установлено");

                    return _connection;
                }
                catch (LdapException ex)
                {
                    _logger.LogError(ex, "Ошибка LDAP при создании подключения. Код ошибки: {ErrorCode}", ex.ErrorCode);
                    _connection?.Dispose();
                    _connection = null;
                    _isInitialized = false;
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Неизвестная ошибка при создании LDAP подключения");
                    _connection?.Dispose();
                    _connection = null;
                    _isInitialized = false;
                    throw;
                }
            }
        }

        private async Task<bool> IsConnectionAliveAsync()
        {
            if (_connection == null || !_isInitialized)
                return false;

            try
            {
                // Пробуем выполнить простой запрос чтобы проверить соединение
                var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";
                var request = new SearchRequest(
                    searchBase,
                    "(objectClass=*)",
                    SearchScope.Base,
                    "1.1" // Только для проверки, не возвращаем атрибуты
                );

                request.Controls.Add(new DirectoryControl("1.2.840.113556.1.4.1781", null, false, true));

                var response = (SearchResponse)_connection.SendRequest(request, TimeSpan.FromSeconds(5));
                return true;
            }
            catch
            {
                // Соединение нерабочее
                _isInitialized = false;
                return false;
            }
        }

        private async Task<LdapConnection> GetValidConnectionAsync()
        {
            var connection = await GetConnectionAsync();

            // Проверяем живое ли соединение
            var isAlive = await IsConnectionAliveAsync();
            if (!isAlive)
            {
                lock (_lock)
                {
                    // Сбрасываем флаг и пересоздаем соединение
                    _isInitialized = false;
                    _connection?.Dispose();
                    _connection = null;
                }

                // Получаем новое соединение
                connection = await GetConnectionAsync();
            }

            return connection;
        }

        public async Task<User> GetUserBySamAccountNameAsync(string samAccountName)
        {
            LdapConnection connection = null;

            try
            {
                connection = await GetValidConnectionAsync();
                var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";

                // Экранируем специальные символы
                var escapedSamAccountName = EscapeLdapFilter(samAccountName);
                var searchFilter = $"(sAMAccountName={escapedSamAccountName})";

                _logger.LogDebug("Поиск пользователя: {Filter} в {Base}", searchFilter, searchBase);

                var attributes = new[] {
                    "sAMAccountName", "displayName", "mail", "title", "department",
                    "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
                    "givenName", "sn", "initials", "whenCreated", "employeeID",
                    "distinguishedName", "objectGUID", "userAccountControl",
                    "company", "description", "officePhone", "mobile", "streetAddress",
                    "postalCode", "co", "userPrincipalName", "memberOf"
                };

                var request = new SearchRequest(
                    searchBase,
                    searchFilter,
                    SearchScope.Subtree,
                    attributes
                );

                var response = (SearchResponse)connection.SendRequest(request);

                if (response.Entries.Count > 0)
                {
                    var entry = response.Entries[0];
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
            catch (DirectoryOperationException ex)
            {
                _logger.LogError(ex, "Ошибка LDAP при поиске пользователя {SamAccountName}: {Message}",
                    samAccountName, ex.Message);
                return null;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "Ошибка LDAP при поиске пользователя {SamAccountName}: {ErrorCode}",
                    samAccountName, ex.ErrorCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неизвестная ошибка при поиске пользователя {SamAccountName}",
                    samAccountName);
                return null;
            }
        }

        public async Task<List<User>> GetUsersFromActiveDirectoryAsync()
        {
            var users = new List<User>();
            LdapConnection connection = null;

            try
            {
                connection = await GetValidConnectionAsync();
                var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";
                var searchFilter = "(&(objectClass=user)(objectCategory=person))";

                _logger.LogInformation("Начало массовой синхронизации из LDAP. База: {Base}", searchBase);

                var attributes = new[] {
                    "sAMAccountName", "displayName", "mail", "title", "department",
                    "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
                    "givenName", "sn", "initials", "whenCreated", "employeeID",
                    "distinguishedName", "objectGUID", "userAccountControl",
                    "company", "description", "officePhone", "mobile"
                };

                // Используем пейджинг для больших наборов данных
                var pageSize = 1000;
                byte[] pageCookie = null;
                int processed = 0, skipped = 0;

                do
                {
                    var request = new SearchRequest(
                        searchBase,
                        searchFilter,
                        SearchScope.Subtree,
                        attributes
                    );

                    // Добавляем контроль пейджинга
                    var pageRequest = new PageResultRequestControl(pageSize);
                    if (pageCookie != null)
                    {
                        pageRequest.Cookie = pageCookie;
                    }
                    request.Controls.Add(pageRequest);

                    // Отправляем запрос
                    var response = (SearchResponse)connection.SendRequest(request);

                    // Обрабатываем результаты
                    foreach (SearchResultEntry entry in response.Entries)
                    {
                        try
                        {
                            var user = MapLdapEntryToUser(entry);
                            if (user != null)
                            {
                                users.Add(user);
                                processed++;
                            }
                            else
                            {
                                skipped++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при обработке записи LDAP");
                            skipped++;
                        }
                    }

                    // Получаем cookie для следующей страницы
                    PageResultResponseControl pageResponse = null;
                    foreach (DirectoryControl control in response.Controls)
                    {
                        if (control is PageResultResponseControl)
                        {
                            pageResponse = (PageResultResponseControl)control;
                            break;
                        }
                    }

                    if (pageResponse != null)
                    {
                        pageCookie = pageResponse.Cookie;

                        // Логируем прогресс
                        if (processed % 1000 == 0)
                        {
                            _logger.LogInformation("Обработано {Processed} пользователей...", processed);
                        }
                    }
                    else
                    {
                        pageCookie = null;
                    }

                } while (pageCookie != null && pageCookie.Length > 0);

                _logger.LogInformation(
                    "Синхронизация завершена: найдено {Processed} пользователей, пропущено {Skipped}",
                    processed, skipped);

                return users;
            }
            catch (DirectoryOperationException ex)
            {
                _logger.LogError(ex, "Ошибка LDAP при массовом получении пользователей: {Message}", ex.Message);
                throw;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "Ошибка LDAP при массовом получении пользователей: {ErrorCode}", ex.ErrorCode);
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
                connection = await GetValidConnectionAsync();
                var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";

                _logger.LogInformation("Получение иерархии LDAP из {Base}", searchBase);

                var response = new LdapHierarchyResponse();

                // Получаем OU (организационные подразделения)
                var ouRequest = new SearchRequest(
                    searchBase,
                    "(objectClass=organizationalUnit)",
                    SearchScope.Subtree,
                    new[] { "ou", "description", "distinguishedName" }
                );

                var ouResponse = (SearchResponse)connection.SendRequest(ouRequest);
                foreach (SearchResultEntry entry in ouResponse.Entries)
                {
                    response.OrganizationalUnits.Add(new LdapOrganizationalUnit
                    {
                        Name = GetAttributeValue(entry, "ou"),
                        Description = GetAttributeValue(entry, "description"),
                        DistinguishedName = GetAttributeValue(entry, "distinguishedName")
                    });
                }

                // Получаем пользователей
                var userRequest = new SearchRequest(
                    searchBase,
                    "(&(objectClass=user)(objectCategory=person))",
                    SearchScope.Subtree,
                    new[] {
                        "sAMAccountName", "displayName", "title", "department",
                        "manager", "distinguishedName", "userAccountControl",
                        "givenName", "sn", "mail", "telephoneNumber", "physicalDeliveryOfficeName"
                    }
                );

                var userResponse = (SearchResponse)connection.SendRequest(userRequest);
                int inactiveSkipped = 0;

                foreach (SearchResultEntry entry in userResponse.Entries)
                {
                    var userAccountControl = GetAttributeValue(entry, "userAccountControl");
                    if (!IsUserActive(userAccountControl))
                    {
                        inactiveSkipped++;
                        continue;
                    }

                    response.Users.Add(new LdapUserInfo
                    {
                        SamAccountName = GetAttributeValue(entry, "sAMAccountName"),
                        DisplayName = GetAttributeValue(entry, "displayName"),
                        FirstName = GetAttributeValue(entry, "givenName"),
                        LastName = GetAttributeValue(entry, "sn"),
                        Title = GetAttributeValue(entry, "title"),
                        Department = GetAttributeValue(entry, "department"),
                        Manager = GetAttributeValue(entry, "manager"),
                        Email = GetAttributeValue(entry, "mail"),
                        Phone = GetAttributeValue(entry, "telephoneNumber"),
                        Office = GetAttributeValue(entry, "physicalDeliveryOfficeName"),
                        DistinguishedName = GetAttributeValue(entry, "distinguishedName")
                    });
                }

                if (inactiveSkipped > 0)
                {
                    _logger.LogDebug("Пропущено {Count} неактивных пользователей", inactiveSkipped);
                }

                response.TotalUsers = response.Users.Count;
                response.TotalOUs = response.OrganizationalUnits.Count;

                _logger.LogInformation(
                    "Иерархия собрана: {Users} пользователей, {OUs} OU",
                    response.TotalUsers, response.TotalOUs);

                return response;
            }
            catch (DirectoryOperationException ex)
            {
                _logger.LogError(ex, "Ошибка LDAP при получении иерархии: {Message}", ex.Message);
                throw;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "Ошибка LDAP при получении иерархии: {ErrorCode}", ex.ErrorCode);
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
                var connection = await GetValidConnectionAsync();

                // Пробуем выполнить простой запрос к корню
                var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";
                var request = new SearchRequest(
                    searchBase,
                    "(objectClass=*)",
                    SearchScope.Base,
                    new[] { "distinguishedName" }
                );

                var response = (SearchResponse)connection.SendRequest(request, TimeSpan.FromSeconds(10));

                _logger.LogInformation("Тест подключения к LDAP: УСПЕШНО");
                return true;
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
                var server = _configuration["Ldap:Server"] ?? "10.51.4.18";
                var port = int.Parse(_configuration["Ldap:Port"] ?? "389");

                using (var testConnection = new LdapConnection(new LdapDirectoryIdentifier(server, port)))
                {
                    testConnection.Timeout = TimeSpan.FromSeconds(10);
                    testConnection.SessionOptions.ProtocolVersion = 3;
                    testConnection.SessionOptions.SecureSocketLayer = false;

                    var credentials = new NetworkCredential(username, password);
                    testConnection.Bind(credentials);

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private User MapLdapEntryToUser(SearchResultEntry entry)
        {
            try
            {
                var samAccountName = GetAttributeValue(entry, "sAMAccountName");
                if (string.IsNullOrEmpty(samAccountName))
                {
                    return null;
                }

                var userAccountControl = GetAttributeValue(entry, "userAccountControl");
                if (!IsUserActive(userAccountControl))
                {
                    _logger.LogTrace("Пропуск неактивного пользователя: {SamAccountName}", samAccountName);
                    return null;
                }

                var whenCreated = ParseLdapDate(GetAttributeValue(entry, "whenCreated"));

                return new User
                {
                    SamAccountName = samAccountName,
                    Email = GetAttributeValue(entry, "mail") ?? GetAttributeValue(entry, "userPrincipalName"),
                    Login = samAccountName,
                    Password = "LDAP_SYNCED_USER",
                    IsActive = true,
                    LastAdSync = DateTime.UtcNow,
                    PersonalInfo = new PersonalInfo
                    {
                        Last_name = GetAttributeValue(entry, "sn") ?? "",
                        First_name = GetAttributeValue(entry, "givenName") ??
                                    GetAttributeValue(entry, "displayName")?.Split(' ')[0] ?? "",
                        Patronymic = GetAttributeValue(entry, "initials") ?? "",
                        Birth_date = whenCreated?.AddYears(-25) ?? DateTime.UtcNow.AddYears(-25),
                        Interests = GetAttributeValue(entry, "description") ?? ""
                    },
                    WorkInfo = new WorkInfo
                    {
                        Position = GetAttributeValue(entry, "title") ?? "Employee",
                        Department = GetAttributeValue(entry, "department") ?? "General",
                        Work_exp = whenCreated ?? DateTime.UtcNow.AddYears(-1)
                    },
                    ContactInfo = new ContactInfo
                    {
                        Phone = GetAttributeValue(entry, "telephoneNumber") ??
                               GetAttributeValue(entry, "officePhone") ?? "",
                        City = GetAttributeValue(entry, "l") ??
                              GetAttributeValue(entry, "co") ??
                              GetAttributeValue(entry, "physicalDeliveryOfficeName") ?? ""
                    },
                    Created_at = DateTime.UtcNow,
                    Updated_at = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ошибка при маппинге пользователя из LDAP");
                return null;
            }
        }

        #region Helper Methods

        private string EscapeLdapFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return string.Empty;

            // Экранирование специальных символов для LDAP фильтров
            var result = filter
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");

            return result;
        }

        private string GetAttributeValue(SearchResultEntry entry, string attributeName)
        {
            try
            {
                if (entry.Attributes.Contains(attributeName))
                {
                    var attribute = entry.Attributes[attributeName];
                    if (attribute != null && attribute.Count > 0)
                    {
                        return attribute[0]?.ToString();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки получения атрибутов
            }
            return null;
        }

        private bool IsUserActive(string userAccountControl)
        {
            if (string.IsNullOrEmpty(userAccountControl))
                return false;

            if (int.TryParse(userAccountControl, out int uac))
            {
                const int disabledFlag = 2; // ADS_UF_ACCOUNTDISABLE
                const int lockedFlag = 16; // ADS_UF_LOCKOUT
                const int passwordExpiredFlag = 8388608; // ADS_UF_PASSWORD_EXPIRED

                return (uac & disabledFlag) == 0 &&
                       (uac & lockedFlag) == 0 &&
                       (uac & passwordExpiredFlag) == 0;
            }

            return false;
        }

        private DateTime? ParseLdapDate(string ldapDate)
        {
            if (string.IsNullOrEmpty(ldapDate) || ldapDate.Length < 14)
                return null;

            try
            {
                // Формат LDAP: yyyyMMddHHmmss.0Z
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
                    // Освобождаем управляемые ресурсы
                    if (_connection != null)
                    {
                        _connection.Dispose();
                        _connection = null;
                    }
                }

                _disposed = true;
                _isInitialized = false;
            }
        }

        #endregion
    }
}