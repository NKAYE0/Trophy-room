using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TrophyRoom.UI
{
    // An item the player could put on display, plus where it came from.
    public class TrophyCandidate
    {
        public TrophyCandidate(EquipmentElement element, TextObject source)
        {
            Element = element;
            Source = source;
        }

        public EquipmentElement Element { get; }

        public TextObject Source { get; }
    }

    // Collects displayable items from the party inventory and the gear of clan heroes in the party.
    public static class TrophyItemSources
    {
        public static List<TrophyCandidate> Collect()
        {
            List<TrophyCandidate> result = new List<TrophyCandidate>();
            HashSet<string> seen = new HashSet<string>();
            MobileParty party = MobileParty.MainParty;
            if (party == null)
            {
                return result;
            }

            TextObject inventoryText = new TextObject("{=trophyroom_source_inventory}Inventory");
            foreach (ItemRosterElement rosterElement in party.ItemRoster)
            {
                if (rosterElement.Amount > 0)
                {
                    TryAdd(result, seen, rosterElement.EquipmentElement, inventoryText);
                }
            }

            foreach (TroopRosterElement troop in party.MemberRoster.GetTroopRoster())
            {
                Hero hero = troop.Character?.HeroObject;
                if (hero == null || hero.Clan != Clan.PlayerClan)
                {
                    continue;
                }

                AddEquipment(result, seen, hero.BattleEquipment, hero.Name);
                AddEquipment(result, seen, hero.CivilianEquipment, hero.Name);
            }

            result.Sort((a, b) => b.Element.ItemValue.CompareTo(a.Element.ItemValue));
            return result;
        }

        private static void AddEquipment(List<TrophyCandidate> result, HashSet<string> seen, Equipment equipment, TextObject source)
        {
            if (equipment == null)
            {
                return;
            }

            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                TryAdd(result, seen, equipment[i], source);
            }
        }

        // Adds the item once per item/quality combination, skipping anything that can't be displayed.
        private static void TryAdd(List<TrophyCandidate> result, HashSet<string> seen, EquipmentElement element, TextObject source)
        {
            ItemObject item = element.Item;
            if (item == null || (!IsDisplayWeapon(item) && GetArmourSlot(item) == EquipmentIndex.None))
            {
                return;
            }

            string key = item.StringId + "|" + (element.ItemModifier?.StringId ?? string.Empty);
            if (seen.Add(key))
            {
                result.Add(new TrophyCandidate(new EquipmentElement(item, element.ItemModifier), source));
            }
        }

        // True for weapons and shields that can go on the weapon wall (ammunition and banners excluded).
        public static bool IsDisplayWeapon(ItemObject item)
        {
            switch (item.ItemType)
            {
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                case ItemObject.ItemTypeEnum.Polearm:
                case ItemObject.ItemTypeEnum.Bow:
                case ItemObject.ItemTypeEnum.Crossbow:
                case ItemObject.ItemTypeEnum.Sling:
                case ItemObject.ItemTypeEnum.Thrown:
                case ItemObject.ItemTypeEnum.Pistol:
                case ItemObject.ItemTypeEnum.Musket:
                case ItemObject.ItemTypeEnum.Shield:
                    return true;
                default:
                    return false;
            }
        }

        // Returns which mannequin slot an armour piece belongs in, or None if it isn't armour.
        public static EquipmentIndex GetArmourSlot(ItemObject item)
        {
            switch (item.ItemType)
            {
                case ItemObject.ItemTypeEnum.HeadArmor:
                    return EquipmentIndex.Head;
                case ItemObject.ItemTypeEnum.Cape:
                    return EquipmentIndex.Cape;
                case ItemObject.ItemTypeEnum.BodyArmor:
                    return EquipmentIndex.Body;
                case ItemObject.ItemTypeEnum.HandArmor:
                    return EquipmentIndex.Gloves;
                case ItemObject.ItemTypeEnum.LegArmor:
                    return EquipmentIndex.Leg;
                default:
                    return EquipmentIndex.None;
            }
        }
    }
}
