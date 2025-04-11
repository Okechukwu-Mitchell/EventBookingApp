using Serilog;
using Umbraco.Cms.Infrastructure.Migrations;

namespace EventBookingApp.Migrations
{
	public class CreateBookingsTableMigration : MigrationBase
	{
		public CreateBookingsTableMigration(IMigrationContext context) : base(context) { }

		protected override void Migrate()
		{
			if (!TableExists("Bookings"))
			{
				try
				{
					Create.Table("Bookings")
						.WithColumn("Id").AsInt32().PrimaryKey().Identity()
						.WithColumn("EventId").AsInt32().NotNullable()
						.WithColumn("BookingDate").AsDateTime().NotNullable()
						.WithColumn("FirstName").AsString(255).NotNullable()
						.WithColumn("Surname").AsString(255).NotNullable()
						.WithColumn("Email").AsString(255).NotNullable()
						.WithColumn("PhoneNumber").AsString(50).NotNullable()
						.WithColumn("Status").AsString(50).NotNullable().WithDefaultValue("Confirmed")
						.Do();
					Logger.LogInformation("Successfully created the Bookings table."); // Log using LogInformation
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "Failed to create the Bookings table: {Message}", ex.Message); //Log using LogError
					throw;
				}
			}
			else
			{
				Logger.LogInformation("Bookings table already exists, skipping creation."); // Log using LogInformation
			}
		}
	}
}
