# Tickit - Ticket Management System (Backend API)

A robust RESTful API for ticket management built with ASP.NET Core 8 following Clean Architecture principles.

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![C#](https://img.shields.io/badge/C%23-12-green)
![EF Core](https://img.shields.io/badge/EF%20Core-8.0-blue)
![SQLite](https://img.shields.io/badge/SQLite-3-lightblue)

## Features

- **Clean Architecture**: Separation of concerns with Domain, Application, Infrastructure, and API layers
- **JWT Authentication**: Secure token-based authentication with refresh tokens
- **Role-Based Authorization**: Admin and User roles with granular permissions
- **Full CRUD Operations**: Complete API for all entities
- **Real-time Notifications**: SignalR hubs for live updates
- **File Uploads**: Support for ticket attachments
- **Audit Logging**: Track all changes to entities

## Tech Stack

- **Framework**: ASP.NET Core 8
- **ORM**: Entity Framework Core 8
- **Database**: SQLite (easily switchable to SQL Server/PostgreSQL)
- **Authentication**: JWT Bearer tokens
- **Real-time**: SignalR
- **Validation**: FluentValidation
- **Mapping**: AutoMapper

## Architecture

```
src/
├── TicketSystem.API/           # Presentation layer (Controllers, Middleware)
├── TicketSystem.Application/   # Application layer (Interfaces, DTOs, Behaviors)
├── TicketSystem.Domain/        # Domain layer (Entities, Enums, Events)
└── TicketSystem.Infrastructure/ # Infrastructure layer (EF Core, Identity, Services)
```

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/register` | User registration |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | User logout |

### Tickets
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tickets` | List all tickets (paginated) |
| GET | `/api/tickets/{id}` | Get ticket by ID |
| POST | `/api/tickets` | Create new ticket |
| PUT | `/api/tickets/{id}` | Update ticket |
| DELETE | `/api/tickets/{id}` | Delete ticket |
| POST | `/api/tickets/{id}/assign` | Assign ticket to user |
| POST | `/api/tickets/{id}/status` | Change ticket status |
| GET | `/api/tickets/{id}/comments` | Get ticket comments |
| POST | `/api/tickets/{id}/comments` | Add comment to ticket |

### Companies
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/companies` | List all companies |
| GET | `/api/companies/{id}` | Get company by ID |
| POST | `/api/companies` | Create company |
| PUT | `/api/companies/{id}` | Update company |
| DELETE | `/api/companies/{id}` | Delete company |

### Categories
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` | List all categories |
| POST | `/api/categories` | Create category |
| PUT | `/api/categories/{id}` | Update category |
| DELETE | `/api/categories/{id}` | Delete category |

### Priorities
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/priorities` | List all priorities |
| POST | `/api/priorities` | Create priority |
| PUT | `/api/priorities/{id}` | Update priority |
| DELETE | `/api/priorities/{id}` | Delete priority |

### Statuses
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/statuses` | List all statuses |
| POST | `/api/statuses` | Create status |
| PUT | `/api/statuses/{id}` | Update status |
| DELETE | `/api/statuses/{id}` | Delete status |

### Teams
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/teams` | List all teams |
| POST | `/api/teams` | Create team |
| PUT | `/api/teams/{id}` | Update team |
| DELETE | `/api/teams/{id}` | Delete team |

### Drafts
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/drafts` | List user's drafts |
| GET | `/api/drafts/{id}` | Get draft by ID |
| POST | `/api/drafts` | Create draft |
| PUT | `/api/drafts/{id}` | Update draft |
| DELETE | `/api/drafts/{id}` | Delete draft |

### Attachments
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/attachments/ticket/{id}` | Upload attachment |
| GET | `/api/attachments/{id}/download` | Download attachment |
| DELETE | `/api/attachments/{id}` | Delete attachment |

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 / VS Code / Rider

### Installation

```bash
# Clone the repository
git clone https://github.com/Core004/Tickit-webApi.git
cd Tickit-webApi

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/TicketSystem.API
```

The API will be available at `http://localhost:5050`

### Database Setup

The application uses SQLite by default. The database is automatically created and seeded on first run.

To apply migrations manually:

```bash
cd src/TicketSystem.API
dotnet ef database update --project ../TicketSystem.Infrastructure
```

## Configuration

Configure the application in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=ticketsystem.db"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "TicketSystem.API",
    "Audience": "TicketSystem.Client",
    "ExpiryMinutes": 60
  }
}
```

## Default Admin Account

```
Email: admin@gmail.com
Password: Admin@123
```

## SignalR Hubs

- `/hubs/notifications` - Real-time notifications
- `/hubs/teamchat` - Team chat functionality
- `/hubs/chatbot` - AI chatbot integration

## Project Structure

### Domain Layer
- Entities (Ticket, Company, Category, Priority, etc.)
- Enums (TicketPriority, TicketStatus, etc.)
- Domain Events
- Interfaces

### Application Layer
- Common interfaces
- DTOs and mappings
- Validation behaviors
- Exception handling

### Infrastructure Layer
- Entity Framework Core DbContext
- Entity configurations
- Migrations
- Identity services
- Token service

### API Layer
- Controllers
- Middleware (Exception handling)
- SignalR Hubs

## Frontend Repository

The frontend application is available at: [Tickit-webUi](https://github.com/Core004/Tickit-webUi)

## License

MIT License
