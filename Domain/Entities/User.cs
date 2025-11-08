using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;


namespace Domain.Entities
{
    public class User
    {
        public Guid User_id { get; set; }

        [Required]
        public string Login { get; set; }
        [Required]
        public string Password { get; set; }
        public Guid? Manager_id { get; set; }
        public string Role { get; set; } = AuthOptions.ROLE_USER; 
        public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
        public WorkInfo WorkInfo { get; set; } = new WorkInfo();
        public ContactInfo ContactInfo { get; set; } = new ContactInfo();
        [Column(TypeName = "jsonb")]
        public JObject Contacts { get; set; } = new JObject();

        public string SamAccountName { get; set; } 
        public string Email { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime LastAdSync { get; set; }
        public string AdGuid { get; set; }

        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }

        public User Manager { get; set; }
        public ICollection<User> Subordinates { get; set; } = new List<User>();

        public bool IsInRole(string role) => Role == role;
        public bool IsAdmin() => Role == AuthOptions.ROLE_ADMIN;
        public bool IsHr() => Role == AuthOptions.ROLE_HR || IsAdmin();
        public bool IsUser() => true;

        public void SetContact<T>(string key, T value)
        {
            Contacts[key] = JToken.FromObject(value);
            Updated_at = DateTime.UtcNow;
        }

        public T GetContact<T>(string key, T defaultValue = default(T))
        {
            return Contacts.TryGetValue(key, out var token) ? token.ToObject<T>() : defaultValue;
        }

        public T GetContact<T>(string key)
        {
            return Contacts[key].ToObject<T>();
        }

        public bool HasContact(string key)
        {
            return Contacts.ContainsKey(key);
        }

        public void RemoveAttribute(string key)
        {
            Contacts.Remove(key);
            Updated_at = DateTime.UtcNow;
        }

        public void UpdateContacts(object contacts)
        {
            var newAttributes = JObject.FromObject(contacts);
            Contacts.Merge(newAttributes);
            Updated_at = DateTime.UtcNow;
        }

        public Dictionary<string, object> GetContactsDictionary()
        {
            return Contacts.ToObject<Dictionary<string, object>>();
        }

        public string GetFullName() => $"{PersonalInfo.Last_name} {PersonalInfo.First_name} {PersonalInfo.Patronymic}".Trim();

        public int CalculateAge()
        {
            var today = DateTime.Today;
            var age = today.Year - PersonalInfo.Birth_date.Year;
            if (PersonalInfo.Birth_date.Date > today.AddYears(-age)) age--;
            return age;
        }

        public int CalculateWorkExperience()
        {
            var today = DateTime.Today;
            var experience = today.Year - WorkInfo.Work_exp.Year;
            if (WorkInfo.Work_exp.Date > today.AddYears(-experience)) experience--;
            return experience;
        }
    }
}
