using AppAlumnos.Data;
using AppAlumnos.Logging;
using AppAlumnos.Models;
using AppAlumnos.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Holgura sobre el tamaño máximo de avatar para el resto del cuerpo multipart (formularios, campos).
const long MargenCabeceraMultipartBytes = 512 * 1024;

var rutaLogDiagnostico = Path.Combine(builder.Environment.ContentRootPath, "logs", $"app-{DateTime.Now:yyyyMMdd}.log");

if (builder.Environment.IsDevelopment())
{
    ConfigurarDiagnosticoDeFallo(builder, rutaLogDiagnostico);
}
var connectionString = builder.Configuration.GetConnectionString("AppAlumnosContextConnection") ?? throw new InvalidOperationException("Connection string 'AppAlumnosContextConnection' not found.");

QuestPDF.Settings.License = LicenseType.Community;

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

// Red de contención para las subidas: el archivo se valida en ArchivoService con un
// mensaje friendly, y este límite evita que Kestrel lea en memoria un cuerpo multipart
// desproporcionado antes de llegar al controlador.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ArchivoService.TamanoMaximoBytes + MargenCabeceraMultipartBytes;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, AppAlumnos.Services.EmailSender>();
builder.Services.AddTransient<ArchivoService>();
builder.Services.AddTransient<CertificadoService>();
builder.Services.AddScoped<MateriaService>();
builder.Services.AddScoped<CursadaService>();
builder.Services.AddScoped<AutogestionService>();
builder.Services.AddScoped<UsuarioService>();

// Sin un endpoint HTTPS (por ejemplo, el perfil "http" de launchSettings.json) Kestrel no
// tiene puerto al que redirigir y el middleware registra un aviso en cada petición. El
// perfil "https" sí define uno, así que la redirección solo se activa cuando existe.
var puertoHttps = ExtraerPuertoHttps(builder.Configuration["urls"] ?? builder.Configuration["ASPNETCORE_URLS"]);
if (puertoHttps.HasValue)
{
    builder.Services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = puertoHttps.Value);
}

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
        logger.LogError(ex, "Ocurrió un error al poblar los datos o inicializar el administrador");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Sin un endpoint HTTPS (por ejemplo, el perfil "http" de launchSettings.json) Kestrel no
// tiene puerto al que redirigir y el middleware registra un aviso en cada petición. El
// perfil "https" sí define uno, así que la redirección solo se activa cuando existe.
if (puertoHttps.HasValue)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Un fallo en un hilo secundario nunca llega al middleware y termina el proceso
// sin dejar rastro en el log de la petición, así que se registra de forma explícita.
if (app.Environment.IsDevelopment())
{
    RegistrarFallosNoGestionados(app.Services.GetRequiredService<ILoggerFactory>());
}

// Con los puertos fijos de launchSettings.json, un F5 con una instancia anterior todavía
// viva hace que Kestrel no pueda enlazar y la excepción sale sin manejar desde app.Run(),
// lo que terminaba el proceso con un crash del runtime. Se captura para dejar un mensaje
// accionable en el log y salir de forma controlada.
//
// El mensaje de puerto ocupado se escribe con un logger independiente del host: cuando el
// enlace de Kestrel falla, el contenedor ya liberó sus proveedores y usar el ILogger del
// host lanzaría ObjectDisposedException, ocultando el mensaje real detrás de otro crash.
try
{
    app.Run();
}
catch (IOException ex) when (ex.InnerException is AddressInUseException or SocketException)
{
    using var fabricaLoggerArranque = LoggerFactory.Create(configuracion =>
    {
        configuracion.SetMinimumLevel(LogLevel.Information);
        configuracion.AddProvider(new ArchivoLogProvider(rutaLogDiagnostico, LogLevel.Information));
    });

    fabricaLoggerArranque.CreateLogger("Program").LogCritical(
        "No se pudo iniciar el servidor porque el puerto ya está en uso. "
        + "Es probable que haya otra instancia de AppAlumnos en ejecución: "
        + "finalizá la depuración anterior en Visual Studio (o cerrá la otra ventana de consola) antes de volver a presionar F5. "
        + "Detalle: {Detalle}",
        ex.Message);
}

void ConfigurarDiagnosticoDeFallo(WebApplicationBuilder builder, string rutaLog)
{
    builder.Logging.AddProvider(new ArchivoLogProvider(rutaLog, LogLevel.Information));
}

// Devuelve el puerto de la primera entrada https:// presente en la lista de direcciones
// configurada para el servidor, o null cuando el arranque no define ningún endpoint HTTPS.
int? ExtraerPuertoHttps(string? urls)
{
    if (string.IsNullOrWhiteSpace(urls))
    {
        return null;
    }

    foreach (var direccion in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (!direccion.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!Uri.TryCreate(direccion, UriKind.Absolute, out var uri))
        {
            continue;
        }

        if (uri.IsDefaultPort)
        {
            return 443;
        }

        return uri.Port;
    }

    return null;
}

// Un fallo en un hilo secundario nunca llega al middleware y termina el proceso
// sin dejar rastro en el log de la petición, así que se registra de forma explícita.
void RegistrarFallosNoGestionados(ILoggerFactory loggerFactory)
{
    AppDomain.CurrentDomain.UnhandledException += (_, args) =>
    {
        var logger = loggerFactory.CreateLogger("Diagnostico.Fallos");
        logger.LogCritical(args.ExceptionObject as Exception, "Fallo no gestionado que termina el proceso. Tipo: {Tipo}", args.IsTerminating);
    };

    TaskScheduler.UnobservedTaskException += (_, args) =>
    {
        var logger = loggerFactory.CreateLogger("Diagnostico.Fallos");
        logger.LogError(args.Exception, "Excepción de tarea no observada.");
        args.SetObserved();
    };
}
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