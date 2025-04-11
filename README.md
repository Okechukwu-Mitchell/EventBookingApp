# EventBookingApp

This repository contains an implementation of an event booking application built with Umbraco 13.
It includes a surface controller (`BookingSurfaceController`) for handling event bookings and a corresponding 
unit test suite (`BookingSurfaceControllerTests`) to verify the controller's behaviour.

## Setup Instructions And Prerequisites

To run this project locally, you need the following:
- **.NET 8 SDK**: Download from https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- **Umbraco 13**: The application is designed to work within an Umbraco 13 CMS environment.
 Download from from https://docs.umbraco.com/umbraco-cms/13.latest/fundamentals/setup/install
- **SQLite Or SQL Server**: Umbraco requires a database to store content and custom data (e.g., bookings).
- **Visual Studio 2022**: Recommended for running and debugging the solution.
  
## Approach used

The EventBookingApp is designed as a streamlined event booking system integrated with Umbraco 13, 
leveraging its CMS capabilities to manage events and bookings. The application uses a surface controller, 
BookingSurfaceController, to handle the booking process, 
including form submissions and validation logic. 
This controller interacts with Umbraco’s services (e.g., IContentService) to retrieve event data and 
store booking information as custom content nodes or database entries.

To ensure reliability, a comprehensive unit test suite, BookingSurfaceControllerTests, 
was developed using the xUnit testing framework. 
The test suite covers key scenarios such as invalid inputs (e.g., invalid email, 
empty email), edge cases (e.g., event not found, event fully booked), and successful bookings. 
The tests mock Umbraco services to isolate the controller’s logic, 
ensuring that the tests are independent of the Umbraco runtime environment. 
The project is built on .NET 8, taking advantage of its cross-platform support and performance improvements, 
and is designed to run within an Umbraco 13 CMS environment with a SQLite or SQL Server database for data persistence.

## Known Limitation

Dependency on Umbraco Services:
The application relies heavily on Umbraco’s APIs (e.g., IContentService) for event and booking management. 
This tight coupling means that changes to Umbraco’s API in future versions may require updates to the application code.

## Assumption

Umbraco Environment:
It is assumed that the application will run within a properly configured Umbraco 13 environment 
with access to necessary services (e.g., IContentService, IMemberService). The application may not function correctly outside of Umbraco or with an improperly configured setup.




