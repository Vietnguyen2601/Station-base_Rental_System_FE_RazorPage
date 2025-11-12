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

public class RegisterModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IAuthService authService, ILogger<RegisterModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = new RegisterRequestDTO
        {
            Username = Input.Username,
            Email = Input.Email,
            ContactNumber = Input.ContactNumber,
            Password = Input.Password,
            ConfirmPassword = Input.ConfirmPassword
        };

        var result = await _authService.RegisterAsync(request);
        if (result is not { Data: TokenResponseDTO tokens })
        {
            var error = string.IsNullOrWhiteSpace(result?.Message) ? "Đăng ký thất bại" : result!.Message;
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            BuildPrincipal(tokens));

        TempData["Ok"] = "Đăng ký thành công";
        return RedirectToPage("/Homepage/Home");
    }

    private ClaimsPrincipal BuildPrincipal(TokenResponseDTO tokens)
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

    public class InputModel
    {
        [Required]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string? ContactNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
