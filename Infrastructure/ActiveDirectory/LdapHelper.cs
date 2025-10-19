using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ActiveDirectory
{
    public class LdapHelper
    {
        public static string GetAttributeValue(LdapAttributeSet attributes, string attributeName)
        {
            try
            {
                var attribute = attributes.getAttribute(attributeName);
                return attribute?.StringValue;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsUserActive(string userAccountControl)
        {
            if (string.IsNullOrEmpty(userAccountControl)) return true;

            return !int.TryParse(userAccountControl, out int uac) || (uac & 0x0002) == 0;
        }

        public static DateTime? ParseLdapDate(string ldapDate)
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

        public static DateTime? ParseWindowsFileTime(string fileTimeString)
        {
            if (string.IsNullOrEmpty(fileTimeString) || !long.TryParse(fileTimeString, out long fileTime))
                return null;

            // Windows file time to DateTime
            try
            {
                return DateTime.FromFileTime(fileTime);
            }
            catch
            {
                return null;
            }
        }

        //public static string GetGuidFromBytes(sbyte[] guidBytes)
        //{
        //    if (guidBytes == null || guidBytes.Length != 16) return null;

        //    try
        //    {
        //        var guid = new Guid(byte.Parse(guidBytes));
        //        return guid.ToString();
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
}
