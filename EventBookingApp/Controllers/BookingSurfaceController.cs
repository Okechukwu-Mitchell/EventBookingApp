
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
		private readonly IPublishedUrlProvider _publishedUrlProvider;

		public BookingSurfaceController(
			IUmbracoContextAccessor umbracoContextAccessor,
			IUmbracoDatabaseFactory databaseFactory,
			ServiceContext services,
			AppCaches appCaches,
			IProfilingLogger profilingLogger,
			IPublishedUrlProvider publishedUrlProvider)
			: base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
		{
			_publishedUrlProvider = publishedUrlProvider;
		}

		[HttpPost]
		public IActionResult BookEvent(int eventId, string userEmail, string firstName, string surname, string phoneNumber)
		{
			// Get the event node
			var eventNode = UmbracoContext.Content?.GetById(eventId);
			if (eventNode == null)
			{
				TempData["Error"] = "Event not found.";
				return RedirectToCurrentPage();
			}

			// Validate email
			if (string.IsNullOrWhiteSpace(userEmail))
			{
				TempData["Error"] = "Email is required.";
				return RedirectToCurrentPage();
			}

			if (!userEmail.Contains("@"))
			{
				TempData["Error"] = "Invalid email address.";
				return RedirectToCurrentPage();
			}

			// Check event capacity
			var capacityProperty = eventNode.GetProperty("capacity");
			int capacity = capacityProperty != null ? (int)capacityProperty.GetValue() : 0;
			using (var db = DatabaseFactory.CreateDatabase())
			{
				int bookingCount = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0", eventId);
				if (bookingCount >= capacity)
				{
					TempData["Error"] = "Event is fully booked.";
					return RedirectToCurrentPage();
				}

				// Check if email is already used
				int emailCount = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0 AND LOWER(Email) = LOWER(@1)", eventId, userEmail);
				if (emailCount > 0)
				{
					TempData["Error"] = "Booking failed. This email has already been used to book this event.";
					return RedirectToCurrentPage();
				}

				// Create the booking
				db.Execute(
					"INSERT INTO Bookings (EventId, BookingDate, Email, FirstName, Surname, PhoneNumber, Status) VALUES (@0, @1, @2, @3, @4, @5, @6)",
					eventId, DateTime.Now, userEmail, firstName, surname, phoneNumber, "Confirmed");
			}

			TempData["Success"] = "Booking successful!";
			return RedirectToCurrentPage();
		}

		private IActionResult RedirectToCurrentPage()
		{
			var currentPage = UmbracoContext.PublishedRequest?.PublishedContent;
			if (currentPage == null)
			{
				return Redirect("/"); // Fall back if current page is not available
			}

			var url = _publishedUrlProvider.GetUrl(currentPage);
			return LocalRedirect(url);
		}
	}
}