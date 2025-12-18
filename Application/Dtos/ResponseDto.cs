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
        public string? hierarchyColor { get; set; }
    }

   

    public class SynchroResponseDto
    {
        public string Status { get; set; } = "success";
        public int AddedUsers { get; set; }
        public int UpdatedUsers { get; set; }
        public int DeactivatedUsers { get; set; }
        public int DeletedUsers { get; set; }
        public int TotalProcessed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime SyncTimestamp { get; set; } = DateTime.UtcNow;
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

        public Guid? manager_id { get; set; }
        public int? hierarchyId { get; set; }

        public List<string>? Skills { get; set; }
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

    public class SkillListDto
    {
        public List<string> skills;
        public int count;
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

    public class HierarchyNodeDto
    {
        public int HierarchyId { get; set; }
        public int Level { get; set; }
        public string Title { get; set; }
        public string Color { get; set; }
        public List<HierarchyNodeDto> Children { get; set; } = new List<HierarchyNodeDto>();

        public EmployeeHierarchyDto? Manager { get; set; }
        public List<EmployeeFlatDto> Employees { get; set; } = new List<EmployeeFlatDto>();
    }

    public class HierarchyNodeWithoutPersonsDto
    {
        public int HierarchyId { get; set; }
        public int Level { get; set; }
        public string Title { get; set; }
        public string Color { get; set; }
        public List<HierarchyNodeWithoutPersonsDto> Children { get; set; } = new List<HierarchyNodeWithoutPersonsDto>();
    }

    public class DepartmentDetailsDto
    {
        public int HierarchyId { get; set; }

        public string Title { get; set; }

        public EmployeeHierarchyDto? Manager { get; set; }
        public List<EmployeeFlatDto> Employees { get; set; } = new List<EmployeeFlatDto>();
    }
}
