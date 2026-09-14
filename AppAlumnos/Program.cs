using AppAlumnos.Data;
using AppAlumnos.Models;
using AppAlumnos.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("AppAlumnosContextConnection") ?? throw new InvalidOperationException("Connection string 'AppAlumnosContextConnection' not found.");

builder.Services.AddDbContext<AppAlumnosContext>(options => options.UseSqlServer(connectionString));

//builder.Services.AddDefaultIdentity<Usuario>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<AppAlumnosContext>();
builder.Services.AddIdentity<Usuario, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
}).AddEntityFrameworkStores<AppAlumnosContext>().AddDefaultTokenProviders();



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, AppAlumnos.Services.EmailSender>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) 
{
    var services = scope.ServiceProvider;
    try
    {
        await InicializarRolesYAdmin(services);
    }
    catch(Exception ex) 
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurri� un error al poblar los datos o inicializar el administrador");
    }
}

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

// M�todo de Seeding en Program.cs
async Task InicializarRolesYAdmin(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();

    string[] nombresDeRoles = { "Administrador", "Docente", "Alumno" };

    foreach (var nombreRol in nombresDeRoles)
    {
        if (!await roleManager.RoleExistsAsync(nombreRol))
        {
            await roleManager.CreateAsync(new IdentityRole(nombreRol));
        }
    }

    // Usuario Administrador inicial
    var adminEmail = "admin@instituto.edu.ar";
    var adminExistente = await userManager.FindByEmailAsync(adminEmail);
    if (adminExistente == null)
    {
        var nuevoAdmin = new Usuario
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nombre = "Administrador",
            Apellido = "Sistema",
            Dni = 11111111,
            Legajo = "ADM-001",
            Rol = "Administrador",
            EmailConfirmed = true
        };
        var resultado = await userManager.CreateAsync(nuevoAdmin, "Admin123!");
        if (resultado.Succeeded)
        {
            await userManager.AddToRoleAsync(nuevoAdmin, "Administrador");
        }
    }
    else if (string.IsNullOrEmpty(adminExistente.Rol))
    {
        adminExistente.Rol = "Administrador";
        await userManager.UpdateAsync(adminExistente);
    }
}