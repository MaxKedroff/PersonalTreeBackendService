using Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUserService
    {

        Task<ResponseTableUsersDto> GetUserTableAsync(TableRequestDto request);

        Task<UserDetailInfoDto> GetUserDetailAsync(Guid userId);

        [Obsolete("Use GetUserTableAsync with search functionality instead")]
        Task<SearchResponseDto> GetSearchResultAsync(SearchRequestDto request);

        Task<HierarchyResponseDto> GetDepartmentHierarchyAsync();

        Task<UserDetailInfoDto> UpdateUserProfileAsync(Guid userId, Guid currentUserId, string currentUserRole, UpdateProfileDto updateDto);
    }
}
