using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public string? SearchText { get; set; }


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

        public string? Position { get; set; }

        public string? Department { get; set; }
    }

    public class RemoveAddSkillDto
    {
        public string skill;
        public Guid userId;
    }
    

    public class MoveUserRequestDto
    {
        [Required]
        public Guid UserId { get; set; }


        [Required]
        public int TargetHierarchyId { get; set; }

        public Guid? NewManagerId { get; set; }

        public Guid? SwapWithUserId { get; set; }

        public bool BecomeCeo { get; set; } = false;
    }

    public class EmployeeFlatDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Position { get; set; }
        public string AvatarUrl { get; set; }
    }


    public class SynchroRequestDto
    {
        public int count { get; set; }
        public bool isHardSynchronize { get; set; }
        public List<UserToSynchronizeDto> users { get; set; } = new List<UserToSynchronizeDto>();
    }

    public class UserToSynchronizeDto
    {
        // Базовые данные пользователя
        public string Login { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SamAccountName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // AD идентификаторы
        public string AdGuid { get; set; } = string.Empty;
        public string AdObjectGuidBase64 { get; set; } = string.Empty;
        public string AdDistinguishedName { get; set; } = string.Empty;
        public string AdEmployeeId { get; set; } = string.Empty;
        public string AdWhenCreated { get; set; } = string.Empty;

        // Личная информация
        public PersonalInfoDto PersonalInfo { get; set; } = new PersonalInfoDto();

        // Рабочая информация
        public WorkInfoDto WorkInfo { get; set; } = new WorkInfoDto();

        // Контактная информация
        public ContactInfoDto ContactInfo { get; set; } = new ContactInfoDto();

        // Связи
        public string ManagerSamAccountName { get; set; } = string.Empty;
        public string ManagerAdGuid { get; set; } = string.Empty;

        // Метаданные синхронизации
        public DateTime LastAdSync { get; set; }
    }

    public class PersonalInfoDto
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Patronymic { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string Interests { get; set; } = string.Empty;
    }

    public class WorkInfoDto
    {
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime? WorkExp { get; set; }
    }

    public class ContactInfoDto
    {
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
