


using EventBookingApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using Xunit;

namespace EventBookingApp.Tests
{
	public class BookingSurfaceControllerTests
	{
		private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
		private readonly Mock<IUmbracoContext> _umbracoContextMock;
		private readonly Mock<IUmbracoDatabaseFactory> _databaseFactoryMock;
		private readonly Mock<IUmbracoDatabase> _databaseMock;
		private readonly Mock<IPublishedContent> _eventNodeMock;
		private readonly Mock<IPublishedContentCache> _contentCacheMock;
		private readonly Mock<IPublishedRequest> _publishedRequestMock;
		private readonly Mock<IPublishedContent> _currentPageMock;
		private readonly Mock<IProfilingLogger> _profilingLoggerMock;
		private readonly Mock<IPublishedUrlProvider> _publishedUrlProviderMock;
		private readonly TempDataDictionary _tempData;

		// Delegate to match the TryGetUmbracoContext signature
		private delegate void TryGetUmbracoContextDelegate(ref IUmbracoContext context);

		public BookingSurfaceControllerTests()
		{
			// Set up mocks for all dependencies
			_umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
			_umbracoContextMock = new Mock<IUmbracoContext>();
			_databaseFactoryMock = new Mock<IUmbracoDatabaseFactory>();
			_databaseMock = new Mock<IUmbracoDatabase>();
			_eventNodeMock = new Mock<IPublishedContent>();
			_contentCacheMock = new Mock<IPublishedContentCache>();
			_publishedRequestMock = new Mock<IPublishedRequest>();
			_currentPageMock = new Mock<IPublishedContent>();
			_profilingLoggerMock = new Mock<IProfilingLogger>();
			_publishedUrlProviderMock = new Mock<IPublishedUrlProvider>();

			// Set up TempData
			var tempDataProvider = new Mock<ITempDataProvider>();
			tempDataProvider.Setup(x => x.LoadTempData(It.IsAny<HttpContext>())).Returns(new Dictionary<string, object>());
			tempDataProvider.Setup(x => x.SaveTempData(It.IsAny<HttpContext>(), It.IsAny<IDictionary<string, object>>()));
			_tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);

			// Configure UmbracoContextAccessor to return UmbracoContext
			_umbracoContextAccessorMock
				.Setup(x => x.TryGetUmbracoContext(out It.Ref<IUmbracoContext>.IsAny))
				.Callback(new TryGetUmbracoContextDelegate((ref IUmbracoContext context) => context = _umbracoContextMock.Object))
				.Returns(true);

			// Configure UmbracoContext to return ContentCache
			_umbracoContextMock
				.Setup(x => x.Content)
				.Returns(_contentCacheMock.Object);

			// Configure UmbracoContext to return PublishedRequest
			_publishedRequestMock
				.Setup(x => x.PublishedContent)
				.Returns(_currentPageMock.Object);
			_publishedRequestMock
				.Setup(x => x.Uri)
				.Returns(new Uri("http://example.com"));
			_umbracoContextMock
				.Setup(x => x.PublishedRequest)
				.Returns(_publishedRequestMock.Object);

			// Configure IPublishedUrlProvider to return a valid URL for the current page
			_publishedUrlProviderMock
				.Setup(x => x.GetUrl(_currentPageMock.Object, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<Uri>()))
				.Returns("/current-page");

			// Configure DatabaseFactory to return Database
			_databaseFactoryMock
				.Setup(x => x.CreateDatabase())
				.Returns(_databaseMock.Object);
		}

