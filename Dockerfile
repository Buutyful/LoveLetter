# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the project files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the entire source code and build the application
COPY . ./
RUN dotnet publish -c Release -o /out

# Use a lightweight runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# Expose the default ASP.NET Core port
EXPOSE 8080

# Set the entry point for the container
CMD ["dotnet", "LoveLetter.Api.dll"]
