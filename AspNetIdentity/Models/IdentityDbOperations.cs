﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AspNetIdentity.Services;
using Microsoft.AspNet.Identity;
using Microsoft.Data.Entity.SqlServer;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace AspNetIdentity.Models
{
    public class IdentityDbOperations
    {
        private static Logger logger = Logger.GetLogger(typeof(IdentityDbOperations).Name);

        // The default administrator role
        const string adminRole = "admin";

        public static async Task InitializeIdentityDbAsync(IServiceProvider serviceProvider)
        {
            using (var db = serviceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var sqlDb = db.Database as SqlServerDatabase;
                if (sqlDb != null)
                {
                    // Create the database if it does not already exist
                    logger.Trace("InitializeIdentityDbAsync: Ensuring Database exists");
                    await sqlDb.EnsureCreatedAsync();
                    // Create the first user if it does not already exist
                    await CreateAdminUser(serviceProvider);
                }
            }
        }

        private static async Task CreateAdminUser(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<IdentityDbContextOptions>>().Options;
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // If the admin role does not exist, create it.
            logger.Trace("CreateAdminUser: Ensuring Role admin exists");
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                logger.Trace("CreateAdminUser: Role admin does not exist - creating");
                var roleCreationResult = await roleManager.CreateAsync(new IdentityRole(adminRole));
                DumpIdentityResult("CreateAdminUser: Role Creation", roleCreationResult);
            }
            else
            {
                logger.Trace("CreateAdminUser: Role admin exists");
            }

            // if the user does not exist, create it.
            logger.Trace(String.Format("CreateAdminUser: Ensuring User {0} exists", options.DefaultAdminUserName));
            var user = await userManager.FindByNameAsync(options.DefaultAdminUserName);
            if (user == null)
            {
                logger.Trace("CreateAdminUser: User does not exist - creating");
                user = new ApplicationUser { UserName = options.DefaultAdminUserName, DisplayName = "Administrator" };
                var userCreationResult = await userManager.CreateAsync(user, options.DefaultAdminPassword);
                DumpIdentityResult("CreateAdminUser: User Creation", userCreationResult);
                if (userCreationResult.Succeeded)
                {
                    logger.Trace("CreateAdminUser: Adding new user to role admin");
                    var roleAdditionResult = await userManager.AddToRoleAsync(user, adminRole);
                    DumpIdentityResult("CreateAdminUser: Role Addition", roleAdditionResult);
                }
            }
            else
            {
                logger.Trace("CreateAdminUser: User already exists");
            }
        }

        private static void DumpIdentityResult(string prefix, IdentityResult result)
        {
            logger.Trace(String.Format("{0}: Result = {1}", prefix, result.Succeeded ? "Success" : "Failed"));
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    logger.Trace(String.Format("--> {0}: {1}", error.Code, error.Description));
                }
            }
        }
    }
}