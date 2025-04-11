using Umbraco.Cms.Infrastructure.Migrations;

namespace EventBookingApp.Migrations
{
	public class BookingMigrationPlan : MigrationPlan
	{
		public BookingMigrationPlan() : base("BookingMigrationPlan")
		{
			From(string.Empty)
				.To<CreateBookingsTableMigration>("CreateBookingsTable");
		}
	}
}