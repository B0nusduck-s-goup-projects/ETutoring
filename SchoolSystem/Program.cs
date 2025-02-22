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

// Register the custom claims principal factory 
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, CustomClaimsPrincipalFactory>();

var app = builder.Build();


// Ensure roles exist in the database on startup
await EnsureRolesExistAsync(app.Services);



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
