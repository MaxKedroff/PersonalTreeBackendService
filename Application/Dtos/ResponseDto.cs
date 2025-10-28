using Domain.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class ResponseTableUsersDto
    {
        public int AmountOfUsers { get; set; }
        public List<TableUserDto> UsersTable { get; set; } = new();
        public bool IsCached { get; set; } = false;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class TableUserDto
    {
        public string UserName { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
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


    public class DepartmentHierarchyDto
    {
        public string Department { get; set; }
        public List<EmployeeHierarchyDto> Employees { get; set; } = new List<EmployeeHierarchyDto>();
    }

    public class EmployeeHierarchyDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Position { get; set; }
        public string AvatarUrl { get; set; }
        public List<EmployeeHierarchyDto> Subordinates { get; set; } = new List<EmployeeHierarchyDto>();
    }

    public class HierarchyResponseDto
    {
        public EmployeeHierarchyDto Ceo { get; set; }
        public List<DepartmentHierarchyDto> Departments { get; set; } = new List<DepartmentHierarchyDto>();
        public int TotalEmployees { get; set; }
    }
}
