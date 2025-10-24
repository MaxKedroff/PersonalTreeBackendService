using Domain.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class ResponseUsersTreeDto
    {
        public int AmountOfUsers { get; set; }

        public List<UserTreeDto> UsersTree { get; set; } = new();

        public bool IsCached = false;
    }

    public class UserTreeDto
    {
        public Guid? ManagerId { get; set; }
        public string ManagerName { get; set; }

        public List<UserTreeItemDto> Subordinates { get; set; } = new();

        public string UserName { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? AvatarUrl { get; set; }
        public Guid UserId { get; set; }
    }

    public class UserTreeItemDto
    {
        public string UserName { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? AvatarUrl { get; set; }
        public Guid UserId { get; set; }
    }

    public class UserDetailInfoDto
    {
        public Guid User_id { get; set; }

        public string UserName { get; set; }

        public DateTime BornDate { get; set; }

        public string Department { get; set; }

        public string Position { get; set; }

        public DateTime WorkExperience { get; set; }

        public string PhoneNumber { get; set; }

        public string City { get; set; }

        public string Interests { get; set; }

        public string avatar { get; set; }

        public JObject Contacts { get; set; } = new JObject();
    }

    public class SearchResponseDto
    {
        public int amount;
        public List<SearchItemDto> searchItems;
        public bool is_cached = false;
    }

    public class SearchItemDto
    {
        public string username;
        public string department;
        public string position;
    }

}
