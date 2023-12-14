# escape=`

# TODO: try switching back to mcr.microsoft.com/dotnet/aspnet:6.0 and install PowerShell
FROM mcr.microsoft.com/windows:20H2 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

#RUN powershell Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy Bypass
#
#RUN powershell -Command Invoke-Expression ((New-Object Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
#RUN choco install -y googlechrome --checksum64 F0A7E673D2DA6DA8005726C0A1E040BDE7201241A5F760E99182C12B025698B2
#RUN choco install -y dotnet-6.0-runtime

FROM mcr.microsoft.com/dotnet/sdk:7.0-windowsservercore-ltsc2022 AS build

SHELL ["cmd", "/S", "/C"]

RUN `
    # Download the Build Tools bootstrapper.
    curl -SL --output vs_buildtools.exe https://aka.ms/vs/17/release/vs_buildtools.exe `
    `
    # Install Build Tools with the Microsoft.VisualStudio.Workload.AzureBuildTools workload, excluding workloads and components with known issues.
    && (start /w vs_buildtools.exe --quiet --wait --norestart --nocache `
        --installPath "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools" `
        --add Microsoft.VisualStudio.Workload.AzureBuildTools `
        --add Microsoft.NetCore.Component.SDK `
        --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        --add Microsoft.VisualStudio.Component.Windows10SDK.20348 `
        --remove Microsoft.VisualStudio.Component.Windows10SDK.10240 `
        --remove Microsoft.VisualStudio.Component.Windows10SDK.10586 `
        --remove Microsoft.VisualStudio.Component.Windows10SDK.14393 `
        --remove Microsoft.VisualStudio.Component.Windows81SDK `
        || IF "%ERRORLEVEL%"=="3010" EXIT 0) `
    `
    # Cleanup
    && del /q vs_buildtools.exe \

WORKDIR /src
RUN curl -SL --output nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
#COPY ["TeamsMediaBot/TeamsMediaBot.csproj", "TeamsMediaBot/"]
#RUN dotnet restore "TeamsMediaBot/TeamsMediaBot.csproj"
COPY . .
RUN .\nuget restore
#WORKDIR "/src/TeamsMediaBot"
RUN ("C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\bin\msbuild" /nr:false /bl /p:Platform=x64 /p:Configuration=Release)

FROM build AS publish
RUN ("C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\bin\msbuild" /t:TeamsMediaBot:Publish /p:Configuration=Release /p:Platform=x64 /p:PublishDir="/app/publish" /p:UseAppHost=false)

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# TODO: use an env variable to set the path got Chrome to "C:/Program Files/Google/Chrome/Application/chrome.exe"
ENTRYPOINT ["dotnet", "TeamsMediaBot.dll"]
