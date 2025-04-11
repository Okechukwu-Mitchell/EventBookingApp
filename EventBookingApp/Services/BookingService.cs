using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;

namespace EventBookingApp.Services
{
	
	public interface IBookingService
	{
		int GetBookingCount(int eventId);
	}
	
	public class BookingService : IBookingService
	{
		private readonly IScopeProvider _scopeProvider;

		public BookingService(IScopeProvider scopeProvider)
		{
			_scopeProvider = scopeProvider;
		}

		public int GetBookingCount(int eventId)
		{
			using var scope = _scopeProvider.CreateScope(autoComplete: true);
			var database = scope.Database;
			return database.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0", eventId);
		}
	}
}



//namespace EventBookingApp.Services
//{
//	public class BookingService
//	{
//	}
//}
