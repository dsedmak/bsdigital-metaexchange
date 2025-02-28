# BSDigital Meta-Exchange

## Overview
This project implements a **.NET Core Web API** that provides the best execution plan for buying or selling Bitcoin (BTC) across multiple crypto exchanges. It ensures users get the **lowest price when buying** and the **highest price when selling**, while respecting exchange balance constraints.

## Features
- Reads multiple **order books** from different exchanges.
- Implements a **best execution algorithm** to optimize BTC transactions.
- Exposes a **REST API** to query execution plans.
- Uses **Entity Framework Core** with a **SQLite** database.

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

