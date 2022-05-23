using LoneWorkingBackend.Models;
using LoneWorkingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
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
            salt = Encoding.ASCII.GetBytes(newAccount.Salt);

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
            for(int i = 0; i < 4; i++)
            {
                for(int j = 0; j < 7; j++)
                {
                    newAccount.signInHeatmap[i] = new int[] {0, 0, 0, 0, 0, 0, 0};
                }
            }
            newAccount.heatmapLastUpdate = ISOWeek.GetWeekOfYear(DateTime.Now);
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
            /*
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Connect(smtpServer, Convert.ToInt16(smtpPort), true);
                smtpClient.Authenticate(email, smtpPassword);
                smtpClient.Send(mailMessage);
                smtpClient.Disconnect(true);

            }
            */


            return CreatedAtAction(null, new {id = newAccount.Id}, newAccount);
        }


        [HttpPost("login")] // .../api/login
        public async Task<ActionResult<Account>> Login(Account loginAccount)
        {
            var currentAccount = await _accountsService.GetAsyncEmail(loginAccount.Email);
            if (currentAccount != null)
            {
                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: loginAccount.Password,
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
                        return await stripData(currentAccount);
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
        [HttpGet("check-session")]
        public async Task<ActionResult<Account>> CheckSession()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.Claims;
            var Sid = claims.Where(c => c.Type == ClaimTypes.Sid).Select(c => c.Value).SingleOrDefault();
            Account currentAccount = await _accountsService.GetAsync(Sid);
            Account returnAccount = await stripData(currentAccount);
            return returnAccount;

        }

        [Authorize]
        [HttpPost("change-rooms")]
        public async Task<ActionResult<int>> changeRooms(Room room)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.Claims;
            var Sid = claims.Where(c => c.Type == ClaimTypes.Sid).Select(c => c.Value).SingleOrDefault();
            Account currentAccount = await _accountsService.GetAsync(Sid);
            if (currentAccount.currentRoom == null)
            {
                await updateHeatmap(currentAccount);
                currentAccount.signInHeatmap[0][Convert.ToInt16(DateTime.Now.DayOfWeek)] += 1;
            }
            currentAccount.currentRoom = room.roomID;
            currentAccount.signInTime = DateTime.Now.TimeOfDay.ToString();
            
            await _accountsService.UpdateAsync(currentAccount.Id, currentAccount);
            return StatusCode(201);
        }

        [Authorize]
        [HttpPost("sign-out")]
        public async Task<ActionResult<int>> Logout()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.Claims;
            var Sid = claims.Where(c => c.Type == ClaimTypes.Sid).Select(c => c.Value).SingleOrDefault();
            Account currentAccount = await _accountsService.GetAsync(Sid);
            currentAccount.currentRoom = null;
            await _accountsService.UpdateAsync(currentAccount.Id, currentAccount);
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow,
                RedirectUri = null
            };
            await HttpContext.SignOutAsync(authProperties);
            return StatusCode(200);
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
                await _accountsService.UpdateAsync(currentAccount.Id, currentAccount);
                return StatusCode(200);
            }
            else
            {
                return StatusCode(401); 
            }
            
            
        }

        [Authorize]
        [HttpGet("students")]
        public async Task<ActionResult<List<Dictionary<string, string>>>> Students()
        {
            List<Account> students = await _accountsService.GetAsync();
            List<Dictionary<string, string>> signedIn = new List<Dictionary<string, string>>();
            foreach(Account s in students)
            {
                if(s.currentRoom != null)
                {
                    Dictionary<string, string> currentSignIn = new Dictionary<string, string>();
                    currentSignIn["StudentID"] = Regex.Match(s.Email, @"\d+").Value;
                    currentSignIn["RoomID"] = s.currentRoom;
                    currentSignIn["time"] = s.signInTime[0..5];
                    signedIn.Add(currentSignIn);
                }
            }
            return(signedIn);

        }

        public async Task updateHeatmap(Account a)
        {
            int curWeek = ISOWeek.GetWeekOfYear(DateTime.Now);
            int lastUpdate = a.heatmapLastUpdate ?? 0;
            if(a.heatmapLastUpdate != ISOWeek.GetWeekOfYear(DateTime.Now))
            {
                int updateDiff = curWeek - lastUpdate;
                for(int i = 0; i < 4 ; i++)
                {
                    if ((i + updateDiff) > 3)
                    {
                        break;
                    }
                    a.signInHeatmap[i + updateDiff] = a.signInHeatmap[i];
                    a.signInHeatmap[i] = new int[] {0, 0, 0, 0, 0, 0, 0};
                }
                await _accountsService.UpdateAsync(a.Id, a);
            }
        }

        public async Task<Account> stripData(Account a)
        {
            Account returnAccount = new Account();
            returnAccount.Id = Regex.Match(a.Email, @"\d+").Value;
            returnAccount.Admin = a.Admin;
            returnAccount.Email = a.Email;
            returnAccount.currentRoom = a.currentRoom;
            returnAccount.Verified = a.Verified;
            returnAccount.signInHeatmap = a.signInHeatmap;
            if (a.signInTime == null)
            {
                returnAccount.signInTime = a.signInTime;
            }
            else
            {
                returnAccount.signInTime = a.signInTime[0..5];
            }
            return returnAccount;

        }

    }
}