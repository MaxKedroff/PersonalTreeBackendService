using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class SearchRequestDto
    {
        public string searchCriteria { get; set; }

        public string searchValue { get; set; }

        public int queryAmount { get; set; }

        public bool is_cached { get; set; } = false;
    }

    public class TableRequestDto
    {
        public int page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string? Sort { get; set; }

        // Раздельные фильтры
        public string? PositionFilter { get; set; }
        public string? DepartmentFilter { get; set; }

        public bool isCached { get; set; } = false;
    }
}
