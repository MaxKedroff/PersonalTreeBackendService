using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SkillService : ISkillService
    {
        public IUserRepository _userRepository;

        public SkillService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task AddSkillAsync(RemoveAddSkillDto dto)
        {
            await _userRepository.AddSkillToUser(dto.userId, dto.skill);
        }

        public async Task<SkillListDto> GetSkillListAsync(Guid userId)
        {
            var skillList = await _userRepository.GetSkillsByUser(userId);
            return new SkillListDto
            {
                skills = skillList,
                count = skillList.Count()
            };
        }

        public async Task RemoveSkillAsync(RemoveAddSkillDto dto)
        {
            await _userRepository.DeleteSkillFromUser(dto.userId, dto.skill);
        }
    }
}
