using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PersonalInfo
    {
        [Required]
        public string Last_name { get; set; }

        [Required]
        public string First_name { get; set; }

        public string Patronymic { get; set; }

        [Required]
        public DateTime Birth_date { get; set; }

        public string Interests { get; set; }
    }
}
