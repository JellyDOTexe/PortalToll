# Portal Toll

A Valheim mod. Portals let you carry metal through, but the portal destroys half of it on every trip.

## What it does

- In the unmodded game a portal refuses you while you carry ore, metal bars, scrap or other restricted items. With this mod the portal lets you through.
- With the default settings, each trip destroys half of every restricted item type you carry. The count is taken across your whole inventory and the loss is rounded up: 20 iron leaves 10, 5 iron leaves 2, and a single bar is lost.
- A message on screen tells you what was taken, for example "The portal consumed 10 Iron, 3 Copper ore".
- "Restricted" means every item the game marks as not teleportable. This covers dragon eggs and similar items, and metals added by other mods, as long as those mods mark them the same way.

## Settings

The config file is `BepInEx/config/jellydotexe.portaltoll.cfg`. It is created the first time the game starts with the mod. Changes apply to the next portal trip; no restart is needed if you edit them with an in-game config manager.

**General**

| Setting | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Master switch. When off, portals behave as in the unmodded game. |
| `LiftPortalBlock` | `true` | Lets players carrying restricted items use portals. When off, portals still refuse them, and the toll only applies where the game lets items through. |

**Toll**

| Setting | Default | Meaning |
| --- | --- | --- |
| `LossFraction` | `0.5` | Share of each restricted item type destroyed per trip. 0 is nothing, 0.5 is half, 1 is everything. |
| `Rounding` | `Up` | What happens when the loss is not a whole number, such as half of 5. `Up` loses 3, `Down` loses 2, `Nearest` picks the closest whole number, and `Random` loses 2 or 3 by chance so the average matches `LossFraction`. |
| `FreeAllowance` | `0` | This many items of each type pass untouched. The toll is taken only from the rest. |
| `ItemOverrides` | empty | Loss fraction for single items, as `Name:Fraction` pairs separated by commas. Example: `IronScrap:0.25, DragonEgg:0, BlackMetal:1`. Use prefab names, the ones the spawn command uses. A fraction of 0 makes an item toll free. Items the game normally lets through can be listed too, and are then tolled. |

**Items**

Once the game has loaded its item list, the config file gains a `5 - Items` section with one setting per restricted item, named by prefab (`Iron`, `CopperOre`, `DragonEgg` and so on). Restricted items from other mods get one too. Start the game once to see them.

| Value | Meaning |
| --- | --- |
| `-1` (default) | Use `LossFraction`. |
| `0` | This item is toll free. |
| above `0` up to `1` | Share of this item destroyed per trip. |

A value set here wins over `ItemOverrides`. Use `ItemOverrides` for items the game normally lets through, which have no setting of their own.

**Free portals**

| Setting | Default | Meaning |
| --- | --- | --- |
| `TollStonePortals` | `true` | Charge the toll at stone portals, which let everything through in the unmodded game. |
| `TollTeleportAllWorlds` | `true` | Charge the toll in worlds with the "teleport all" modifier. |

**Message**

| Setting | Default | Meaning |
| --- | --- | --- |
| `Message` | `Center` | Where the message appears: `Center`, `TopLeft`, or `Off`. |
| `MessageText` | `The portal consumed {0}` | The message text. `{0}` is replaced by the list of lost items. |

## Install

Needs [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

1. Download `PortalToll-1.2.0.zip` from the [Releases](https://github.com/JellyDOTexe/PortalToll/releases) page.
2. Copy `PortalToll.dll` into `BepInEx/plugins`. Mod managers such as r2modman can import the zip as a local mod.

## Multiplayer

The mod runs on each player's game, not on the server. Every player who should pay the toll needs it installed. A player without it is blocked by portals as usual.

## Limits

- Only your own inventory is counted. Items inside containers added by other mods, such as backpacks, are not.
- Other mods that change the portal metal rule may conflict.
- The mod was compiled and checked against Valheim's code, but not yet tested in a running game. Please open an issue if something is off.

## Build from source

```
cd src
dotnet build -c Release -p:ValheimDir=/path/to/Valheim
```

`ValheimDir` is your Valheim install folder with BepInEx in it.

## License

MIT. Use it, change it and share it freely. See [LICENSE](LICENSE).
