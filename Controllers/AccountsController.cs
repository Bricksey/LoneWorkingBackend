using LoneWorkingBackend.Models;
using LoneWorkingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace LoneWorkingBackend.Controllers
{
    [ApiController]
    [Route("api/")]
    public class AccountsController : ControllerBase
    {
        
        private readonly AccountsService _accountsService;

        public AccountsController(AccountsService accountsService)
        {
            _accountsService = accountsService;
        }

        [HttpPost("register")] // .../api/register
        public async Task<IActionResult> Register(Account newAccount)
        {
            
            // Generate salt for security, store in db
            byte[]salt = new byte[128/8];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetNonZeroBytes(salt);
            }
            newAccount.Salt = Convert.ToBase64String(salt);

            // Hash and salt the user's password, store hashed password in db
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: newAccount.Password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
            newAccount.Password = hashed;
            Random rnd = new Random();
            newAccount.AuthCode = Convert.ToString((rnd.Next(0000000, 9999999))).PadLeft(7, '0');

            // Accounts are not admin by default
            newAccount.Admin = false;
            newAccount.Verified = false;

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow,
                RedirectUri = null
            };


            // Commit changes to db, return 201
            // Null is placeholder, update when GET endpoint added for accounts
            await _accountsService.CreateAsync(newAccount);

            var claims = new List<Claim>
            {
                new Claim (ClaimTypes.Sid, newAccount.Id),
                new Claim (ClaimTypes.Role, "User")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            
            Console.WriteLine(claimsIdentity.Claims.Where(c => c.Type == ClaimTypes.Sid).Select(c => c.Value).SingleOrDefault());

            return CreatedAtAction(null, new {id = newAccount.Id}, newAccount);
        }

        [Authorize]
        [HttpPost("auth")] // .../api/auth
        public async Task<ActionResult<int>> Auth([FromQuery]string authCode)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.Claims;
            var Sid = claims.Where(c => c.Type == ClaimTypes.Sid).Select(c => c.Value).SingleOrDefault();
            Account currentAccount = await _accountsService.GetAsync(Sid);
            if (currentAccount.AuthCode == authCode)
            {
                currentAccount.Verified = true;
                return StatusCode(200);
            }
            else
            {
               return StatusCode(401); 
            }
            
        }

    }
}