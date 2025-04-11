
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Core.Services;
using EventBookingApp.Migrations;
using EventBookingApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
	.AddBackOffice()
	.AddWebsite()
	.AddDeliveryApi()
	.AddComposers()
	.Build();

// Register the booking service
builder.Services.AddScoped<IBookingService, BookingService>();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

// Ensure the migration runs and creates the Bookings table if  not created
using (var scope = app.Services.CreateScope())
{
	var migrationPlan = new BookingMigrationPlan();
	var upgrader = new Upgrader(migrationPlan);
	var scopeProvider = scope.ServiceProvider.GetRequiredService<IScopeProvider>();
	var migrationExecutor = scope.ServiceProvider.GetRequiredService<IMigrationPlanExecutor>();
	var keyValueService = scope.ServiceProvider.GetRequiredService<IKeyValueService>();
	upgrader.Execute(migrationExecutor, scopeProvider, keyValueService);
}

app.UseUmbraco()
	.WithMiddleware(u =>
	{
		u.UseBackOffice();
		u.UseWebsite();
	})
	.WithEndpoints(u =>
	{
		u.UseInstallerEndpoints();
		u.UseBackOfficeEndpoints();
		u.UseWebsiteEndpoints();
	});

await app.RunAsync();
