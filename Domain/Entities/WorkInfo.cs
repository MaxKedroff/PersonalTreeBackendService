using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class WorkInfo
    {
        [Required]
        public string Position { get; set; }

        [Required]
        public string Department { get; set; }

        [Required]
        public DateTime Work_exp { get; set; }
    }
}
