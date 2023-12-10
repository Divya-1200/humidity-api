# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY . .

# Install the Entity Framework tools
RUN dotnet tool install --global dotnet-ef --version 7.0

# Add the tools directory to the PATH
ENV PATH="/root/.dotnet/tools:${PATH}"


# Build and publish the application
RUN dotnet publish -c Release -o /app/out

# Stage 2: Create a runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .

# Copy the tools directory to the runtime image
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools

EXPOSE 5029

# Run the database update command and start the application
CMD ["dotnet", "humidity-api-minimal.dll"]
