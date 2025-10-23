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
                Manager = user.Manager != null ? MapUserToUserTreeDto(user.Manager) : null,
                Subordinates = user.Subordinates?.Select(sub => MapUserToUserTreeDto(sub))
                .ToList() ?? new List<UserTreeDto>(),

                UserName = user.GetFullName() ?? user.Login,
                Position = user.WorkInfo?.Position,
                Department = user.WorkInfo?.Department,
                AvatarUrl = user.ContactInfo?.Avatar
            };
        }
    }
}
