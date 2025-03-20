using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Models;



var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("default");

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(
    options => options.UseSqlServer(connectionString));

builder.Services.AddIdentity<AppUser, IdentityRole>(
    options =>
    {
        options.Password.RequiredUniqueChars = 0;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();


//Add Email Service
builder.Services.AddScoped(sp => new EmailSender.Services.EmailService(
    smtpHost: "smtp.gmail.com", 
    smtpPort: 587,              
    smtpUser: "buicaonguyen115@gmail.com", 
    smtpPass: "tzju egvo icfu rrnc"
));


// Register the custom claims principal factory 
//builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, CustomClaimsPrincipalFactory>();

var app = builder.Build();


// Ensure roles exist in the database on startup
await EnsureRolesExistAsync(app.Services);
// Ensure an Admin user exists in the database
await EnsureAdminUserExistsAsync(app.Services);




// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// Method to create roles if they don't exist
async Task EnsureRolesExistAsync(IServiceProvider serviceProvider)
{
	using var scope = serviceProvider.CreateScope();
	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
	string[] roles = { "Student", "Tutor", "Staff", "Admin" };

	foreach (var role in roles)
	{
		if (!await roleManager.RoleExistsAsync(role))
		{
			await roleManager.CreateAsync(new IdentityRole(role));
		}
	}
}
// Method to create admin if users don't exist
async Task EnsureAdminUserExistsAsync(IServiceProvider serviceProvider)
{
	using var scope = serviceProvider.CreateScope();
	var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

	// Check if any user exists
	if (!userManager.Users.Any())
	{
		var Email = "thinh@gmail.com";
		var Password = "thinh123"; // Change for security
		var User = new AppUser
		{
			Name = "Thinh",
			Code = "GCS210895",
			UserName = Email,
			Email = Email,
			Address = "Admin Office",
			Gender = "Male"
		};

		// Create Admin user
		var result = await userManager.CreateAsync(User, Password);
		if (result.Succeeded)
		{
			// Ensure Admin role exists
			if (await roleManager.RoleExistsAsync("Admin"))
			{
				await userManager.AddToRoleAsync(User, "Admin");
			}
		}
	}
}

