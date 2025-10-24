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

        Task<ResponseUsersTreeDto> GetUsersAsync(bool isCached = false);

        Task<UserDetailInfoDto> GetUserDetailAsync(Guid userId);

        Task<SearchResponseDto> GetSearchResultAsync(SearchRequestDto request);
    }
}
