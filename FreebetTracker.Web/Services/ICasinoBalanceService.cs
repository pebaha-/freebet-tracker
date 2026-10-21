using FreebetTracker.Web.Models;

namespace FreebetTracker.Web.Services
{
    public interface ICasinoBalanceService
    {
        static abstract IReadOnlyList<CasinoBalance> SelectForReminder(IEnumerable<CasinoBalance> accounts, DateOnly? today = null);
        static abstract string GetStatus(CasinoBalance account, DateOnly? today = null);
    }
}
