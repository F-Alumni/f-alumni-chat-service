FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ChatService.sln", "./"]
COPY ["src/ChatService.Api/ChatService.Api.csproj", "src/ChatService.Api/"]
COPY ["src/ChatService.Services/ChatService.Services.csproj", "src/ChatService.Services/"]
COPY ["src/ChatService.Repositories/ChatService.Repositories.csproj", "src/ChatService.Repositories/"]
COPY ["tests/ChatService.Tests/ChatService.Tests.csproj", "tests/ChatService.Tests/"]

RUN dotnet restore "src/ChatService.Api/ChatService.Api.csproj"

COPY . .
WORKDIR "/src/src/ChatService.Api"
RUN dotnet publish "ChatService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ChatService.Api.dll"]
