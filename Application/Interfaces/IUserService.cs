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

        [Obsolete]
        Task<HierarchyResponseDto> GetDepartmentHierarchyAsync();

        Task<HierarchyNodeDto> GetDepartmentHierarchyAsyncV2();

        Task<UserDetailInfoDto> UpdateUserProfileAsync(Guid userId, Guid currentUserId, string currentUserRole, UpdateProfileDto updateDto);

        Task<UserDetailInfoDto> MoveUserToHierarchyAsync(MoveUserRequestDto moveRequest, Guid currentUserId, string currentUserRole);

        Task<HierarchyNodeWithoutPersonsDto> GetDepartmentTreeAsync();

        Task<DepartmentDetailsDto> GetDetailsFromDepartment(string hierarchyId);
    }
}
