# Technical report — KPSGroup ticket platform

## System structure and chosen technologies

The project is built as a simple **3-tier application**. This means that the system is split into three main parts: frontend, backend business logic, and database/data access. The project also has a React single-page application for the user interface.

- **Frontend tier** - this part is made with React 18 and is located in the `frontend/` folder. It communicates with the backend API using JSON and REST requests. React is responsible for showing pages, forms, loading states, and keeping the screen updated. The API does not store user session data on the server.

- **Business Logic tier** - this part is in the `TicketPlatform.Business` project. It contains the main logic of the system, for example authentication, event management, orders, pricing, payments, and background email sending. This layer decides what should happen in the system.

- **Data Access tier** - this part is in the `TicketPlatform.Data` project. It is responsible for working with the database. The system uses EF Core with PostgreSQL for most database operations. It also uses Dapper for some read-only event search queries.

The API project connects the frontend with the business logic. The business logic works with the data layer. The frontend only communicates with the API, not directly with the database. This keeps the project structure cleaner and easier to maintain.

JWT authentication is used, so the backend does not need to keep server-side sessions. Because of this, the same user can open the system in several browser tabs and each tab can still work normally.

## Quality requirements and implementation

| # | Requirement | Where it is implemented | Explanation |
|---|---|---|---|
| 1 | **Concurrency** - the same user can use multiple tabs | `backend/TicketPlatform.Api/Program.cs:62-79`; `frontend/src/auth/AuthContext.tsx:22-31` | The system uses JWT tokens instead of server sessions. The token is stored in `localStorage`, so every browser tab can send its own request to the API. There is no shared server session that could cause problems between tabs. |
| 2 | **Memory management** - objects are not stored longer than needed | `backend/TicketPlatform.Api/Program.cs:22-29` | Services are registered with `AddScoped`, so they live only during one HTTP request. After the request is finished, they are disposed. The system does not use `AddSession` or static fields for storing user data. |
| 3 | **Security** - protection from SQL injection | `backend/TicketPlatform.Business/Services/AuthService.cs:37`; `backend/TicketPlatform.Data/Repositories/EventSearchRepository.cs:40` | The system does not build SQL queries by simply joining user text into SQL strings. EF Core and Dapper use parameters, so user input is safely passed to the database. |
| 4 | **Data access** - ORM and Data Mapper are used | `backend/TicketPlatform.Data/AppDbContext.cs`; `backend/TicketPlatform.Business/Services/EventService.cs:25-37`; `backend/TicketPlatform.Data/Repositories/EventSearchRepository.cs:24-46` | EF Core is used as the ORM for normal database operations, especially when creating, updating, or deleting data. Dapper is used as a Data Mapper for reading event lists, where a simpler and faster query is enough. |
| 5 | **Transactions** - work is finished inside one request | `backend/TicketPlatform.Business/Services/OrderService.cs:65`; `backend/TicketPlatform.Business/Services/EventService.cs:78` | Database changes are saved with `SaveChangesAsync` during the same HTTP request. The system does not keep a transaction open while waiting for the user to do something. |
| 6 | **Data consistency** - optimistic locking is used | `backend/TicketPlatform.Data/AppDbContext.cs:31-38`; `backend/TicketPlatform.Data/AppDbContext.cs:48-55`; `backend/TicketPlatform.Business/Services/EventService.cs:52`; `backend/TicketPlatform.Api/Controllers/EventsController.cs:65-77`; `frontend/src/pages/AdminEventEditPage.tsx:54-64` | If two admins edit the same event at the same time, the system can detect that the data has changed. The first save is accepted. The second user gets a conflict message in the website and can either reload the newest version or keep their changes and overwrite. |
| 7 | **Asynchronous work** - the browser does not freeze while server work is running | `backend/TicketPlatform.Business/Services/OrderService.cs:89-92`; `backend/TicketPlatform.Business/Background/EmailBackgroundService.cs:18-31`; `frontend/src/pages/EventDetailPage.tsx:29-46` | When an order is created, the email job is put into a background queue. The API can return a response without waiting for the email sending process to finish. On the frontend, loading states are used so the page stays responsive. |
| 8 | **Cross-cutting logic** - logging can be turned on or off | `backend/TicketPlatform.Api/Filters/BusinessLogicAuditFilter.cs:11-43`; `backend/TicketPlatform.Api/Program.cs:54-58`; `backend/TicketPlatform.Api/appsettings.json:14-16` | The project has an action filter that logs information about API actions, such as user, roles, time, and controller action. This logging can be enabled or disabled in the JSON configuration file without changing the code. |
| 9 | **Extensibility** - Strategy pattern is used for pricing | `backend/TicketPlatform.Business/Pricing/IPricingStrategy.cs:7-12`; `RegularPricingStrategy.cs:5-10`; `EarlyBirdPricingStrategy.cs:5-19`; `PricingStrategyFactory.cs:10-22`; `backend/TicketPlatform.Api/Program.cs:27-29` | Different pricing rules are separated into different classes. For example, regular pricing and early bird pricing are different strategies. If a new pricing type is needed, a new strategy class can be added without changing the old pricing classes. |
| 10 | **Extensibility** - Decorator pattern is used for payments | `backend/TicketPlatform.Business/Payments/IPaymentProcessor.cs:9-12`; `MockPaymentProcessor.cs:5-12`; `LoggingPaymentProcessorDecorator.cs:8-33`; `backend/TicketPlatform.Api/Program.cs:33-44` | The payment processor can be wrapped with extra behavior, such as logging. This is done with a decorator, so the original payment processor does not need to be changed. In the future, other decorators could be added, for example fraud checking or extra validation. |

## Summary

In this project, the system is divided into clear layers: frontend, business logic, and data access. React is used for the frontend, ASP.NET Core is used for the API, and PostgreSQL is used as the database. EF Core is used for most database operations, while Dapper is used for some simple read queries.
