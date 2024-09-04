using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Client;
using Quartz;
using static OpenIddict.Abstractions.OpenIddictConstants;

using PSC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
// var httpsConnectionAdapterOptions = new HttpsConnectionAdapterOptions
// {
//     SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
//     ClientCertificateMode = ClientCertificateMode.AllowCertificate,
//     ServerCertificate = new X509Certificate2("./tls.pfx", "1234")
//
// };
// builder.WebHost.ConfigureKestrel(options =>
// options.ConfigureEndpointDefaults(listenOptions =>
// listenOptions.UseHttps(httpsConnectionAdapterOptions)));
IdentityModelEventSource.ShowPII = true;
// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    //options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseSqlite($"Filename={Path.Combine(Path.GetTempPath(), "psc-client.sqlite3")}");

    options.UseOpenIddict();

});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
        options.SlidingExpiration = false;
    });
builder.Services.AddQuartz(options =>
{
    options.UseMicrosoftDependencyInjectionJobFactory();
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddOpenIddict()

    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        // Configure OpenIddict to use the Entity Framework Core stores and models.
        // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();

        // Developers who prefer using MongoDB can remove the previous lines
        // and configure OpenIddict to use the specified MongoDB database:
        // options.UseMongoDb()
        //        .UseDatabase(new MongoClient().GetDatabase("openiddict"));

        // Enable Quartz.NET integration.
        options.UseQuartz();
    })

    // Register the OpenIddict client components.
    .AddClient(options =>
    {
        // Note: this sample uses the code flow, but you can enable the other flows if necessary.
        options.AllowAuthorizationCodeFlow();

        // Register the signing and encryption credentials used to protect
        // sensitive data like the state tokens produced by OpenIddict.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();


        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        options.UseAspNetCore()
               .EnableStatusCodePagesIntegration()
               .EnableRedirectionEndpointPassthrough()
               .EnablePostLogoutRedirectionEndpointPassthrough();

        // Register the System.Net.Http integration and use the identity of the current
        // assembly as a more specific user agent, which can be useful when dealing with
        // providers that use the user agent as a way to throttle requests (e.g Reddit).
        options.UseSystemNetHttp()
               .SetProductInformation(typeof(Program).Assembly);

        // Add a client registration matching the client application definition in the server project.
        options.AddRegistration(new OpenIddictClientRegistration
        {
            Issuer = new Uri("https://localhost:7023", UriKind.Absolute),

            ClientId = "psc",
            ClientSecret = "psc@secret",
            Scopes = { Scopes.OpenId },

            // Note: to mitigate mix-up attacks, it's recommended to use a unique redirection endpoint
            // URI per provider, unless all the registered providers support returning a special "iss"
            // parameter containing their URL as part of authorization responses. For more information,
            // see https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics#section-4.4.
            RedirectUri = new Uri("callback/login/local", UriKind.Relative),
            PostLogoutRedirectUri = new Uri("callback/logout/local", UriKind.Relative)
            //,
            //SigningCredentials =
            //{
            //    new SigningCredentials(GetECDsaSecurityKey("-----BEGIN ENCRYPTED PRIVATE KEY-----\r\nMIH0MF8GCSqGSIb3DQEFDTBSMDEGCSqGSIb3DQEFDDAkBBBALaLtHqwEkZxJCOF6\r\n2hMkAgIIADAMBggqhkiG9w0CCQUAMB0GCWCGSAFlAwQBKgQQ7mUpQ/c8xyrSGh2z\r\nCKmO6wSBkDJNWY0g/E3oUSWR+2RR7RB+Mvzc+1qYO22xhV8lLdaAub/k7hdVbKSx\r\nMy5ZFgMlbdpO7m95YaciC0hxs1bAzOLfQ5iK4Cd8ToWUBKZ97USIeowNYpNneSIZ\r\nLVmog/m/v8z+tYY4Fi9Sr9EPWAb+/t2j17a5HK0GjxHC8xo6HioCnouOLKhjrUra\r\n4LQlsSzbcw==\r\n-----END ENCRYPTED PRIVATE KEY-----"), SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.Sha256)
            //}

            
        });
    })
    .AddValidation(options =>
    {
        options.SetIssuer("https://localhost:7023/");
        options.UseSystemNetHttp();
    });
static ECDsaSecurityKey GetECDsaSecurityKey(ReadOnlySpan<char> key)
{
    var algorithm = ECDsa.Create();
    algorithm.ImportFromEncryptedPem(key, "1234");

    return new ECDsaSecurityKey(algorithm);
}
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();

builder.Services.AddHostedService<PSC.Worker>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseDeveloperExceptionPage();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(options =>
{
	options.MapControllers();
	options.MapDefaultControllerRoute();
});
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
