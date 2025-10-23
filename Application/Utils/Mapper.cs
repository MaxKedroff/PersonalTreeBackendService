using Application.Dtos;
using Domain.Entities;
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
    }
}
