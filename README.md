f```
  ███████╗██╗  ██╗██╗   ██╗████████╗████████╗███████╗██████╗
  ██╔════╝██║  ██║██║   ██║╚══██╔══╝╚══██╔══╝██╔════╝██╔══██╗
  ███████╗███████║██║   ██║   ██║      ██║   █████╗  ██████╔╝
  ╚════██║██╔══██║██║   ██║   ██║      ██║   ██╔══╝  ██╔══██╗
  ███████║██║  ██║╚██████╔╝   ██║      ██║   ███████╗██║  ██║
  ╚══════╝╚═╝  ╚═╝ ╚═════╝    ╚═╝      ╚═╝   ╚══════╝╚═╝  ╚═╝
  Made by WAM-Sofware (c) since 1997.
```

**Shutter** is een kleine Windows GUI om een **shutdown** of **restart** te plannen (lokaal of remote) op basis van een datum/tijd uit een kalender. De app berekent de `/t` seconden en toont die live in de GUI.

## Features
- Kies **Shutdown** of **Restart**
- Kies datum via **kalender** + tijd (HH:mm:ss)
- Toont **seconden tot actie** + **aftel mechanisme** (live countdown)
- Optioneel doelserver: leeg = lokaal, of computernaam/IP voor remote (`/m \\\\SERVER`)
- **Stop/Annuleer** via `shutdown /a`
- Optioneel **force** (`/f`)
- **Systeemvak icoon (tray)** met menu: Open/Verberg, Start, Stop, Afsluiten

## Gebruik
1) Start `dist/Shutter.exe`
2) Vul optioneel een servernaam/IP in (leeg = lokaal)
3) Kies datum in de kalender + tijd
4) Klik **Start** en bevestig
5) Stop/annuleer indien nodig met **Stop** (of via het tray menu)

> Remote shutdown/restart vereist rechten op de doelserver en dat remote shutdown toegestaan is in de Windows configuratie.

## Build
Vereist: .NET Framework 4.x (csc.exe aanwezig op Windows).

```powershell
./build.ps1
```

Output: `dist/Shutter.exe`

## License
MIT
