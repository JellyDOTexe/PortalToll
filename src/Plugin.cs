using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PortalToll
{
    public enum Rounding { Up, Down, Nearest, Random }
    public enum MessageMode { Center, TopLeft, Off }

    // Portals normally refuse a player who carries metal (any item the game marks as not teleportable).
    // This plugin lets the player through and has the portal destroy a share of those items instead.
    [BepInPlugin(Guid, "Portal Toll", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jellydotexe.portaltoll";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> LiftPortalBlock;
        internal static ConfigEntry<float> LossFraction;
        internal static ConfigEntry<Rounding> RoundingMode;
        internal static ConfigEntry<int> FreeAllowance;
        internal static ConfigEntry<string> ItemOverrides;
        internal static ConfigEntry<bool> TollStonePortals;
        internal static ConfigEntry<bool> TollTeleportAllWorlds;
        internal static ConfigEntry<MessageMode> Message;
        internal static ConfigEntry<string> MessageText;

        // The portal whose Teleport() call is running right now, if any.
        internal static TeleportWorld ActivePortal;

        // Parsed ItemOverrides: item name (lower case) -> loss fraction.
        private static Dictionary<string, float> overrides = new Dictionary<string, float>();

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("1 - General", "Enabled", true,
                "Master switch. When off, portals behave as in the unmodded game.");
            LiftPortalBlock = Config.Bind("1 - General", "LiftPortalBlock", true,
                "Let players carrying restricted items (metal etc.) use portals. When off, portals still refuse them " +
                "as in the unmodded game, and the toll only applies where the game lets items through.");

            LossFraction = Config.Bind("2 - Toll", "LossFraction", 0.5f,
                new ConfigDescription("Share of each restricted item type destroyed per portal trip. 0 = nothing, 0.5 = half, 1 = everything.",
                    new AcceptableValueRange<float>(0f, 1f)));
            RoundingMode = Config.Bind("2 - Toll", "Rounding", Rounding.Up,
                "What to do when the loss is not a whole number (for example half of 5). " +
                "Up: lose 3. Down: lose 2. Nearest: closest whole number, halves go up. " +
                "Random: lose 2 or 3 by chance, so the average matches LossFraction exactly.");
            FreeAllowance = Config.Bind("2 - Toll", "FreeAllowance", 0,
                new ConfigDescription("This many items of each type pass untouched; the toll is taken only from the rest. 0 = toll everything.",
                    new AcceptableValueRange<int>(0, 9999)));
            ItemOverrides = Config.Bind("2 - Toll", "ItemOverrides", "",
                "Loss fraction for single items, as a comma separated list of Name:Fraction. " +
                "Example: IronScrap:0.25, DragonEgg:0, BlackMetal:1. Use prefab names (the names the spawn command uses). " +
                "0 makes an item toll free. Items the game normally lets through can be listed too, and are then tolled.");

            TollStonePortals = Config.Bind("3 - Free portals", "TollStonePortals", true,
                "Charge the toll at stone portals, which let everything through in the unmodded game.");
            TollTeleportAllWorlds = Config.Bind("3 - Free portals", "TollTeleportAllWorlds", true,
                "Charge the toll in worlds with the 'teleport all' modifier, where the unmodded game lets everything through.");

            Message = Config.Bind("4 - Message", "Message", MessageMode.Center,
                "Where to show what the portal took. Center: large text mid screen. TopLeft: small text top left. Off: no message.");
            MessageText = Config.Bind("4 - Message", "MessageText", "The portal consumed {0}",
                "Text of the message. {0} is replaced by the list of lost items.");

            ParseOverrides();
            ItemOverrides.SettingChanged += (_, __) => ParseOverrides();
            new Harmony(Guid).PatchAll(typeof(Patches));
        }

        private static void ParseOverrides()
        {
            var parsed = new Dictionary<string, float>();
            foreach (string entry in ItemOverrides.Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(':');
                float fraction;
                if (parts.Length != 2 || parts[0].Trim().Length == 0 ||
                    !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out fraction))
                {
                    Log.LogWarning("ItemOverrides: skipped '" + entry.Trim() + "', expected Name:Fraction such as IronScrap:0.25");
                    continue;
                }
                parsed[parts[0].Trim().ToLowerInvariant()] = Mathf.Clamp01(fraction);
            }
            overrides = parsed;
        }

        // Loss fraction for this item, or 0 when it is not tolled.
        internal static float FractionFor(ItemDrop.ItemData item)
        {
            float fraction;
            if (overrides.Count > 0)
            {
                if (item.m_dropPrefab != null && overrides.TryGetValue(item.m_dropPrefab.name.ToLowerInvariant(), out fraction)) return fraction;
                if (overrides.TryGetValue(item.m_shared.m_name.ToLowerInvariant(), out fraction)) return fraction;
            }
            return item.m_shared.m_teleportable ? 0f : LossFraction.Value;
        }

        internal static int LossFor(int total, float fraction)
        {
            int taxable = total - FreeAllowance.Value;
            if (taxable <= 0 || fraction <= 0f) return 0;
            float exact = taxable * fraction;
            int loss;
            switch (RoundingMode.Value)
            {
                case Rounding.Down: loss = Mathf.FloorToInt(exact + 0.0001f); break;
                case Rounding.Nearest: loss = Mathf.FloorToInt(exact + 0.5001f); break;
                case Rounding.Random:
                    loss = Mathf.FloorToInt(exact + 0.0001f);
                    if (UnityEngine.Random.value < exact - loss) loss++;
                    break;
                // The small offset keeps float error from turning an exact half into one extra item.
                default: loss = Mathf.CeilToInt(exact - 0.0001f); break;
            }
            return Mathf.Clamp(loss, 0, taxable);
        }

        internal static void ChargeToll(Player player, TeleportWorld portal)
        {
            if (!TollStonePortals.Value && portal != null && portal.m_allowAllItems) return;
            if (!TollTeleportAllWorlds.Value && ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll)) return;

            Inventory inventory = player.GetInventory();
            var totals = new Dictionary<string, int>();
            var fractions = new Dictionary<string, float>();
            var order = new List<string>();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                float fraction = FractionFor(item);
                if (fraction <= 0f) continue;
                string name = item.m_shared.m_name;
                if (!totals.ContainsKey(name))
                {
                    totals[name] = 0;
                    fractions[name] = fraction;
                    order.Add(name);
                }
                totals[name] += item.m_stack;
            }
            if (order.Count == 0) return;

            var lost = new List<string>();
            foreach (string name in order)
            {
                int toRemove = LossFor(totals[name], fractions[name]);
                if (toRemove <= 0) continue;
                int removed = 0;
                // Copy the list: removing a whole stack changes the inventory while we walk it.
                foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
                {
                    if (removed >= toRemove) break;
                    if (item.m_shared.m_name != name || FractionFor(item) <= 0f) continue;
                    int take = Math.Min(item.m_stack, toRemove - removed);
                    if (take >= item.m_stack && item.m_equipped) player.UnequipItem(item, false);
                    if (inventory.RemoveItem(item, take)) removed += take;
                }
                if (removed > 0) lost.Add(removed + " " + Localization.instance.Localize(name));
            }
            if (lost.Count == 0) return;

            string list = string.Join(", ", lost);
            string text = MessageText.Value.Contains("{0}") ? MessageText.Value.Replace("{0}", list) : MessageText.Value + " " + list;
            Log.LogInfo(text);
            if (Message.Value == MessageMode.Off) return;
            player.Message(Message.Value == MessageMode.TopLeft ? MessageHud.MessageType.TopLeft : MessageHud.MessageType.Center, text);
        }
    }

    internal static class Patches
    {
        // Lift the "you carry metal" block. Items the game never lets through (tool tier 1000+) stay blocked.
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
        [HarmonyPostfix]
        private static void AllowRestrictedItems(Inventory __instance, ref bool __result)
        {
            if (__result || !Plugin.Enabled.Value || !Plugin.LiftPortalBlock.Value) return;
            foreach (ItemDrop.ItemData item in __instance.GetAllItems())
            {
                if (item.m_shared.m_toolTier >= 1000) return;
            }
            __result = true;
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
        [HarmonyPrefix]
        private static void BeforePortalTeleport(TeleportWorld __instance)
        {
            Plugin.ActivePortal = __instance;
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
        [HarmonyFinalizer]
        private static void AfterPortalTeleport()
        {
            Plugin.ActivePortal = null;
        }

        // Charge only when a portal actually started the teleport (TeleportTo returns false on cooldown etc.).
        [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
        [HarmonyPostfix]
        private static void AfterTeleportTo(Player __instance, bool __result)
        {
            if (!__result || Plugin.ActivePortal == null || !Plugin.Enabled.Value) return;
            if (__instance != Player.m_localPlayer) return;
            try
            {
                Plugin.ChargeToll(__instance, Plugin.ActivePortal);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Toll failed: " + e);
            }
        }
    }
}
