# Voyl

> Voyl (prononcé « voïl », comme "oil" en anglais avec un V) - surcouche d'écran d'accueil Windows ultra-légère.

## Build

```powershell
cd src/Voyl
dotnet build -c Release
```

## Publish (AOT)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishAot=true
```

L'exécutable est généré dans : `bin/Release/net10.0/win-x64/publish/Voyl.exe`

## Lancement

```powershell
.inelease
et10.0in-x64elease
et10.0-windowsin-x64elease
et10.0in-x64elease
et10.0-windowsinelease
et10.0in-x64elease
```

Double-cliquez sur `Voyl.exe` ou exécutez-le depuis l'invite de commandes.
