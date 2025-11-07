using Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UnitTests
{
    public static class UserServiceTestExtensions
    {
        public static (string Field, string Order) TestParseSortParameter(this UserService userService, string sort)
        {
            var method = typeof(UserService).GetMethod("ParseSortParameter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return ((string Field, string Order))method.Invoke(userService, new object[] { sort });
        }
    }
}
