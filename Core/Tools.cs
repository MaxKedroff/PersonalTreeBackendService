using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class Tools
    {
        public static List<string> ParseMultiFilter(string filterValue)
        {
            if (string.IsNullOrEmpty(filterValue))
                return new List<string>();

            return filterValue
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }
    }
}
