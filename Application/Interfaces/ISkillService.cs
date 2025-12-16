using Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISkillService
    {
        Task<SkillListDto> GetSkillListAsync(Guid userId);
        Task RemoveSkillAsync(RemoveAddSkillDto dto);
        Task AddSkillAsync(RemoveAddSkillDto dto);

        
    }
}
