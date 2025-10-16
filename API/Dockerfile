FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["*.sln", "."]
COPY ["API/*.csproj", "API/"]
COPY ["Application/*.csproj", "Application/"]
COPY ["Domain/*.csproj", "Domain/"]
COPY ["Infrastructure/*.csproj", "Infrastructure/"]

RUN dotnet restore

COPY . .

RUN dotnet build -c Release --no-restore

FROM build AS publish
RUN dotnet publish "API/API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "API.dll"]