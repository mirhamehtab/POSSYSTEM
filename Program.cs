using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(
    opts => opts.UseSqlServer(builder.Configuration.GetConnectionString("default"))
    );

// 3 roles: Administrator, Manager, Cashier
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<POSSystem.Services.AuditLogger>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- Seed roles + first Administrator, sirf ek dafa app start hone par ---
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = { "Administrator", "Manager", "Cashier" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // demo admin account
    var adminEmail = "admin@synexus.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        await userManager.CreateAsync(admin, "Admin@123");
        await userManager.AddToRoleAsync(admin, "Administrator");
    }

    // FR-036: ek asal "Walk-in Customer" record hona chahiye, sirf null CustomerId nahi
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!await dbContext.Customers.AnyAsync(c => c.IsWalkIn))
    {
        dbContext.Customers.Add(new Customer
        {
            Name = "Walk-in Customer",
            IsWalkIn = true,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
    }
}

app.Run();