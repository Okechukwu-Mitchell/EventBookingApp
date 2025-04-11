
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace EventBookingApp.Controllers
{
	public class BookingSurfaceController : SurfaceController
	{
		private readonly IUmbracoDatabaseFactory _databaseFactory;

		public BookingSurfaceController(
			IUmbracoContextAccessor umbracoContextAccessor,
			IUmbracoDatabaseFactory databaseFactory,
			ServiceContext services,
			AppCaches appCaches,
			IProfilingLogger profilingLogger,
			IPublishedUrlProvider publishedUrlProvider)
			: base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
		{
			_databaseFactory = databaseFactory;
		}

		[HttpPost]
		public IActionResult BookEventnow(int eventId, string userEmail, string firstName, string surname, string phoneNumber)
		{
			// Validate the event exists
			var eventNode = UmbracoContext.Content.GetById(eventId);
			if (eventNode == null)
			{
				TempData["Error"] = "Event not found.";
				return CurrentUmbracoPage();
			}

			// Check capacity
			int capacity = eventNode.Value<int>("capacity");
			using (var db = _databaseFactory.CreateDatabase())
			{
				int bookingCount = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0", eventId);
				if (bookingCount >= capacity)
				{
					TempData["Error"] = "Event is fully booked.";
					return CurrentUmbracoPage();
				}

				// Create the booking
				db.Execute("INSERT INTO Bookings (EventId, BookingDate, Email, FirstName, Surname, PhoneNumber, Status) VALUES (@0, @1, @2, @3, @4, @5, @6)",
					eventId, DateTime.Now, userEmail, firstName, surname, phoneNumber, "Confirmed");
			}

			TempData["Success"] = "Booking successful!";
			return RedirectToCurrentUmbracoPage();
		}

		public IActionResult BookEvent(int eventId, string userEmail, string firstName, string surname, string phoneNumber)
		{
			// Validate the event exists
			var eventNode = UmbracoContext.Content.GetById(eventId);
			if (eventNode == null)
			{
				TempData["Error"] = "Event not found.";
				return CurrentUmbracoPage();
			}

			// Validate email
			userEmail = userEmail?.Trim();
			if (string.IsNullOrEmpty(userEmail))
			{
				TempData["Error"] = "Email is required.";
				return CurrentUmbracoPage();
			}
			if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(userEmail))
			{
				TempData["Error"] = "Invalid email address.";
				return CurrentUmbracoPage();
			}

			// Check capacity
			int capacity = eventNode.Value<int>("capacity");
			using (var db = _databaseFactory.CreateDatabase())
			{
				int bookingCount = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0", eventId);
				if (bookingCount >= capacity)
				{
					TempData["Error"] = "Event is fully booked.";
					return CurrentUmbracoPage();
				}

				// Check if the email has already booked this event (case-insensitive)
				bool emailAlreadyBooked = db.ExecuteScalar<int>(
					"SELECT COUNT(*) FROM Bookings WHERE EventId = @0 AND LOWER(Email) = LOWER(@1)",
					eventId, userEmail) > 0;
				if (emailAlreadyBooked)
				{
					TempData["Error"] = "Booking failed. This email has already been used to book this event.";
					return CurrentUmbracoPage();
				}

				// Create the booking
				db.Execute("INSERT INTO Bookings (EventId, BookingDate, Email, FirstName, Surname, PhoneNumber, Status) VALUES (@0, @1, @2, @3, @4, @5, @6)",
					eventId, DateTime.Now, userEmail, firstName, surname, phoneNumber, "Confirmed");
			}

			TempData["Success"] = "Booking successful!";
			return RedirectToCurrentUmbracoPage();
		}
	}
}