using System.Threading.Tasks;

namespace Rental.Business.Interfaces
{
    public interface IBackgroundJobsService
    {
        Task ExtendSchedulesAsync();
        Task UpdateBookingStatusesAsync();
    }
}
