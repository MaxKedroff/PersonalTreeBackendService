using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.ActiveDirectory
{
    public class LdapService : ILdapService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LdapService> _logger;

        public LdapService(IConfiguration configuration, ILogger<LdapService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private LdapConnection GetConnection()
        {
            try
            {
                var connection = new LdapConnection();
                _logger.LogDebug("Создано новое LDAP-соединение (LdapConnection)");
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось создать LDAP-соединение");
                throw;
            }
        }

        private void ConnectAndBind(LdapConnection connection)
        {
            var server = _configuration["Ldap:Server"] ?? "stud.local";
            var port = int.Parse(_configuration["Ldap:Port"] ?? "389");
            var username = _configuration["Ldap:Username"] ?? "";
            var password = _configuration["Ldap:Password"] ?? "";
            _logger.LogInformation("LDAP Username: {Username}, Password length: {Length}, password: {Password}",
    username, password?.Length ?? 0, password);
            try
            {
                _logger.LogInformation("Подключение к LDAP-серверу: {Server}:{Port} под пользователем {Username}", server, port, username);
                connection.Connect(server, port);
                connection.Bind(LdapConnection.Ldap_V3, username, password);
                _logger.LogInformation("Успешно подключено и авторизовано в LDAP");
            }
            catch (LdapException ldapEx)
            {
                _logger.LogError(ldapEx, "Ошибка LDAP при подключении или авторизации. Код: {ResultCode}, Сообщение: {Message}", ldapEx.ResultCode, ldapEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при подключении к LDAP-серверу: {Server}:{Port}", server, port);
                throw;
            }
        }

        public async Task<User> GetUserBySamAccountNameAsync(string samAccountName)
        {
            return await Task.Run(() =>
            {
                LdapConnection connection = null;
                try
                {
                    connection = GetConnection();
                    ConnectAndBind(connection);

                    var searchFilter = $"(sAMAccountName={EscapeLdapFilter(samAccountName)})";
                    var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";
                    var attributes = new[] {
                        "sAMAccountName", "displayName", "mail", "title", "department",
                        "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
                        "givenName", "sn", "initials", "whenCreated", "employeeID",
                        "distinguishedName", "objectGUID", "userAccountControl",
                        "company", "description", "officePhone", "mobile", "streetAddress",
                        "postalCode", "co", "userPrincipalName", "memberOf"
                    };

                    _logger.LogInformation("Поиск пользователя по sAMAccountName: {SamAccountName} в базе {SearchBase}", samAccountName, searchBase);

                    var searchResults = connection.Search(
                        searchBase,
                        LdapConnection.SCOPE_SUB,
                        searchFilter,
                        attributes,
                        false
                    );

                    if (searchResults.hasMore())
                    {
                        var entry = searchResults.next();
                        var user = MapLdapEntryToUser(entry);
                        if (user != null)
                        {
                            _logger.LogInformation("Пользователь найден: {DisplayName} ({SamAccountName})", user.PersonalInfo?.First_name, user.SamAccountName);
                            return user;
                        }
                        else
                        {
                            _logger.LogWarning("Пользователь {SamAccountName} найден, но не прошёл фильтрацию (неактивен или некорректные данные)", samAccountName);
                            return null;
                        }
                    }

                    _logger.LogWarning("Пользователь с sAMAccountName={SamAccountName} не найден в LDAP", samAccountName);
                    return null;
                }
                catch (LdapException ldapEx)
                {
                    _logger.LogError(ldapEx, "LDAP-ошибка при поиске пользователя {SamAccountName}: {ResultCode} {Message}", samAccountName, ldapEx.ResultCode, ldapEx.Message);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Неизвестная ошибка при поиске пользователя {SamAccountName}", samAccountName);
                    return null;
                }
                finally
                {
                    if (connection?.Connected == true)
                    {
                        connection.Disconnect();
                        _logger.LogDebug("LDAP-соединение закрыто");
                    }
                }
            });
        }

        public async Task<List<User>> GetUsersFromActiveDirectoryAsync()
        {
            return await Task.Run(() =>
            {
                var users = new List<User>();
                LdapConnection connection = null;

                try
                {
                    connection = GetConnection();
                    ConnectAndBind(connection);

                    var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";
                    var searchFilter = "(&(objectClass=user)(objectCategory=person))";

                    _logger.LogInformation("Начало массовой синхронизации пользователей из LDAP. База: {SearchBase}, Фильтр: {SearchFilter}", searchBase, searchFilter);

                    var searchResults = connection.Search(
                        searchBase,
                        LdapConnection.SCOPE_SUB,
                        searchFilter,
                        new[] {
                            "sAMAccountName", "displayName", "mail", "title", "department",
                            "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
                            "givenName", "sn", "initials", "whenCreated", "employeeID",
                            "distinguishedName", "objectGUID", "userAccountControl",
                            "company", "description", "officePhone", "mobile"
                        },
                        false
                    );

                    int processed = 0, skipped = 0;
                    while (searchResults.hasMore())
                    {
                        try
                        {
                            var entry = searchResults.next();
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
                            _logger.LogWarning(ex, "Ошибка при обработке одного из LDAP-объектов (пропуск)");
                            skipped++;
                        }
                    }

                    _logger.LogInformation("Синхронизация LDAP завершена: найдено {Processed} активных пользователей, пропущено {Skipped}", processed, skipped);
                    return users;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Критическая ошибка при массовом получении пользователей из LDAP");
                    throw;
                }
                finally
                {
                    if (connection?.Connected == true)
                    {
                        connection.Disconnect();
                        _logger.LogDebug("LDAP-соединение закрыто после массовой синхронизации");
                    }
                }
            });
        }

        public async Task<LdapHierarchyResponse> GetLdapHierarchyAsync()
        {
            return await Task.Run(() =>
            {
                LdapConnection connection = null;
                try
                {
                    connection = GetConnection();
                    ConnectAndBind(connection);

                    var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";
                    _logger.LogInformation("Получение иерархии LDAP: поиск OUs и пользователей в базе {SearchBase}", searchBase);

                    // Поиск организационных единиц
                    var ouResults = connection.Search(
                        searchBase,
                        LdapConnection.SCOPE_SUB,
                        "(objectClass=organizationalUnit)",
                        new[] { "ou", "description", "distinguishedName" },
                        false
                    );

                    var ous = new List<LdapOrganizationalUnit>();
                    int ouCount = 0;
                    while (ouResults.hasMore())
                    {
                        try
                        {
                            var entry = ouResults.next();
                            var attributes = entry.getAttributeSet();

                            ous.Add(new LdapOrganizationalUnit
                            {
                                Name = LdapHelper.GetAttributeValue(attributes, "ou"),
                                Description = LdapHelper.GetAttributeValue(attributes, "description"),
                                DistinguishedName = LdapHelper.GetAttributeValue(attributes, "distinguishedName")
                            });
                            ouCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при обработке OU");
                        }
                    }
                    _logger.LogDebug("Найдено {OuCount} организационных единиц", ouCount);

                    // Поиск пользователей
                    var userResults = connection.Search(
                        searchBase,
                        LdapConnection.SCOPE_SUB,
                        "(&(objectClass=user)(objectCategory=person))",
                        new[] {
                            "sAMAccountName", "displayName", "title", "department",
                            "manager", "distinguishedName", "userAccountControl",
                            "givenName", "sn", "mail", "telephoneNumber", "physicalDeliveryOfficeName"
                        },
                        false
                    );

                    var users = new List<LdapUserInfo>();
                    int userCount = 0, inactiveSkipped = 0;
                    while (userResults.hasMore())
                    {
                        try
                        {
                            var entry = userResults.next();
                            var attributes = entry.getAttributeSet();

                            var userAccountControl = LdapHelper.GetAttributeValue(attributes, "userAccountControl");
                            if (!LdapHelper.IsUserActive(userAccountControl))
                            {
                                inactiveSkipped++;
                                continue;
                            }

                            users.Add(new LdapUserInfo
                            {
                                SamAccountName = LdapHelper.GetAttributeValue(attributes, "sAMAccountName"),
                                DisplayName = LdapHelper.GetAttributeValue(attributes, "displayName"),
                                FirstName = LdapHelper.GetAttributeValue(attributes, "givenName"),
                                LastName = LdapHelper.GetAttributeValue(attributes, "sn"),
                                Title = LdapHelper.GetAttributeValue(attributes, "title"),
                                Department = LdapHelper.GetAttributeValue(attributes, "department"),
                                Manager = LdapHelper.GetAttributeValue(attributes, "manager"),
                                Email = LdapHelper.GetAttributeValue(attributes, "mail"),
                                Phone = LdapHelper.GetAttributeValue(attributes, "telephoneNumber"),
                                Office = LdapHelper.GetAttributeValue(attributes, "physicalDeliveryOfficeName"),
                                DistinguishedName = LdapHelper.GetAttributeValue(attributes, "distinguishedName")
                            });
                            userCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при обработке пользователя");
                        }
                    }

                    _logger.LogInformation("Иерархия LDAP собрана: {UserCount} активных пользователей, {OuCount} OUs", userCount, ouCount);

                    return new LdapHierarchyResponse
                    {
                        OrganizationalUnits = ous,
                        Users = users,
                        TotalUsers = userCount,
                        TotalOUs = ouCount
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при получении иерархии LDAP");
                    throw;
                }
                finally
                {
                    if (connection?.Connected == true)
                    {
                        connection.Disconnect();
                        _logger.LogDebug("LDAP-соединение закрыто после получения иерархии");
                    }
                }
            });
        }

        private User MapLdapEntryToUser(LdapEntry entry)
        {
            try
            {
                var attributes = entry.getAttributeSet();
                var samAccountName = LdapHelper.GetAttributeValue(attributes, "sAMAccountName");
                if (string.IsNullOrEmpty(samAccountName))
                {
                    return null;
                }

                var userAccountControl = LdapHelper.GetAttributeValue(attributes, "userAccountControl");
                var isActive = LdapHelper.IsUserActive(userAccountControl);
                if (!isActive)
                {
                    _logger.LogDebug("Пропуск: пользователь {SamAccountName} неактивен (userAccountControl={UserAccountControl})", samAccountName, userAccountControl);
                    return null;
                }

                var whenCreated = LdapHelper.ParseLdapDate(LdapHelper.GetAttributeValue(attributes, "whenCreated"));

                _logger.LogTrace("Создание пользователя из LDAP: {SamAccountName}", samAccountName);

                return new User
                {
                    SamAccountName = samAccountName,
                    Email = LdapHelper.GetAttributeValue(attributes, "mail") ?? LdapHelper.GetAttributeValue(attributes, "userPrincipalName"),
                    Login = samAccountName,
                    Password = "LDAP_SYNCED_USER",
                    IsActive = true,
                    LastAdSync = DateTime.UtcNow,
                    PersonalInfo = new PersonalInfo
                    {
                        Last_name = LdapHelper.GetAttributeValue(attributes, "sn") ?? "",
                        First_name = LdapHelper.GetAttributeValue(attributes, "givenName") ??
                                    LdapHelper.GetAttributeValue(attributes, "displayName")?.Split(' ')[0] ?? "",
                        Patronymic = LdapHelper.GetAttributeValue(attributes, "initials") ?? "",
                        Birth_date = whenCreated?.AddYears(-25) ?? DateTime.UtcNow.AddYears(-25),
                        Interests = LdapHelper.GetAttributeValue(attributes, "description") ?? ""
                    },
                    WorkInfo = new WorkInfo
                    {
                        Position = LdapHelper.GetAttributeValue(attributes, "title") ?? "Employee",
                        Department = LdapHelper.GetAttributeValue(attributes, "department") ?? "General",
                        Work_exp = whenCreated ?? DateTime.UtcNow.AddYears(-1)
                    },
                    ContactInfo = new ContactInfo
                    {
                        Phone = LdapHelper.GetAttributeValue(attributes, "telephoneNumber") ??
                               LdapHelper.GetAttributeValue(attributes, "officePhone") ?? "",
                        City = LdapHelper.GetAttributeValue(attributes, "l") ??
                              LdapHelper.GetAttributeValue(attributes, "co") ??
                              LdapHelper.GetAttributeValue(attributes, "physicalDeliveryOfficeName") ?? ""
                    },
                    Created_at = DateTime.UtcNow,
                    Updated_at = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

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
    }
}
