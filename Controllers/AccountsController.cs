using LoneWorkingBackend.Models;
using LoneWorkingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

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

            // Accounts are not admin by default
            newAccount.Admin = false;

            // Commit changes to db, return 201
            // Null is placeholder, update when GET endpoint added for accounts
            await _accountsService.CreateAsync(newAccount);
            return CreatedAtAction(null, new {id = newAccount.Id}, newAccount);
        }

    }
}