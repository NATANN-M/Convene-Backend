# Use SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Convene.API/Convene.API.csproj", "Convene.API/"]
COPY ["Convene.Application/Convene.Application.csproj", "Convene.Application/"]
COPY ["Convene.Domain/Convene.Domain.csproj", "Convene.Domain/"]
COPY ["Convene.Infrastructure/Convene.Infrastructure.csproj", "Convene.Infrastructure/"]
COPY ["Convene.Shared/Convene.Shared.csproj", "Convene.Shared/"]

RUN dotnet restore "Convene.API/Convene.API.csproj"

# Copy the rest of the code and build
COPY . .
WORKDIR "/src/Convene.API"
RUN dotnet build "Convene.API.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "Convene.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# The app listens on the port defined by the PORT environment variable (set by Render)
# Our Program.cs already handles this.
ENTRYPOINT ["dotnet", "Convene.API.dll"]
