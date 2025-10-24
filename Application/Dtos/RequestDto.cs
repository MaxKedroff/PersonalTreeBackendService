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
}
