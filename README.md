# Teams Media Bot

## Running Locally

### Configure Ngrok Tunnels

```yaml
version: "2"
region: us
tunnels:
  signalling:
    addr: 5228
    proto: http
  media:
    addr: 8445
    proto: tcp
```

If you get an error "TCP tunnels are only available to registered users", follow instructions from Ngrok to acquire auth
token.

### Configure The App

Create `appsettings.Local.json` next to `appsettings.json` with the next params:

```json
{
    "AppName": "Teams Media Bot",
    "AppId": "47670821-7231-452c-9661-4439a0ad5b35",
    "AppSecret": "",
    "PublicMediaUrl": "net.tcp://bot.yaskovdev.com:11432",
    "CertificateThumbprint": "A909502DD82AE41433E6F83886B00D4277A32A7B",
    "MediaProcessorEndpointInternalPort": "8445",
    "NotificationUrl": "https://adca-85-253-32-115.ngrok-free.app/api/calls",
    "ServiceBaseUrl": "https://graph.microsoft.com/v1.0",
    "TenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47",
    "AuthResource": "https://api.botframework.com"
}
```

Params to update after every Ngrok restart:

1. `NotificationUrl` (only host, e.g., `e244-85-253-32-115.ngrok-free.app`),
2. `PublicMediaUrl` (only port, e.g., `16847`).

`localhost:8445` should receive traffic from `2.tcp.ngrok.io` (depends on how `bot.yaskovdev.com` is
configured in DNS).

### Configure Media Platform

Open PowerShell *as admin*, go to `TeamsMediaBot\bin\x64\Debug\net6.0` (or `Release`) and
run `.\MediaPlatformStartupScript.bat` (note the dot and the backslash).

## Running With Docker In Linux Container

```powershell
docker build -f TeamsMediaBot/Dockerfile -t yaskovdev/teams-media-bot .
docker run -p 4000:4000/udp -d yaskovdev/teams-media-bot
ffplay -i srt://127.0.0.1:4000
```

## Running With Docker In Windows Container

```powershell
docker build -f TeamsMediaBot/Windows.Dockerfile -t yaskovdev/teams-media-bot .
docker run -p 4000:4000/udp -d yaskovdev/teams-media-bot
ffplay -i srt://127.0.0.1:4000
```

## API

```shell
curl -k -H 'Content-Type: application/json' https://localhost:7105/api/join-call-requests -d '{ "joinUrl": "https://teams.microsoft.com/l/meetup-join/19%3ameeting_MDA1NDJjZDgtNDRhYy00MGY4LWE2YzQtMjI1YzFlNTAzYzMw%40thread.v2/0?context=%7b%22Tid%22%3a%2272f988bf-86f1-41af-91ab-2d7cd011db47%22%2c%22Oid%22%3a%22b1b11b68-1839-4792-a462-1854254ddfe8%22%2c%22MessageId%22%3a%220%22%7d" }'
```

```shell
curl -H 'Content-Type: application/json' http://localhost:5228/api/calls -d '{ "key": "value" }'
```
