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

        public LdapService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private LdapConnection GetConnection()
        {
            var connection = new LdapConnection();
            return connection;
        }

        private void ConnectAndBind(LdapConnection connection)
        {
            var server = _configuration["Ldap:Server"] ?? "stud.local";
            var port = int.Parse(_configuration["Ldap:Port"] ?? "389");
            var username = _configuration["Ldap:Username"] ?? "";
            var password = _configuration["Ldap:Password"] ?? "";

            try
            {
                connection.Connect(server, port);
                connection.Bind(LdapConnection.Ldap_V3, username, password);
            }
            catch (Exception ex)
            {
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
                    var attributes = new[] {
                        "sAMAccountName", "displayName", "mail", "title", "department",
                        "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
                        "givenName", "sn", "initials", "whenCreated", "employeeID",
                        "distinguishedName", "objectGUID", "userAccountControl",
                        "company", "description", "officePhone", "mobile", "streetAddress",
                        "postalCode", "co", "userPrincipalName", "memberOf"
                    };

                    var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";

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
                        return MapLdapEntryToUser(entry);
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    return null;
                }
                finally
                {
                    connection?.Disconnect();
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

                    var searchFilter = "(&(objectClass=user)(objectCategory=person))";
                    var attributes = new[] {
                        "sAMAccountName", "displayName", "mail", "title", "department",
                        "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
                        "givenName", "sn", "initials", "whenCreated", "employeeID",
                        "distinguishedName", "objectGUID", "userAccountControl",
                        "company", "description", "officePhone", "mobile"
                    };

                    var searchBase = _configuration["Ldap:SearchBase"] ?? "DC=stud,DC=local";

                    var searchResults = connection.Search(
                        searchBase,
                        LdapConnection.SCOPE_SUB,
                        searchFilter,
                        attributes,
                        false
                    );

                    while (searchResults.hasMore())
                    {
                        try
                        {
                            var entry = searchResults.next();
                            var user = MapLdapEntryToUser(entry);
                            if (user != null)
                                users.Add(user);
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    return users;
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    connection?.Disconnect();
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

                    // Получаем организационные единицы
                    var ouResults = connection.Search(
                        searchBase,
                        LdapConnection.SCOPE_SUB,
                        "(objectClass=organizationalUnit)",
                        new[] { "ou", "description", "distinguishedName" },
                        false
                    );

                    var ous = new List<LdapOrganizationalUnit>();
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
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    // Получаем пользователей с информацией о подразделениях
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
                    while (userResults.hasMore())
                    {
                        try
                        {
                            var entry = userResults.next();
                            var attributes = entry.getAttributeSet();

                            var userAccountControl = LdapHelper.GetAttributeValue(attributes, "userAccountControl");
                            if (!LdapHelper.IsUserActive(userAccountControl))
                                continue;

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
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    return new LdapHierarchyResponse
                    {
                        OrganizationalUnits = ous,
                        Users = users,
                        TotalUsers = users.Count,
                        TotalOUs = ous.Count
                    };
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    connection?.Disconnect();
                }
            });
        }

        private User MapLdapEntryToUser(LdapEntry entry)
        {
            var attributes = entry.getAttributeSet();

            var samAccountName = LdapHelper.GetAttributeValue(attributes, "sAMAccountName");
            if (string.IsNullOrEmpty(samAccountName)) return null;

            var userAccountControl = LdapHelper.GetAttributeValue(attributes, "userAccountControl");
            var isActive = LdapHelper.IsUserActive(userAccountControl);
            if (!isActive) return null;

            var whenCreated = LdapHelper.ParseLdapDate(LdapHelper.GetAttributeValue(attributes, "whenCreated"));

            return new User
            {
                SamAccountName = samAccountName,
                Email = LdapHelper.GetAttributeValue(attributes, "mail") ?? LdapHelper.GetAttributeValue(attributes, "userPrincipalName"),
                Login = samAccountName,
                Password = "LDAP_SYNCED_USER",
                IsActive = isActive,
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