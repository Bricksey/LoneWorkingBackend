using LoneWorkingBackend.Models;
using LoneWorkingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using System.Text.Json;

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

            string json = System.IO.File.ReadAllText("./conf.json");
            Dictionary<string, Dictionary<string, string>> configFile = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            Dictionary<string, string> emailConf = configFile["Email"];
            

            string email = emailConf["senderEmail"];
            string senderName = emailConf["senderName"];
            string smtpPassword = emailConf["smtpPassword"];
            string smtpServer = emailConf["smtpServer"];
            string smtpPort = emailConf["smtpPort"];
            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress(senderName, email));
            mailMessage.To.Add(new MailboxAddress(newAccount.Name, newAccount.Email));
            mailMessage.Subject = "Verify Your Lone Working Register Account.";
            mailMessage.Body = new TextPart("plain")
            {
                Text = $"Your verification code is {newAccount.AuthCode}."
            };
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Connect(smtpServer, Convert.ToInt16(smtpPort), true);
                smtpClient.Authenticate(email, smtpPassword);
                smtpClient.Send(mailMessage);
                smtpClient.Disconnect(true);

            }



            return CreatedAtAction(null, new {id = newAccount.Id}, newAccount);
        }

        [HttpPost("login")] // .../api/login
        public async Task<ActionResult<int>> Login([FromBody] string Email, [FromBody] string Password)
        {
            var currentAccount = await _accountsService.GetAsyncEmail(Email);
            if (currentAccount != null)
            {
                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: Password,
                    salt: Encoding.ASCII.GetBytes(currentAccount.Salt),
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8));

                    if(hashed == currentAccount.Password)
                    {
                        string role;
                        if (currentAccount.Admin == true)
                        {
                            role = "Admin";
                        }
                        else
                        {
                            role = "User";
                        }
                        var claims = new List<Claim>
                        {
                            new Claim (ClaimTypes.Sid, currentAccount.Id),
                            new Claim (ClaimTypes.Role, role)
                        };

                        var claimsIdentity = new ClaimsIdentity(
                            claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        
                        var authProperties = new AuthenticationProperties
                        {
                            AllowRefresh = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                            IsPersistent = true,
                            IssuedUtc = DateTimeOffset.UtcNow,
                            RedirectUri = null
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);
                        return StatusCode(200);
                    }
                    else
                    {
                        return StatusCode(401);
                    }
            }
            else
            {
                return StatusCode(401);
            }
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