```
  ███████╗██╗  ██╗██╗   ██╗████████╗████████╗███████╗██████╗
  ██╔════╝██║  ██║██║   ██║╚══██╔══╝╚══██╔══╝██╔════╝██╔══██╗
  ███████╗███████║██║   ██║   ██║      ██║   █████╗  ██████╔╝
  ╚════██║██╔══██║██║   ██║   ██║      ██║   ██╔══╝  ██╔══██╗
  ███████║██║  ██║╚██████╔╝   ██║      ██║   ███████╗██║  ██║
  ╚══════╝╚═╝  ╚═╝ ╚═════╝    ╚═╝      ╚═╝   ╚══════╝╚═╝  ╚═╝
  Idea by WAM-Software, vibed coded by Codex.
```

**Shutter** is een kleine Windows GUI om lokaal een **shutdown** of **restart** te plannen op basis van een datum/tijd uit een moderne kalender. De app berekent de `/t` seconden en toont die live in de GUI.

## Features
- Kies **Shutdown** of **Restart**
- Kies datum via de custom **moderne kalender** + tijd (HH:mm:ss)
- Toont **seconden tot actie** + ronde live countdown
- **Stop/Annuleer** via `shutdown /a`
- **Force** staat standaard aan (`/f`)
- Controleert bij opstarten of Windows al een shutdown/restart lijkt te hebben ingepland
- **Systeemvak icoon (tray)** blijft alleen resident wanneer er een schedule loopt
- **Over... / About box** met versie + info

## Gebruik
1) Start `dist/Shutter.exe`
2) Kies datum in de kalender + tijd
3) Standaard staat de planning op **vandaag 22:00**
4) Klik **Schedule** en bevestig
5) Stop/annuleer indien nodig met **Stop** (of via het tray menu als er een schedule loopt)

## Build
Vereist: .NET Framework 4.x (csc.exe aanwezig op Windows).

```powershell
./build.ps1
```

Output: `dist/Shutter.exe`

## License
MIT
