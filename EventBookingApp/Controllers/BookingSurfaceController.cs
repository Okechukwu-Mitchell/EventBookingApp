
using EventBookingApp.Services;
using Microsoft.AspNetCore.Mvc;
using NPoco;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

public class BookingSurfaceController : SurfaceController
{
	private readonly IBookingService _bookingService;
	private readonly IPublishedUrlProvider _publishedUrlProvider;

	public BookingSurfaceController(
		IBookingService bookingService,
		IUmbracoContextAccessor umbracoContextAccessor,
		IUmbracoDatabaseFactory databaseFactory,
		ServiceContext services,
		AppCaches appCaches,
		IProfilingLogger profilingLogger,
		IPublishedUrlProvider publishedUrlProvider)
		: base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
	{
		_bookingService = bookingService;
		_publishedUrlProvider = publishedUrlProvider;
	}

	[HttpPost]
	public IActionResult BookEvent(int eventId, string userEmail, string firstName, string surname, string phoneNumber)
	{
		var (success, message) = _bookingService.TryBookEvent(eventId, userEmail, firstName, surname, phoneNumber);
		TempData[success ? "Success" : "Error"] = message;
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
