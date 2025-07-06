# FinanceFlow Pro 💰

A comprehensive personal finance management application built with ASP.NET Core and Blazor WebAssembly.

## Features ✨

### 🔐 Authentication & Security
- User registration and login system
- JWT token-based authentication
- Secure password handling with ASP.NET Core Identity

### 💸 Transaction Management
- Record income, expenses, and transfers
- Categorize transactions with custom categories
- Add tags for better organization
- Advanced filtering and search capabilities

### 📊 Budget Planning
- Create monthly, quarterly, or custom period budgets
- Set spending limits by category
- Real-time budget tracking and alerts
- Visual progress indicators

### 🎯 Financial Goals
- Set savings goals with target amounts and dates
- Track progress with visual indicators
- Different goal types (savings, debt payoff, etc.)
- Automatic calculation of required monthly contributions

### 🏦 Account Management
- Multiple account types (Checking, Savings, Credit, Investment, etc.)
- Real-time balance tracking
- Account-specific transaction history

### 📈 Reports & Analytics
- Income vs. expenses analysis
- Spending by category breakdowns
- Budget performance tracking
- Financial goal progress reports

### 🎨 Modern UI
- Responsive Blazor WebAssembly frontend
- Modern design with Bootstrap and FontAwesome icons
- Interactive dashboards and charts
- Mobile-friendly interface

## Technology Stack 🛠️

### Backend
- **ASP.NET Core 8.0** - Web API framework
- **Entity Framework Core** - Database ORM
- **SQL Server** - Database
- **ASP.NET Core Identity** - User management
- **JWT Authentication** - Token-based security

### Frontend
- **Blazor WebAssembly** - Client-side framework
- **Bootstrap 5** - CSS framework
- **FontAwesome** - Icons
- **HTTP Client** - API communication

## Project Structure 📁

```
src/
├── FinanceFlow.Api/           # Backend API
│   ├── Controllers/           # API endpoints
│   ├── Models/               # Data models and DTOs
│   ├── Data/                 # Database context
│   └── Services/             # Business logic
└── FinanceFlow.Client/        # Frontend Blazor app
    ├── Pages/                # Blazor pages
    ├── Layout/               # Layout components
    ├── Services/             # Client services
    └── wwwroot/              # Static assets
```

## Getting Started 🚀

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/cavr46/financeflow-pro.git
   cd financeflow-pro
   ```

2. **Set up the database**
   ```bash
   cd src/FinanceFlow.Api
   dotnet ef database update
   ```

3. **Run the API**
   ```bash
   dotnet run --project src/FinanceFlow.Api
   ```
   API will be available at `https://localhost:7001`

4. **Run the Client (in a new terminal)**
   ```bash
   dotnet run --project src/FinanceFlow.Client
   ```
   Client will be available at `https://localhost:7002`

### Configuration

Update the connection string in `src/FinanceFlow.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FinanceFlowDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

## API Endpoints 📡

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login

### Transactions
- `GET /api/transactions` - Get user transactions
- `POST /api/transactions` - Create new transaction
- `PUT /api/transactions/{id}` - Update transaction
- `DELETE /api/transactions/{id}` - Delete transaction

### Budgets
- `GET /api/budgets` - Get user budgets
- `POST /api/budgets` - Create new budget
- `GET /api/budgets/summary` - Get budget summary

### Goals
- `GET /api/goals` - Get user goals
- `POST /api/goals` - Create new goal
- `POST /api/goals/{id}/contributions` - Add goal contribution

### Dashboard
- `GET /api/dashboard/overview` - Get dashboard overview
- `GET /api/dashboard/spending-by-category` - Get spending analysis
- `GET /api/dashboard/income-vs-expenses` - Get income/expense trends

## Development 🔧

### Database Migrations

To add a new migration:
```bash
cd src/FinanceFlow.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Building for Production

```bash
# Build API
dotnet publish src/FinanceFlow.Api -c Release

# Build Client
dotnet publish src/FinanceFlow.Client -c Release
```

## Features in Detail 📝

### Dashboard
- Real-time financial overview with key metrics
- Recent transactions with category icons
- Account balances summary
- Quick action buttons for common tasks

### Transaction Management
- Support for multiple transaction types (Income, Expense, Transfer)
- Category and tag organization
- Date range filtering
- Search functionality
- Bulk operations

### Budget System
- Flexible budget periods (monthly, quarterly, yearly)
- Category-based allocation
- Progress tracking with visual indicators
- Overspending alerts and notifications

### Goal Tracking
- Multiple goal types (Emergency Fund, Vacation, House Down Payment, etc.)
- Progress visualization
- Automatic calculation of required contributions
- Goal completion tracking

## Contributing 🤝

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License 📄

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support 💬

If you have any questions or need help, please open an issue on GitHub.

---

**Built with ❤️ using ASP.NET Core and Blazor WebAssembly**