		[Fact]
		public void BookEvent_EventNotFound_ReturnsCurrentUmbracoPageWithError()
		{
			// Arrange
			int eventId = 123;
			_contentCacheMock
				.Setup(x => x.GetById(eventId))
				.Returns((IPublishedContent)null); // Event not found

			var controller = new BookingSurfaceController(
				_umbracoContextAccessorMock.Object,
				_databaseFactoryMock.Object,
				null, // ServiceContext
				null, // AppCaches
				_profilingLoggerMock.Object,
				_publishedUrlProviderMock.Object)
			{
				TempData = _tempData
			};

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("Event not found.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_EmptyEmail_ReturnsCurrentUmbracoPageWithError()
		{
			// Arrange
			int eventId = 123;
			_contentCacheMock
				.Setup(x => x.GetById(eventId))
				.Returns(_eventNodeMock.Object);

			var controller = new BookingSurfaceController(
				_umbracoContextAccessorMock.Object,
				_databaseFactoryMock.Object,
				null, // ServiceContext
				null, // AppCaches
				_profilingLoggerMock.Object,
				_publishedUrlProviderMock.Object)
			{
				TempData = _tempData
			};

			// Act
			var result = controller.BookEvent(eventId, "", "John", "Doe", "1234567890");

			// Assert
			Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("Email is required.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_InvalidEmail_ReturnsCurrentUmbracoPageWithError()
		{
			// Arrange
			int eventId = 123;
			_contentCacheMock
				.Setup(x => x.GetById(eventId))
				.Returns(_eventNodeMock.Object);

			var controller = new BookingSurfaceController(
				_umbracoContextAccessorMock.Object,
				_databaseFactoryMock.Object,
				null, // ServiceContext
				null, // AppCaches
				_profilingLoggerMock.Object,
				_publishedUrlProviderMock.Object)
			{
				TempData = _tempData
			};

			// Act
			var result = controller.BookEvent(eventId, "invalid-email", "John", "Doe", "1234567890");

			// Assert
			Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("Invalid email address.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_EventFullyBooked_ReturnsCurrentUmbracoPageWithError()
		{
			// Arrange
			int eventId = 123;
			int capacity = 2;
			var propertyMock = new Mock<IPublishedProperty>();
			propertyMock.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(capacity);
			_contentCacheMock
				.Setup(x => x.GetById(eventId))
				.Returns(_eventNodeMock.Object);
			_eventNodeMock
				.Setup(x => x.GetProperty("capacity"))
				.Returns(propertyMock.Object);
			_databaseMock
				.Setup(x => x.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0", eventId))
				.Returns(capacity); // Booking count equals capacity

			var controller = new BookingSurfaceController(
				_umbracoContextAccessorMock.Object,
				_databaseFactoryMock.Object,
				null, // ServiceContext
				null, // AppCaches
				_profilingLoggerMock.Object,
				_publishedUrlProviderMock.Object)
			{
				TempData = _tempData
			};

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("Event is fully booked.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_EmailAlreadyUsed_ReturnsCurrentUmbracoPageWithError()
		{
			// Arrange
			int eventId = 123;
			int capacity = 2;
			var propertyMock = new Mock<IPublishedProperty>();
			propertyMock.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(capacity);
			_contentCacheMock
				.Setup(x => x.GetById(eventId))
				.Returns(_eventNodeMock.Object);
			_eventNodeMock
				.Setup(x => x.GetProperty("capacity"))
				.Returns(propertyMock.Object);
			_databaseMock
				.Setup(x => x.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0", eventId))
				.Returns(1); // Booking count less than capacity
			_databaseMock
				.Setup(x => x.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0 AND LOWER(Email) = LOWER(@1)", eventId, "test@example.com"))
				.Returns(1); // Email already used

			var controller = new BookingSurfaceController(
				_umbracoContextAccessorMock.Object,
				_databaseFactoryMock.Object,
				null, // ServiceContext
				null, // AppCaches
				_profilingLoggerMock.Object,
				_publishedUrlProviderMock.Object)
			{
				TempData = _tempData
			};

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("Booking failed. This email has already been used to book this event.", _tempData["Error"]?.ToString());
		}

		[Fact]
		public void BookEvent_SuccessfulBooking_ReturnsRedirectWithSuccessMessage()
		{
			// Arrange
			int eventId = 123;
			int capacity = 2;
			var propertyMock = new Mock<IPublishedProperty>();
			propertyMock.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(capacity);
			_contentCacheMock
				.Setup(x => x.GetById(eventId))
				.Returns(_eventNodeMock.Object);
			_eventNodeMock
				.Setup(x => x.GetProperty("capacity"))
				.Returns(propertyMock.Object);
			_databaseMock
				.Setup(x => x.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0", eventId))
				.Returns(1); // Booking count less than capacity
			_databaseMock
				.Setup(x => x.ExecuteScalar<int>("SELECT COUNT(*) FROM Bookings WHERE EventId = @0 AND LOWER(Email) = LOWER(@1)", eventId, "test@example.com"))
				.Returns(0); // Email not used
			_databaseMock
				.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
				.Returns(1); // Simulate successful insert

			var controller = new BookingSurfaceController(
				_umbracoContextAccessorMock.Object,
				_databaseFactoryMock.Object,
				null, // ServiceContext
				null, // AppCaches
				_profilingLoggerMock.Object,
				_publishedUrlProviderMock.Object)
			{
				TempData = _tempData
			};

			// Act
			var result = controller.BookEvent(eventId, "test@example.com", "John", "Doe", "1234567890");

			// Assert
			Assert.IsType<LocalRedirectResult>(result);
			Assert.Equal("Booking successful!", _tempData["Success"]?.ToString());
			_databaseMock.Verify(x => x.Execute(
				"INSERT INTO Bookings (EventId, BookingDate, Email, FirstName, Surname, PhoneNumber, Status) VALUES (@0, @1, @2, @3, @4, @5, @6)",
				It.IsAny<object[]>()), Times.Once());
		}
	}
}
