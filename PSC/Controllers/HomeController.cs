using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Client.AspNetCore;
using System.Net.Http.Headers;
using PSC.Models;
using PSC.ViewModels;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Security.Principal;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;

namespace PSC.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _dbContext;

    public HomeController(IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
    }

    [HttpGet("~/")]
    public async Task<ActionResult> Index(string icno)
    {
        var token = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIddictClientAspNetCoreConstants.Tokens.BackchannelIdentityToken);
        var viewModel = new UserViewModel();

        var isTokenValid = TokenKontrol(token);
        if (isTokenValid)
        {
            ClaimsIdentity claimsIdentity = new ClaimsIdentity();
            if (HttpContext.User.Identity is { IsAuthenticated: true })
            {
                claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;

            }
            if (claimsIdentity != null)
            {
                foreach (var item in claimsIdentity.Claims)
                {
                    if (item.Type == "icnumber")
                    {
                        //Get info of registered user.
                        var user = _dbContext.Users.Where(x => x.IcNo == item.Value).FirstOrDefault();
                        if (user != null)
                        {
                            viewModel.IcNo = user.IcNo;
                        }
                        else if (user == null)
                        {
                            //Register user to PSC (can be handled seperately)
                            _dbContext.Users.Add(new User()
                            {
                                Name = "New User",
                                Email = "generic@generic.com",
                                IcNo = item.Value
                            });
                            _dbContext.SaveChanges();
                            viewModel.IcNo = item.Value;
                        }
                    }
                }
            }
        }

        return View(viewModel);
    }

    [Authorize, HttpPost("~/")]
    public async Task<ActionResult> Index(CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIddictClientAspNetCoreConstants.Tokens.BackchannelAccessToken);

        using var client = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7023/api/message");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return View(model: await response.Content.ReadAsStringAsync(cancellationToken));
    }

    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }

    //public IActionResult 
    public bool TokenKontrol(string authToken)
    {
        try
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            SecurityToken validatedToken;
            IPrincipal principal = tokenHandler.ValidateToken(authToken, validationParameters.Result, out validatedToken);

            if (principal.Identity != null && principal.Identity.IsAuthenticated)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            return false;

        }
    }

    private async Task<TokenValidationParameters> GetValidationParameters()
    {
        try
        {
            var issuer = "https://localhost:7023/";
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(issuer + ".well-known/openid-configuration",
 new OpenIdConnectConfigurationRetriever(),
 new HttpDocumentRetriever());

            var discoveryDocument = await configurationManager.GetConfigurationAsync();
            var signingKeys = discoveryDocument.SigningKeys;
            return new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidAudience = "psc",
                IssuerSigningKeys = signingKeys,
                ValidateTokenReplay = true,
            };
        }
        catch (Exception)
        {

            throw;
        }

    }
}
