using Application.Dtos;
using Application.Interfaces;
using Application.Utils;
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

        public async Task<SearchResponseDto> GetSearchResultAsync(SearchRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.searchValue))
                return new SearchResponseDto
                {
                    amount = 0,
                    searchItems = new List<SearchItemDto>(),
                    is_cached = false
                };

            var queryAmount = request.queryAmount > 0 ? request.queryAmount : 10;

            var users = await _userRepository.GetSearchResultAsync(request.searchCriteria, request.searchValue, request.queryAmount);
            var searchItems = users.Select(user => new SearchItemDto
            {
                username = user.GetFullName() ?? user.Login,
                department = user.WorkInfo?.Department ?? string.Empty,
                position = user.WorkInfo?.Position ?? string.Empty
            }).ToList();

            return new SearchResponseDto
            {
                amount = searchItems.Count,
                searchItems = searchItems,
                is_cached = false
            };
        }

        public async Task<UserDetailInfoDto> GetUserDetailAsync(Guid userId)
        {
            var user = await _userRepository.GetUsersByIdAsync(userId);
            return Mapper.MapUserToUserDetailInfoDto(user);
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
