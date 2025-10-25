using Application.Dtos;
using Domain.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utils
{
    public static class Mapper
    {
        public static UserTreeDto MapUserToUserTreeDto(User user)
        {

            if (user == null)
                return null;

            return new UserTreeDto
            {
                UserId = user.User_id,
                UserName = user.GetFullName() ?? user.Login,
                Position = user.WorkInfo?.Position,
                Department = user.WorkInfo?.Department,
                AvatarUrl = user.ContactInfo?.Avatar,
                ManagerId = user.Manager_id,
                ManagerName = user.Manager?.GetFullName() ?? user.Manager?.Login,
                Subordinates = user.Subordinates?
                .Select(sub => new UserTreeItemDto
                {
                    UserId = sub.User_id,
                    UserName = sub.GetFullName() ?? sub.Login,
                    Position = sub.WorkInfo?.Position,
                    Department = sub.WorkInfo?.Department,
                    AvatarUrl = sub.ContactInfo?.Avatar
                })
                .ToList() ?? new List<UserTreeItemDto>()
            };
        }

        public static UserDetailInfoDto MapUserToUserDetailInfoDto(User user)
        {
            if (user == null)
                return null;

            return new UserDetailInfoDto
            {
                User_id = user.User_id,
                UserName = user.GetFullName() ?? user.Login,
                BornDate = user.PersonalInfo?.Birth_date ?? DateTime.MinValue,
                Department = user.WorkInfo?.Department,
                Position = user.WorkInfo?.Position,
                WorkExperience = user.WorkInfo?.Work_exp ?? DateTime.MinValue,
                PhoneNumber = user.ContactInfo?.Phone,
                City = user.ContactInfo?.City,
                Interests = user.PersonalInfo?.Interests,
                avatar = user.ContactInfo?.Avatar,
                Contacts = user.Contacts ?? new JObject()
            };
        }

        public static EmployeeHierarchyDto MapEmployeeToHierarchyDto(User user, List<User> allUsers, HashSet<Guid> processedUsers = null, int maxDepth = 10)
        {
            if (user == null) return null;

            processedUsers ??= new HashSet<Guid>();

            // Защита от циклических ссылок
            if (processedUsers.Contains(user.User_id) || maxDepth <= 0)
                return null;

            processedUsers.Add(user.User_id);

            var dto = new EmployeeHierarchyDto
            {
                UserId = user.User_id,
                UserName = user.GetFullName() ?? user.Login,
                Position = user.WorkInfo?.Position,
                AvatarUrl = user.ContactInfo?.Avatar
            };

            // Рекурсивно маппим подчиненных
            var subordinates = allUsers
                .Where(u => u.Manager_id == user.User_id &&
                           u.WorkInfo?.Department == user.WorkInfo?.Department)
                .ToList();

            foreach (var subordinate in subordinates)
            {
                var subordinateDto = MapEmployeeToHierarchyDto(subordinate, allUsers, processedUsers, maxDepth - 1);
                if (subordinateDto != null)
                {
                    dto.Subordinates.Add(subordinateDto);
                }
            }

            processedUsers.Remove(user.User_id);
            return dto;
        }
    }
}
