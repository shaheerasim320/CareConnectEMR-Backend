using CareConnectEMR.Domain.Enitites;
using Microsoft.AspNetCore.Identity;

namespace CareConnectEMR.Infrastructure.Persistence.Seed
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Doctor", "Receptionist" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var usersToSeed = new List<(ApplicationUser User, string Password, string Role)>
            {
                (new ApplicationUser { UserName = "admin", Email = "admin@careconnect.com", FirstName = "Evelyn", LastName = "Reed" }, "Admin@123", "Admin"),

                (new ApplicationUser { UserName = "sarah", Email = "sarah@careconnect.com", FirstName = "Sarah", LastName = "Khan" }, "Doctor@123", "Doctor"),
                (new ApplicationUser { UserName = "ali", Email = "ali@careconnect.com", FirstName = "Ali", LastName = "Ahmed" }, "Doctor@123", "Doctor"),
                (new ApplicationUser { UserName = "maryam", Email = "maryam@careconnect.com", FirstName = "Maryam", LastName = "Noor" }, "Doctor@123", "Doctor"),

                (new ApplicationUser { UserName = "reception1", Email = "reception1@careconnect.com", FirstName = "Reception", LastName = "One" }, "Staff@123", "Receptionist"),
                (new ApplicationUser { UserName = "reception2", Email = "reception2@careconnect.com", FirstName = "Reception", LastName = "Two" }, "Staff@123", "Receptionist")
            };

            foreach (var seedData in usersToSeed)
            {
                var existingUser = await userManager.FindByEmailAsync(seedData.User.Email!);

                if (existingUser == null)
                {
                    var result = await userManager.CreateAsync(seedData.User, seedData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(seedData.User, seedData.Role);
                    }
                }
            }
        }
    }
}