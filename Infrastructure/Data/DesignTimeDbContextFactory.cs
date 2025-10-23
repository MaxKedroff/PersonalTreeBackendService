using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserDb>
    {
        public UserDb CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDb>();

            var connectionString = "Host=localhost;Port=5432;Database=persontree;Username=postgres;Password=postgres";

            optionsBuilder.UseNpgsql(connectionString);

            return new UserDb(optionsBuilder.Options);
        }
    }
}
