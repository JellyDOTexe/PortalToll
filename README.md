# Portal Toll

A Valheim mod. Portals let you carry metal through, but the portal destroys half of it on every trip.

## What it does

- In the unmodded game a portal refuses you while you carry ore, metal bars, scrap or other restricted items. With this mod the portal lets you through.
- On each trip the portal destroys half of every restricted item type you carry. The count is taken across your whole inventory and the loss is rounded up: 20 iron leaves 10, 5 iron leaves 2, and a single bar is lost.
- A message on screen tells you what was taken, for example "The portal consumed 10 Iron, 3 Copper ore".
- "Restricted" means every item the game marks as not teleportable. This covers dragon eggs and similar items, and metals added by other mods, as long as those mods mark them the same way.

## Settings

The config file is `BepInEx/config/jellydotexe.portaltoll.cfg`. It is created the first time the game starts with the mod.

| Setting | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Turns the mod on. When off, portals behave as in the unmodded game. |
| `LossFraction` | `0.5` | Share of each restricted item type destroyed per trip, from 0 to 1. |
| `TollFreePortals` | `true` | Also charge the toll where the unmodded game already lets metal through: stone portals, and worlds with the "teleport all" modifier. Set to `false` to keep those free. |

## Install

Needs [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

1. Download `PortalToll-1.0.0.zip` from the [Releases](https://github.com/JellyDOTexe/PortalToll/releases) page.
2. Copy `PortalToll.dll` into `BepInEx/plugins`. Mod managers such as r2modman can import the zip as a local mod.

## Multiplayer

The mod runs on each player's game, not on the server. Every player who should pay the toll needs it installed. A player without it is blocked by portals as usual.

## Limits

- Only your own inventory is counted. Items inside containers added by other mods, such as backpacks, are not.
- Other mods that change the portal metal rule may conflict.
- Version 1.0.0 was compiled and checked against Valheim's code, but not yet tested in a running game. Please open an issue if something is off.

## Build from source

```
cd src
dotnet build -c Release -p:ValheimDir=/path/to/Valheim
```

`ValheimDir` is your Valheim install folder with BepInEx in it.
