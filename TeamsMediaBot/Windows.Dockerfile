# TODO: try switching back to mcr.microsoft.com/dotnet/aspnet:6.0 and install PowerShell
FROM mcr.microsoft.com/windows:20H2 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN powershell Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy Bypass

RUN powershell -Command Invoke-Expression ((New-Object Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
RUN choco install -y googlechrome --checksum64 F0A7E673D2DA6DA8005726C0A1E040BDE7201241A5F760E99182C12B025698B2
RUN choco install -y dotnet-6.0-runtime

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
# TODO: use an env variable to set the path got Chrome to "C:/Program Files/Google/Chrome/Application/chrome.exe"
ENTRYPOINT ["dotnet", "TeamsMediaBot.dll"]
