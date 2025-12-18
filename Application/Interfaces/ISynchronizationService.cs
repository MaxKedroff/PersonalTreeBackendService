using Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISynchronizationService
    {
        Task<SynchroResponseDto> SyncData(SynchroRequestDto dto);
        Task<SynchroResponseDto> HardSyncData(SynchroRequestDto dto);
    }
}
