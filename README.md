# BSDigital Meta-Exchange

## Overview
This project implements a **.NET Core Web API** that provides the best execution plan for buying or selling Bitcoin (BTC) across multiple crypto exchanges. It ensures users get the **lowest price when buying** and the **highest price when selling**, while respecting exchange balance constraints.

## Features
- Reads multiple **order books** from different exchanges.
- Implements a **best execution algorithm** to optimize BTC transactions.
- Exposes a **REST API** to query execution plans.
- Uses **Entity Framework Core** with a **SQLite** database.

---

## Project Structure

The solution is organized into several key directories:

### Data
- **order_books_data** - The data given as part of the assignment

### Hosts
- **Hosts.WebHost** - Web API application entry point
- **Hosts.ConsoleHost** - Console application entry point
- **Hosts.EfCore.Design** - EF Core design-time project for migrations

### Source
- **Core.Api.AspNetCore** - API endpoints implementation using ASP.NET Core
- **Core.Db.EfCore.SQLite** - Database access layer using Entity Framework Core with SQLite
- **Core.UseCases** - Business logic and use case implementations

### Framework
- **Framework.UseCases** - Base abstractions for commands and queries

### Libraries
- **Injectable** - Custom dependency injection functionality

### Tests
- **Tests.Core.UseCases** - Tests for the core part of the application

---

## Best Execution Algorithm

The core of the Meta-Exchange is the best execution algorithm which:

1. **Aggregates Order Books**: Combines order books from all available exchanges

2. **Price Optimization**:
   - For buy orders: Sorts sell orders by price (ascending) to get the lowest prices first
   - For sell orders: Sorts buy orders by price (descending) to get the highest prices first

3. **Exchange Balance Constraints**: Respects available balances at each exchange
   - Cannot buy more than the available USD balance allows
   - Cannot sell more than the available BTC balance

4. **Order Execution Simulation**:
   - Walks through the order book, consuming liquidity as needed
   - Tracks remaining amount to execute
   - Stops when the order is fully satisfied or no more liquidity is available

5. **Execution Plan Generation**:
   - Creates a detailed plan with amounts to trade at each exchange
   - Calculates weighted average price
   - Determines total cost or proceeds

The algorithm ensures market takers get:
- The lowest possible price when buying BTC
- The highest possible price when selling BTC

---

## Prerequisites
Ensure your system has the following installed:

- [**.NET 9 SDK**](https://dotnet.microsoft.com/en-us/download)

---

## Running the Application

First we need to build the solution.

```bash
dotnet build ./BSDigital.MetaExchange.sln 
```

### Console Application
The console app provides a CLI interface for testing market orders:

```bash
dotnet run --project Hosts/Hosts.ConsoleHost/ --no-build -- <buy|sell> <amount> /path/to/your/order_books_data
```

### Web API
1. Update `Hosts/Hosts.WebHost/appsettings.Development.json`:
```json
{
  "SqLiteDb": {
    "ConnectionString": "Data Source=Hosts.WebHost.db",
    "OrderBooksPath": "/path/to/your/order_books_data"
  }
}
```

2. Run the application:
```bash
dotnet run --project Hosts/Hosts.WebHost/ --no-build --launch-profile http
```

3. Access the Swagger UI:
- Development: http://localhost:5092/swagger
- The API will be available at https://localhost:5092

