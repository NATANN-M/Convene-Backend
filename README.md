# Convene

Convene is a  Event Management Platform designed to facilitate the creation, discovery, and management of events. 
It provides a robust backend API supporting features like ticketing, payments, real-time notifications, and personalized event recommendations using ML.NET.

##  Live Demos

-   **Backend API (Render):**[ [[https://convene-backend-7whb.onrender.com/swagger](https://convene-backend-7whb.onrender.com/swagger)]
-   **Frontend Client (Netlify):** [https://curious-buttercream-1ddd3a.netlify.app/](https://curious-buttercream-1ddd3a.netlify.app/)


##  Features

-   **User Management:**
    -   Role-based authentication (Admin, Organizer, Attendee) via JWT.
    -   Secure password hashing.
-   **Event Management:**
    -   Create, update, and manage events.
    -   Categorization and tagging.
    -   Support for multiple ticket types and pricing rules.
-  **Dynamic Pricing for Tickets**
    -   EarlyBird
    -   lastDayBefore
    -   Demandbased   
-   **Booking & Ticketing:**
    -   Seamless booking flow.
    -   QR code generation for tickets.
    -   Ticket scanning and validation (Gatekeeper role).
-   **Payments:**
    -   Integrated with **Chapa** for payment processing.
    -   Background payment verification jobs.
-   **Recommendation System:**
    -   Hybrid recommendation engine using **ML.NET** (Matrix Factorization) and rule-based scoring.
    -   Personalized event suggestions based on user interactions.
-   **Real-time Notifications:**
    -   **SignalR** integration for instant updates to users.
    -   Email notifications via SMTP.
    -   Telegram bot integration.
-   **Monitoring & Logging:**
    -   Structured logging with **Serilog**.
    -   Health checks and performance monitoring.

##  Tech Stack

-   **Framework:** ASP.NET Core 8.0
-   **Database:** PostgreSQL (Entity Framework Core)
-   **Caching:** Redis
-   **Machine Learning:** ML.NET
-   **Real-time:** SignalR
-   **Payment Gateway:** Chapa
-   **Cloud Storage:** Cloudinary (for images)
-   **Containerization:** Docker

## Prerequisites

Before you begin, ensure you have the following installed:

-   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
-   [PostgreSQL](https://www.postgresql.org/download/)


## ⚙️ Configuration

The application relies on `appsettings.json` or `appsetting.template.json` for configuration. You may need to update the `ConnectionStrings` and API keys.

1.  **Clone the repository**
2.  **Navigate to the API project:**
    ```bash
    cd Convene.API
    ```
3.  **Update `appsettings.json`  or `appsetting.template.json`** (or use User Secrets/Environment Variables):
#location `Convene-Backend/Convene.Api/appsetting.json`  or `appsetting.template.json`
    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=5432;Database=ConveneDb;Username=postgres;Password=your_password",
        "Redis": "localhost:6379"
      },
      "Jwt": {
        "Secret": "YourSuperSecretKeyHere..."
      },
      "Chapa": {
        "SecretKey": "your_chapa_secret_key"
      },
      "CloudinarySettings": {
        "CloudName": "your_cloud_name",
        "ApiKey": "your_api_key",
        "ApiSecret": "your_api_secret"
      }
    }
    ```

## 🚀 How to Run

### Option 1: Using .NET CLI

1.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```
2.  **Apply database migrations:**
    ```bash
    dotnet ef database update --project ../Convene.Infrastructure --startup-project .
    ```
    *Note: The application also attempts to apply migrations automatically on startup.*
3.  **Run the application:**
    ```bash
    dotnet run
    ```
    The API will be available at `http://localhost:5000` (or the port configured in `launchSettings.json`).

### Option 2: Using Docker

1.  **Build the Docker image:**
    ```bash
    docker build -t convene-api .
    ```
2.  **Run the container:**
    ```bash
    docker run -p 5000:8080 -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;..." convene-api
    ```

## 🧪 Testing

To run the unit and integration tests:

```bash
dotnet test
```

## 📚 API Documentation

Once the application is running, you can access the Swagger UI to explore the API endpoints:

-   **Swagger UI:** `http://localhost:5000/swagger/index.html`




