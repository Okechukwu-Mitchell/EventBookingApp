


using EventBookingApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Website.Controllers;
using Xunit;

namespace EventBookingApp.Tests
{
	public class BookingSurfaceControllerTests
	{
		private readonly Mock<IBookingService> _bookingServiceMock;
		private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
		private readonly Mock<IUmbracoContext> _umbracoContextMock;
		private readonly Mock<IPublishedRequest> _publishedRequestMock;
		private readonly Mock<IPublishedContent> _currentPageMock;
		private readonly Mock<IPublishedUrlProvider> _publishedUrlProviderMock;
		private readonly TempDataDictionary _tempData;

		public BookingSurfaceControllerTests()
		{
			// Initialize mocks
			_bookingServiceMock = new Mock<IBookingService>();
			_umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
			_umbracoContextMock = new Mock<IUmbracoContext>();
			_publishedRequestMock = new Mock<IPublishedRequest>();
			_currentPageMock = new Mock<IPublishedContent>();
			_publishedUrlProviderMock = new Mock<IPublishedUrlProvider>();

			// Set up TempData
			var tempDataProvider = new Mock<ITempDataProvider>();
			tempDataProvider.Setup(x => x.LoadTempData(It.IsAny<HttpContext>())).Returns(new Dictionary<string, object>());
			tempDataProvider.Setup(x => x.SaveTempData(It.IsAny<HttpContext>(), It.IsAny<IDictionary<string, object>>()));
			_tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);

			// Configure UmbracoContextAccessor to return UmbracoContext via out parameter
			_umbracoContextAccessorMock
				.Setup(x => x.TryGetUmbracoContext(out It.Ref<IUmbracoContext>.IsAny))
				.Returns((out IUmbracoContext context) =>
				{
					context = _umbracoContextMock.Object;
					return true;
				});

			// Configure UmbracoContext to return PublishedRequest
			_publishedRequestMock
				.Setup(x => x.PublishedContent)
				.Returns(_currentPageMock.Object);
			_umbracoContextMock
				.Setup(x => x.PublishedRequest)
				.Returns(_publishedRequestMock.Object);

			// Configure IPublishedUrlProvider to return a valid URL
			_publishedUrlProviderMock
				.Setup(x => x.GetUrl(_currentPageMock.Object, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<Uri>()))
				.Returns("/current-page");
		}

		[Fact]
		public void BookEvent_EventNotFound_ReturnsRedirectWithError()
		{
			// Arrange
			int eventId = 123;
			_bookingServiceMock
				.Setup(x => x.TryBookEvent(eventId, "test@example.com", "John", "Doe", "1234567890"))
				.Returns((false, "Event not found."));

			var controller = CreateController();

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			var redirectResult = Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("/current-page", redirectResult.Url);
			Assert.Equal("Event not found.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_EmptyEmail_ReturnsRedirectWithError()
		{
			// Arrange
			int eventId = 123;
			_bookingServiceMock
				.Setup(x => x.TryBookEvent(eventId, "", "John", "Doe", "1234567890"))
				.Returns((false, "Email is required."));

			var controller = CreateController();

			// Act
			var result = controller.BookEvent(eventId, "", "John", "Doe", "1234567890");

			// Assert
			var redirectResult = Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("/current-page", redirectResult.Url);
			Assert.Equal("Email is required.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_InvalidEmail_ReturnsRedirectWithError()
		{
			// Arrange
			int eventId = 123;
			_bookingServiceMock
				.Setup(x => x.TryBookEvent(eventId, "invalid-email", "John", "Doe", "1234567890"))
				.Returns((false, "Invalid email address."));

			var controller = CreateController();

			// Act
			var result = controller.BookEvent(eventId, "invalid-email", "John", "Doe", "1234567890");

			// Assert
			var redirectResult = Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("/current-page", redirectResult.Url);
			Assert.Equal("Invalid email address.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_EventFullyBooked_ReturnsRedirectWithError()
		{
			// Arrange
			int eventId = 123;
			_bookingServiceMock
				.Setup(x => x.TryBookEvent(eventId, "test@example.com", "John", "Doe", "1234567890"))
				.Returns((false, "Event is fully booked."));

			var controller = CreateController();

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			var redirectResult = Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("/current-page", redirectResult.Url);
			Assert.Equal("Event is fully booked.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_EmailAlreadyUsed_ReturnsRedirectWithError()
		{
			// Arrange
			int eventId = 123;
			_bookingServiceMock
				.Setup(x => x.TryBookEvent(eventId, "test@example.com", "John", "Doe", "1234567890"))
				.Returns((false, "Booking failed. This email has already been used to book this event."));

			var controller = CreateController();

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			var redirectResult = Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("/current-page", redirectResult.Url);
			Assert.Equal("Booking failed. This email has already been used to book this event.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_SuccessfulBooking_ReturnsRedirectWithSuccessMessage()
		{
			// Arrange
			int eventId = 123;
			_bookingServiceMock
				.Setup(x => x.TryBookEvent(eventId, "test@example.com", "John", "Doe", "1234567890"))
				.Returns((true, "Booking successful!"));

			var controller = CreateController();

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			var redirectResult = Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("/current-page", redirectResult.Url);
			Assert.Equal("Booking successful!", _tempData["Success"]?.ToString());
		}
		[Fact]
		public void BookEvent_NoPublishedRequest_ReturnsFallbackRedirect()
		{
			// Arrange
			int eventId = 123;
			_bookingServiceMock
				.Setup(x => x.TryBookEvent(eventId, "test@example.com", "John", "Doe", "1234567890"))
				.Returns((true, "Booking successful!"));

			// Simulate PublishedRequest being null
			_umbracoContextMock
				.Setup(x => x.PublishedRequest)
				.Returns((IPublishedRequest)null);

			var controller = CreateController();

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			var redirectResult = Assert.IsType<RedirectResult>(result);
			Assert.Equal("/", redirectResult.Url);
			Assert.Equal("Booking successful!", _tempData["Success"]?.ToString());
		}
		private BookingSurfaceController CreateController()
		{
			return new BookingSurfaceController(
				_bookingServiceMock.Object,
				_umbracoContextAccessorMock.Object,
				null, // IUmbracoDatabaseFactory
				null, // ServiceContext
				null, // AppCaches
				null, // IProfilingLogger
				_publishedUrlProviderMock.Object)
			{
				TempData = _tempData
			};
		}
	}
}
