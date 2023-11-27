FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TeamsMediaBot/TeamsMediaBot.csproj", "TeamsMediaBot/"]
RUN dotnet restore "TeamsMediaBot/TeamsMediaBot.csproj"
COPY . .
WORKDIR "/src/TeamsMediaBot"
RUN dotnet build "TeamsMediaBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TeamsMediaBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TeamsMediaBot.dll"]
