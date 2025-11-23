using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Hierarchy
    {
        public int HierarchyId { get; set; }
        public int? ParentId { get; set; }
        public int LevelHierarchy { get; set; }
        public required string TitleHierarchy { get; set; }
        public required string ColorHierarchy { get; set; }
    }
}
