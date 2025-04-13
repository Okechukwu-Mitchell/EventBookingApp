

using System;
using System.ComponentModel.DataAnnotations;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Scoping;

namespace EventBookingApp.Services
{
	public interface IBookingService
	{
		int GetBookingCount(int eventId);
		(bool success, string message) TryBookEvent(int eventId, string userEmail, string firstName, string surname, string phoneNumber);
	}

	public class BookingService : IBookingService
	{
		private readonly IScopeProvider _scopeProvider;
		private readonly IUmbracoContextAccessor _umbracoContextAccessor;

		public BookingService(IScopeProvider scopeProvider, IUmbracoContextAccessor umbracoContextAccessor)
		{
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_umbracoContextAccessor = umbracoContextAccessor ?? throw new ArgumentNullException(nameof(umbracoContextAccessor));
		}

		public int GetBookingCount(int eventId)
		{
			using var scope = _scopeProvider.CreateScope(autoComplete: true);
			return scope.Database.ExecuteScalar<int>(
				"SELECT COUNT(*) FROM Bookings WHERE EventId = @0", eventId);
		}

		public (bool success, string message) TryBookEvent(int eventId, string userEmail, string firstName, string surname, string phoneNumber)
		{
			try
			{
				var umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();
				var eventNode = umbracoContext.Content?.GetById(eventId);

				if (eventNode == null)
					return (false, "Event not found.");

				if (string.IsNullOrWhiteSpace(userEmail))
					return (false, "Email is required.");

				if (!IsValidEmail(userEmail))
					return (false, "Invalid email address.");

				var capacityProperty = eventNode.GetProperty("capacity");
				if (capacityProperty == null)
					return (false, "Event capacity not configured.");

				int capacity = (int)capacityProperty.GetValue();

				using var scope = _scopeProvider.CreateScope();
				var db = scope.Database;

				int bookingCount = GetBookingCount(eventId);
				if (bookingCount >= capacity)
					return (false, "Event is fully booked.");

				int emailCount = db.ExecuteScalar<int>(
					"SELECT COUNT(*) FROM Bookings WHERE EventId = @0 AND LOWER(Email) = LOWER(@1)",
					eventId, userEmail);

				if (emailCount > 0)
					return (false, "Booking failed. This email has already been used to book this event.");

				db.Execute(
					"INSERT INTO Bookings (EventId, BookingDate, Email, FirstName, Surname, PhoneNumber, Status) " +
					"VALUES (@0, @1, @2, @3, @4, @5, @6)",
					eventId, DateTime.Now, userEmail, firstName, surname, phoneNumber, "Confirmed");

				scope.Complete();
				return (true, "Booking successful!");
			}
			catch (Exception ex)
			{
				// Log the exception (in a real app, use a logger)
				return (false, "An error occurred while processing your booking.");
			}
		}

		private bool IsValidEmail(string email)
		{
			return new EmailAddressAttribute().IsValid(email);
		}
	}
}

