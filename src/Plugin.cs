using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PortalToll
{
    // Portals normally refuse a player who carries metal (any item the game marks as not teleportable).
    // This plugin lets the player through and has the portal destroy a share of those items instead.
    [BepInPlugin(Guid, "Portal Toll", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jellydotexe.portaltoll";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> LossFraction;
        internal static ConfigEntry<bool> TollFreePortals;

        // The portal whose Teleport() call is running right now, if any.
        internal static TeleportWorld ActivePortal;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Let metal through portals and charge the toll. When off, portals behave as in the unmodded game.");
            LossFraction = Config.Bind("General", "LossFraction", 0.5f,
                new ConfigDescription("Share of each restricted item type destroyed per portal trip. The loss is rounded up, so a single item is always lost.",
                    new AcceptableValueRange<float>(0f, 1f)));
            TollFreePortals = Config.Bind("General", "TollFreePortals", true,
                "Also charge the toll where the unmodded game already lets metal through for free: " +
                "stone portals and worlds with the 'teleport all' modifier. When off, those stay free.");
            new Harmony(Guid).PatchAll(typeof(Patches));
        }

        internal static bool VanillaAllowsAll(TeleportWorld portal)
        {
            if (portal != null && portal.m_allowAllItems) return true;
            return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll);
        }

        internal static int LossFor(int total, float fraction)
        {
            if (total <= 0 || fraction <= 0f) return 0;
            // The small offset keeps float error from turning an exact half into one extra item.
            return Mathf.Clamp(Mathf.CeilToInt(total * fraction - 0.0001f), 0, total);
        }

        internal static void ChargeToll(Player player, TeleportWorld portal)
        {
            if (!TollFreePortals.Value && VanillaAllowsAll(portal)) return;

            Inventory inventory = player.GetInventory();
            var totals = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.m_shared.m_teleportable) continue;
                string name = item.m_shared.m_name;
                if (!totals.ContainsKey(name))
                {
                    totals[name] = 0;
                    order.Add(name);
                }
                totals[name] += item.m_stack;
            }
            if (order.Count == 0) return;

            var lost = new List<string>();
            foreach (string name in order)
            {
                int toRemove = LossFor(totals[name], LossFraction.Value);
                if (toRemove <= 0) continue;
                int removed = 0;
                // Copy the list: removing a whole stack changes the inventory while we walk it.
                foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
                {
                    if (removed >= toRemove) break;
                    if (item.m_shared.m_teleportable || item.m_shared.m_name != name) continue;
                    int take = Math.Min(item.m_stack, toRemove - removed);
                    if (take >= item.m_stack && item.m_equipped) player.UnequipItem(item, false);
                    if (inventory.RemoveItem(item, take)) removed += take;
                }
                if (removed > 0) lost.Add(removed + " " + Localization.instance.Localize(name));
            }
            if (lost.Count == 0) return;

            string text = "The portal consumed " + string.Join(", ", lost);
            player.Message(MessageHud.MessageType.Center, text);
            Log.LogInfo(text);
        }
    }

    internal static class Patches
    {
        // Lift the "you carry metal" block. Items the game never lets through (tool tier 1000+) stay blocked.
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
        [HarmonyPostfix]
        private static void AllowRestrictedItems(Inventory __instance, ref bool __result)
        {
            if (__result || !Plugin.Enabled.Value) return;
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
