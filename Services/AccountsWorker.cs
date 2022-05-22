using LoneWorkingBackend.Models;

namespace LoneWorkingBackend.Services
{
    public class AccountsWorker : BackgroundService
    {
        
        private readonly AccountsService _accountsService;
        public AccountsWorker(AccountsService accountsService)
        {
            _accountsService = accountsService;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool reset = false;
            while (!stoppingToken.IsCancellationRequested)
            {
                if(DateTime.Now.Hour == 6 && reset == false)
                {
                    foreach(Account a in await _accountsService.GetAsync())
                    {
                        a.currentRoom = null;
                        reset = true;
                    }
                }
                else
                {
                    reset = false;
                }
                TimeSpan t = new TimeSpan(0, 30, 0);
                await Task.Delay(t);
            }
        }
    }
}