FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*

# TODO: find a way to install Google Chrome without --fix-broken hack
RUN wget https://dl.google.com/linux/direct/google-chrome-unstable_current_amd64.deb \
&& dpkg -i google-chrome-unstable_current_amd64.deb \
 ; apt-get -y update \
&& apt-get -y upgrade \
 ; apt -y --fix-broken install

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
# TODO: use an env variable to set the path got Chrome to "/usr/bin/google-chrome"
ENTRYPOINT ["dotnet", "TeamsMediaBot.dll"]
