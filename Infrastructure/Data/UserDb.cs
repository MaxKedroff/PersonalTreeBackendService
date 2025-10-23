using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class UserDb : DbContext
    {
        public DbSet<User> Users { get; set; }

        public UserDb(DbContextOptions<UserDb> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.User_id);

                entity.HasOne(u => u.Manager)
                 .WithMany(u => u.Subordinates)
                 .HasForeignKey(u => u.Manager_id)  
                 .IsRequired(false);

                entity.OwnsOne(e => e.PersonalInfo, personal =>
                {
                    personal.Property(p => p.Last_name)
                    .HasColumnName("last_name")
                    .IsRequired();

                    personal.Property(p => p.First_name)
                    .HasColumnName("first_name")
                    .IsRequired();

                    personal.Property(p => p.Patronymic)
                        .HasColumnName("patronymic");

                    personal.Property(p => p.Birth_date)
                        .HasColumnName("birth_date")
                        .IsRequired();

                    personal.Property(p => p.Interests)
                        .HasColumnName("interests");
                });

                entity.OwnsOne(e => e.WorkInfo, work =>
                {
                    work.Property(w => w.Position)
                        .HasColumnName("position")
                        .IsRequired();

                    work.Property(w => w.Department)
                        .HasColumnName("department")
                        .IsRequired();

                    work.Property(w => w.Work_exp)
                        .HasColumnName("work_exp")
                        .IsRequired();
                });

                entity.OwnsOne(e => e.ContactInfo, contact =>
                {
                    contact.Property(c => c.Phone)
                        .HasColumnName("phone");

                    contact.Property(c => c.City)
                        .HasColumnName("city");

                    contact.Property(c => c.Avatar)
                        .HasColumnName("avatar");

                    contact.Property(c => c.New_avatar)
                        .HasColumnName("new_avatar");
                });

                entity.Property(e => e.Contacts)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => v.ToString(Formatting.None),
                        v => JObject.Parse(v)
                    );
            });
            base.OnModelCreating(modelBuilder);
        }
    }
}
