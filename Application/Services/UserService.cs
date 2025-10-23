using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserService : IUserService
    {

        public IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ResponseUsersTreeDto> GetUsersAsync(bool isCached = false)
        {
            var users = await _userRepository.GetUsersAsync();
            var response = new ResponseUsersTreeDto
            {
                AmountOfUsers = users.Count(),
                UsersTree = [.. users.Select(usr => Mapper.MapUserToUserTreeDto(usr))],
                IsCached = isCached
            };
            return response;
        }
    }
}
