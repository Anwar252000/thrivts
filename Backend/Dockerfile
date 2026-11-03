# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the .csproj files, so Docker can cache the restore layer
# and skip it on rebuilds that only change .cs files.
COPY ["src/Thrivts.Api/Thrivts.Api.csproj", "src/Thrivts.Api/"]
COPY ["src/Thrivts.Application/Thrivts.Application.csproj", "src/Thrivts.Application/"]
COPY ["src/Thrivts.Infrastructure/Thrivts.Infrastructure.csproj", "src/Thrivts.Infrastructure/"]
COPY ["src/Thrivts.Domain/Thrivts.Domain.csproj", "src/Thrivts.Domain/"]
RUN dotnet restore "src/Thrivts.Api/Thrivts.Api.csproj"

COPY . .
RUN dotnet publish "src/Thrivts.Api/Thrivts.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Thrivts.Api.dll"]
