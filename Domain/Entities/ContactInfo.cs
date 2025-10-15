using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ContactInfo
    {
        [Phone]
        public string Phone { get; set; }
        public string City { get; set; }
        public string Avatar { get; set; }
        public string New_avatar { get; set; }
    }
}
