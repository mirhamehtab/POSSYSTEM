# SYNEXUS Point of Sale (POS) System

A single-store, web-based Point of Sale system built with ASP.NET Core MVC, Entity Framework Core, and SQL Server. Built to satisfy the SYNEXUS Point of Sale System SRS (v1.0, 04 August 2026).

## Tech Stack

- **Framework:** ASP.NET Core MVC (.NET 8.0)
- **ORM:** Entity Framework Core 8.0 (code-first migrations)
- **Database:** Microsoft SQL Server
- **Auth:** ASP.NET Core Identity, role-based (Administrator, Manager, Cashier)
- **UI:** Razor views, Bootstrap

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, SQL Express, or full SQL Server)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`

## Getting Started

### 1. Clone the repository

```bash
git clone <repo-url>
cd POSSystem/POSSystem
```

### 2. Configure the database connection

Open `appsettings.json` (or better, `appsettings.Development.json` / user-secrets for local dev) and set the `default` connection string to point at your own SQL Server instance:

```json
"ConnectionStrings": {
  "default": "Server=YOUR_SERVER\\SQLEXPRESS;Database=POSSystem;Trusted_Connection=True;TrustServerCertificate=True"
}
```

> **Note:** Do not commit real server names, credentials, or production connection strings. Use `dotnet user-secrets` for local development and environment variables / a secrets manager in production (see NFR-007).

### 3. Restore packages and apply migrations

```bash
dotnet restore
dotnet ef database update
```

This creates the database and applies all migrations (`InitialSetup`, `AddSales`, `AddPurchases`, `AddReturns`, `AddExpenses`).

### 4. Run the application

```bash
dotnet run
```

The app will be available at the URL shown in the console (typically `https://localhost:5001` or similar).

## First Login / Seeded Accounts

On first startup, the application automatically seeds:

- **Roles:** `Administrator`, `Manager`, `Cashier`
- **Default Administrator account:**
  - Email: `admin@synexus.com`
  - Password: `Admin@123`
- **A "Walk-in Customer" record** (required for sales that don't specify a customer, per FR-036)

**Important:** Log in with the default admin account and change the password immediately (or create a new admin and deactivate this one) before using the system for anything beyond local testing. Do not deploy to production with this default password still active.

From the Administrator account you can:
- Create additional users and assign them the Manager or Cashier role (Users screen)
- Configure business/tax/currency/receipt settings (Settings screen)
- Add categories, products, suppliers, and enter opening stock before taking sales

## Project Structure

```
POSSystem/
  Controllers/     # MVC controllers (Account, Products, Sales, Purchases, Returns, Reports, Users, Settings, etc.)
  Models/          # Domain models, DbContext, ViewModels
  Views/           # Razor views, organized by controller
  Migrations/      # EF Core migrations
  Services/        # AuditLogger and other services
  wwwroot/         # Static assets (css, js, lib)
  Program.cs       # App startup, Identity config, role/admin seeding
```

## Roles and Permissions

| Module              | Administrator | Manager | Cashier                     |
|---------------------|:-------------:|:-------:|:----------------------------|
| Sales (create)      | Yes           | Yes     | Yes                          |
| View all sales      | Yes           | Yes     | Own sales / authorized scope |
| Returns             | Yes           | Yes     | Only if permission granted   |
| Products/Categories | Yes           | Yes     | No                           |
| Stock adjustments   | Yes           | Yes     | No                           |
| Suppliers/Purchases | Yes           | Yes     | No                           |
| Expenses            | Yes           | Yes     | No                           |
| Reports             | Yes           | Yes     | Own summary only (if enabled)|
| User management     | Yes           | No      | No                           |
| System settings     | Yes           | No      | No                           |
| Audit log           | Yes           | Yes     | No                           |

Full detail in the SRS, Section 3.

## Known Limitations (MVP scope)

Per the SRS "Out of Scope" section, this MVP does **not** include:
- Multi-branch / multi-warehouse support
- Native mobile apps or offline sync
- E-commerce storefront
- Payment gateway / card-terminal integration
- Split payments or customer credit accounts
- Full accounting/payroll/general ledger
- Purchase returns, supplier payment ledger, batch/expiry tracking

## License / Ownership

Internal project prepared for SYNEXUS Software Technologies by Mirha Mehtab
