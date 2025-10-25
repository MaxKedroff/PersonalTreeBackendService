using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ActiveDirectory
{
    public class LdapHierarchyResponse
    {
        public List<LdapOrganizationalUnit> OrganizationalUnits { get; set; } = new List<LdapOrganizationalUnit>();
        public List<LdapUserInfo> Users { get; set; } = new List<LdapUserInfo>();
        public int TotalUsers { get; set; }
        public int TotalOUs { get; set; }
    }

    public class LdapOrganizationalUnit
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DistinguishedName { get; set; }
    }

    public class LdapUserInfo
    {
        public string SamAccountName { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public string Department { get; set; }
        public string Manager { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Office { get; set; }
        public string DistinguishedName { get; set; }
    }
}
