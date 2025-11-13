using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EVStationRental.Common.DTOs.Authentication;
using EVStationRental.Services.InternalServices.IServices.IAuthServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVStationRental.Presentation.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IAuthService authService, ILogger<LoginModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; private set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = new LoginRequestDTO
        {
            Username = Input.Username,
            Password = Input.Password
        };

        var result = await _authService.LoginAsync(request);
        if (result is not { Data: TokenResponseDTO tokens })
        {
            var error = string.IsNullOrWhiteSpace(result?.Message) ? "Đăng nhập thất bại" : result!.Message;
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        var principal = BuildPrincipal(tokens);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (principal.IsInRole("Admin"))
        {
            return RedirectToPage("/Admin/Accounts/Index");
        }

        if (principal.IsInRole("Staff"))
        {
            return RedirectToPage("/Staff/Dashboard/Index");
        }

        return RedirectToPage("/Homepage/Home");
    }

    private ClaimsPrincipal BuildPrincipal(TokenResponseDTO tokens)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokens.AccessToken);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, jwt.Subject ?? Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value ?? Input.Username)
            };

            foreach (var roleClaim in jwt.Claims.Where(c => c.Type == ClaimTypes.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot parse JWT token for user {Username}", Input.Username);
            throw;
        }
    }

    public class InputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
