using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class SearchRequestDto
    {
        public string searchCriteria { get; set; }

        public string searchValue { get; set; }

        public int queryAmount { get; set; }

        public bool is_cached { get; set; } = false;
    }

    public class TableRequestDto
    {
        public int page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string? Sort { get; set; }

        // Раздельные фильтры
        public string? PositionFilter { get; set; }
        public string? DepartmentFilter { get; set; }

        public bool isCached { get; set; } = false;
    }

    public class CreateUserDTO
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string Login { get; set; }
    }

    public class LoginDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? Phone { get; set; }
        public string? City { get; set; }
        public string? Interests { get; set; }
        public string? Avatar { get; set; } 
        public Dictionary<string, object>? Contacts { get; set; }
    }
}
