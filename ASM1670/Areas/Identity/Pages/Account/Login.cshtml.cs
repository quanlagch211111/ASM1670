using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ASM1670.Areas.Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using ASM1670.Data;
using ASM1670.Models;
using ASM1670.Hubs;

namespace ASM1670.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ASM1670Context _context; 
        private readonly TwilioService _twilioService;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager, ASM1670Context context, TwilioService twilioService) 
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _context = context; 
            _twilioService = twilioService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailOrPhoneNumber]
            public string EmailOrPhoneNumber { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                Input.EmailOrPhoneNumber = Input.EmailOrPhoneNumber.TrimStart('0');

                ApplicationUser user = null;

                if (Input.EmailOrPhoneNumber.Contains("@"))
                {
                    user = await _userManager.FindByEmailAsync(Input.EmailOrPhoneNumber);
                }
                else
                {
                    user = await FindByPhoneNumberAsync(Input.EmailOrPhoneNumber);
                }

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");  
                        return LocalRedirect(returnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Phone");

                        await _twilioService.SendSmsAsync(user.PhoneNumber, $"Your OTP code is: {code}");

                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                }

                // Nếu không tìm thấy người dùng hoặc xác thực thất bại, thêm thông báo lỗi
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            // Nếu có lỗi xảy ra hoặc xác thực thất bại, hiển thị lại form đăng nhập
            return Page();
        }

        private async Task<ApplicationUser> FindByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }
    }
}
