using Domain.Entities;
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
        public UserTreeDto? Manager { get; set; } = new();

        public List<UserTreeDto> Subordinates { get; set; } = new();

        public string UserName { get; set; }

        public string? Position  { get; set; }

        public string? Department { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
