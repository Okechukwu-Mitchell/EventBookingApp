using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Migrations;
using EventBookingApp.Migrations;

namespace EventBookingApp.Composers
{
	public class BookingMigrationComposer : IComposer
	{
		public void Compose(IUmbracoBuilder builder)
		{
			// Register the migration plan as a MigrationPlan so Umbraco can execute it
			builder.Services.AddUnique<MigrationPlan, BookingMigrationPlan>();
		}
	}
}

