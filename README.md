# Portal Toll

Lets you take metal through portals, but the portal eats half of it.

I always thought the "no metal through portals" rule was a bit too all-or-nothing. With this you can skip the boat trip if you're willing to pay for it. Walk in with 20 iron, walk out with 10.

**Heads up:** this mod was made with Claude Code, Anthropic's AI coding tool.

## How it works

- Portals no longer refuse you when you're carrying ore, bars, scrap and so on.
- Every trip costs you half of each of those items. It counts your whole inventory, not each stack, and rounds against you: 5 iron becomes 2, and a single bar is just gone. So no, you can't sneak them through one at a time.
- You get a message telling you what you lost.
- It works on anything the game flags as non-teleportable, so dragon eggs count, and so do metals from other mods as long as they use the same flag.

Everything above is the default. Pretty much all of it can be changed.

## Config

`BepInEx/config/jellydotexe.portaltoll.cfg`, created on first launch. Changes apply on your next portal trip.

General

- `Enabled` - turn the whole mod on or off.
- `LiftPortalBlock` - if you turn this off, portals block metal like normal and the toll only applies where the game already lets things through (see below).

Toll

- `LossFraction` (0.5) - how much gets destroyed. 0.25 is a quarter, 1 is everything.
- `Rounding` (Up) - what to do with uneven numbers. `Up`, `Down`, `Nearest`, or `Random`. Random means half of 5 costs you 2 or 3 by coin flip.
- `FreeAllowance` (0) - how many of each item you can carry for free before the toll kicks in.
- `ItemOverrides` - custom rates for specific items, like `IronScrap:0.25, DragonEgg:0, BlackMetal:1`. Uses prefab names. You can also list items that normally teleport fine if you want them taxed too.

Stone portals and "teleport all" worlds

The game already lets metal through in these cases. By default the toll still applies.

- `TollStonePortals` - turn off to make stone portals free. This wins over the per portal setting below.
- `TollTeleportAllWorlds` - turn off to make worlds with the teleport all modifier free.

Message

- `Message` - `Center`, `TopLeft` or `Off`.
- `MessageText` - the text itself. `{0}` is where the item list goes.

Per item

After you've loaded into the game once, the config gets a `5 - Items` section with an entry for every restricted item (`Iron`, `CopperOre`, `DragonEgg`...), including ones from other mods. -1 means "use LossFraction", 0 means free, anything else is that item's own rate. These win over `ItemOverrides`.

Per portal

You also get a `6 - Portals` section with an entry for every kind of portal in the game (`portal_wood`, `portal_stone`, plus any that other mods add; the game also has an older one just called `portal`). Each one is a multiplier on the toll at that kind of portal. 1 is the normal toll, 0.5 is half, 0 makes that portal free, 2 is double. It stacks with the item rates, so if iron is set to 0.5 and a portal is set to 0.5 you lose a quarter of your iron there. You can never lose more than everything.

This is per kind of portal, not per portal you've placed. It only sees modded portals that are built on the game's own portal, which most are.

## Install

Use a mod manager, or drop `PortalToll.dll` into `BepInEx/plugins`. Needs BepInEx, nothing else.

## Multiplayer

It's client side. Anyone who has it pays the toll, anyone who doesn't gets blocked by portals like normal. The server doesn't need it and can't force settings on players.

## Known issues

- Only looks at your own inventory. Stuff inside modded backpacks isn't counted.
- Will probably fight with other mods that change the portal metal rule.
- This is a new mod and hasn't had much testing yet. If something's broken, open an issue on [GitHub](https://github.com/JellyDOTexe/PortalToll/issues).

## Source

[github.com/JellyDOTexe/PortalToll](https://github.com/JellyDOTexe/PortalToll), MIT licensed. To build: `dotnet build -c Release -p:ValheimDir=/path/to/Valheim` in `src`.
