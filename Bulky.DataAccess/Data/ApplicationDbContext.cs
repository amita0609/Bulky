using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId=1,Name="Action",DisplayOrder=101},
                  new Category { CategoryId = 2, Name = "History", DisplayOrder = 102 }
                );

            modelBuilder.Entity<Product>().HasData(
                new Product {
                    Id = 1,
                    Title = "Rock in the ocean",
                    Author = "Ron Parker",
                    Description = "‘INDO-PAK WAR 1971- Reminiscences of Air Warriors’",
                    ISBN = "SOTJ111",
                    ListPrice = 30,
                    Price = 27,
                    Price50 = 25,
                    Price100 = 20,
                    CategoryId=1,
                    ImageUrl=""

                },
                new Product
                {
                    Id = 2,
                    Title = "The India Story’",
                    Author = "Bimal Jalal",
                    Description = "‘INDO-PAK WAR 1971- Reminiscences of Air Warriors’",
                    ISBN = "SOTJ111",
                    ListPrice = 30,
                    Price = 27,
                    Price50 = 25,
                    Price100 = 20,
                     CategoryId = 2,
                    ImageUrl = ""
                },
                new Product
                {
                    Id = 3,
                    Title = "A Place Called Home’",
                    Author = "Preeti Shenoy",
                    Description = "‘INDO-PAK WAR 1971- Reminiscences of Air Warriors’",
                    ISBN = "SOTJ111",
                    ListPrice = 30,
                    Price = 27,
                    Price50 = 25,
                    Price100 = 20,
                     CategoryId = 2,
                    ImageUrl = ""

                },
                 new Product
                 {
                     Id = 4,
                     Title = "‘Leaders, Politicians, Citizens",
                     Author = "Rasheed Kidwai ",
                     Description = "‘INDO-PAK WAR 1971- Reminiscences of Air Warriors’",
                     ISBN = "SOTJ111",
                     ListPrice = 30,
                     Price = 27,
                     Price50 = 25,
                     Price100 = 20,
                     CategoryId = 1,
                     ImageUrl = ""

                 }



                );
        }
    }
}
