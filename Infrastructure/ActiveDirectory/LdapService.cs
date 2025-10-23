//using Domain.Entities;
//using Microsoft.Extensions.Configuration;
//using System.DirectoryServices;
//using System.DirectoryServices.AccountManagement;

//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Novell.Directory.Ldap;

//namespace Infrastructure.ActiveDirectory
//{
//    class LdapService : ILdapService
//    {

//        private readonly IConfiguration _configuration;

//        public LdapService(IConfiguration configuration)
//        {
//            _configuration = configuration;
//        }

//        public async Task<User> GetUserBySamAccountNameAsync(string samAccountName)
//        {
//            return await Task.Run(() =>
//            {
//                try
//                {
//                    using var connection = new LdapConnection();
//                    connection.Connect("stud.local", 389);
//                    connection.Bind(LdapConnection.Ldap_V3,
//                        _configuration["Ldap:Username"] ?? "",
//                        _configuration["Ldap:Password"] ?? "");

//                    var searchFilter = $"(sAMAccountName={samAccountName})";
//                    var attributes = new[] {
//                        "sAMAccountName", "displayName", "mail", "title", "department",
//                        "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
//                        "givenName", "sn", "initials", "whenCreated", "employeeID",
//                        "distinguishedName", "objectGUID", "userAccountControl",
//                        "company", "description", "officePhone", "mobile", "streetAddress",
//                        "postalCode", "co", "userPrincipalName", "memberOf"
//                    };

//                    var searchResults = connection.Search(
//                        "DC=stud,DC=local",
//                        LdapConnection.SCOPE_SUB,
//                        searchFilter,
//                        attributes,
//                        false
//                    );

//                    if (searchResults.hasMore())
//                    {
//                        var entry = searchResults.next();
//                        return MapLdapEntryToUser(entry);
//                    }

//                    return null;
//                }
//                catch (Exception ex)
//                {
//                    return null;
//                }
//            });
//        }

//        public async Task<List<User>> GetUsersFromActiveDirectoryAsync()
//        {
//            return await Task.Run(() =>
//            {
//                var users = new List<User>();
//                LdapConnection connection = null;

//                try
//                {
//                    using var connection = new LdapConnection();
//                    connection.Connect("stud.local", 389);

//                    connection.Bind(LdapConnection.Ldap_V3, _configuration["Ldap:Username"] ?? "", _configuration["Ldap:Password"] ?? "");

//                    var searchFilter = "(&(objectClass=user)(objectCategory=person))";
//                    var attributes = new[] {
//                        "sAMAccountName", "displayName", "mail", "title", "department",
//                        "manager", "telephoneNumber", "l", "physicalDeliveryOfficeName",
//                        "givenName", "sn", "initials", "whenCreated", "employeeID",
//                        "distinguishedName", "objectGUID", "userAccountControl"
//                    };
//                    var searchResults = connection.Search(
//                        "DC=stud,DC=local", 
//                        LdapConnection.SCOPE_SUB,
//                        searchFilter,
//                        attributes,
//                        false
//                    );
//                    while (searchResults.hasMore())
//                    {
//                        try
//                        {
//                            var entry = searchResults.next();
//                            var user = MapLdapEntryToUser(entry);
//                            if (user != null)
//                                users.Add(user);
//                        }
//                        catch (Exception ex)
//                        {

//                        }
//                    }

//                    connection.Disconnect();



//                }
//                catch (Exception ex)
//                {
//                    throw;
//                }
//                return users;
//            });
//        }

//        private User MapLdapEntryToUser(LdapEntry entry)
//        {
//            var attributes = entry.getAttributeSet();

//            var samAccountName = LdapHelper.GetAttributeValue(attributes, "SamAccountName");
//            if (string.IsNullOrEmpty(samAccountName)) return null;

//            var userAccountControl = LdapHelper.GetAttributeValue(attributes, "userAccountControl");

//            var isActive = LdapHelper.IsUserActive(userAccountControl);
//            if (!isActive)
//            {
//                return null;
//            }

//            var whenCreated = LdapHelper.ParseLdapDate(LdapHelper.GetAttributeValue(attributes, "whenCreated");

//            var pwdLastSet = LdapHelper.ParseWindowsFileTime(LdapHelper.GetAttributeValue(attributes, "pwdLastSet"));

//            var user = new User
//            {
//                SamAccountName = samAccountName,
//                Email = LdapHelper.GetAttributeValue(attributes, "mail") ?? LdapHelper.GetAttributeValue(attributes, "userPrincipalName"),
//                Login = samAccountName,
//                Password = "LDAP_SYNCED_USER",
//                IsActive = isActive,
//                LastAdSync = DateTime.UtcNow,
//                //AdGuid = LdapHelper.GetGuidFromBytes(attributes.getAttribute("objectGUID")?.ByteValue),
//                PersonalInfo = new PersonalInfo
//                {
//                    Last_name = LdapHelper.GetAttributeValue(attributes, "sn") ?? "",
//                    First_name = LdapHelper.GetAttributeValue(attributes, "givenName") ??
//                                LdapHelper.GetAttributeValue(attributes, "displayName")?.Split(' ')[0] ?? "",
//                    Patronymic = LdapHelper.GetAttributeValue(attributes, "initials") ?? "",
//                    Birth_date = whenCreated?.AddYears(-25) ?? DateTime.Now.AddYears(-25), // нет даты рождения
//                    Interests = LdapHelper.GetAttributeValue(attributes, "description") ?? ""

//                },
//                WorkInfo = new WorkInfo
//                {
//                    Position = LdapHelper.GetAttributeValue(attributes, "title") ?? "Employee",
//                    Department = LdapHelper.GetAttributeValue(attributes, "department") ?? "General",
//                    Work_exp = whenCreated ?? DateTime.Now.AddYears(-1)
//                },
//                ContactInfo = new ContactInfo
//                {
//                    Phone = LdapHelper.GetAttributeValue(attributes, "telephoneNumber") ??
//                           LdapHelper.GetAttributeValue(attributes, "officePhone") ?? "",
//                    City = LdapHelper.GetAttributeValue(attributes, "l") ??
//                          LdapHelper.GetAttributeValue(attributes, "co") ??
//                          LdapHelper.GetAttributeValue(attributes, "physicalDeliveryOfficeName")
//                },
//                Created_at = DateTime.UtcNow,
//                Updated_at = DateTime.UtcNow
//            };
//            return user;

//        } 
//    }
//}
