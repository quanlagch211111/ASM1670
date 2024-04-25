using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using ASM1670.Models;

namespace ASM1670.Areas.Identity.Pages.Account.Manage
{
    public class EmailOrPhoneNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var userManager = (UserManager<ApplicationUser>)validationContext.GetService(typeof(UserManager<ApplicationUser>));
            var email = value.ToString();
            var userByEmail = userManager.FindByEmailAsync(email).Result;

            // If there is a user with this email, return success
            if (userByEmail != null)
            {
                return ValidationResult.Success;
            }

            // If not, check if the value is a valid phone number
            if (IsValidPhoneNumber(email))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult("Invalid email or phone number.");
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // Check if the phone number matches a valid format
            // For simplicity, I'm using a basic regex pattern here. You may need to adjust it based on your requirements.
            var regexPattern = @"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$";
            return Regex.IsMatch(phoneNumber, regexPattern);
        }

    }
}
