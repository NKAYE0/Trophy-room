using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TrophyRoom
{
    // Holds each fief's trophy display and converts it to and from plain text for saving.
    public class TrophyStorage
    {
        private const char Separator = '|';
        private const string ArmourPrefix = "A";
        private const string WeaponPrefix = "W";

        private readonly Dictionary<string, TrophyDisplay> _displays = new Dictionary<string, TrophyDisplay>();

        // Saved entries whose item could not be found (e.g. an item mod was removed); kept so they are not lost.
        private readonly List<string> _unresolvedEntries = new List<string>();

        // Returns the display for a settlement, creating an empty one if needed.
        public TrophyDisplay GetOrCreate(string settlementId)
        {
            if (!_displays.TryGetValue(settlementId, out TrophyDisplay display))
            {
                display = new TrophyDisplay();
                _displays[settlementId] = display;
            }
            return display;
        }

        // Deletes a settlement's display, including any entries that could not be loaded.
        public void Remove(string settlementId)
        {
            _displays.Remove(settlementId);
            string prefix = settlementId + Separator;
            _unresolvedEntries.RemoveAll(entry => entry.StartsWith(prefix));
        }

        // Converts all displays to text lines in the form: settlementId|slot|itemId|modifierId.
        public List<string> Serialize()
        {
            List<string> entries = new List<string>(_unresolvedEntries);

            foreach (KeyValuePair<string, TrophyDisplay> pair in _displays)
            {
                AddEntries(entries, pair.Key, ArmourPrefix, pair.Value.Armour);
                AddEntries(entries, pair.Key, WeaponPrefix, pair.Value.Weapons);
            }

            return entries;
        }

        private static void AddEntries(List<string> entries, string settlementId, string prefix, EquipmentElement[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                ItemObject item = slots[i].Item;
                if (item == null)
                {
                    continue;
                }

                string slot = prefix + i.ToString(CultureInfo.InvariantCulture);
                string modifierId = slots[i].ItemModifier?.StringId ?? string.Empty;
                entries.Add(string.Join(Separator.ToString(), settlementId, slot, item.StringId, modifierId));
            }
        }

        // Rebuilds the displays from saved text lines.
        public void Deserialize(List<string> entries)
        {
            _displays.Clear();
            _unresolvedEntries.Clear();

            if (entries == null)
            {
                return;
            }

            foreach (string entry in entries)
            {
                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                string[] parts = entry.Split(Separator);
                if (parts.Length != 4 || parts[1].Length < 2)
                {
                    continue;
                }

                string prefix = parts[1].Substring(0, 1);
                if (!int.TryParse(parts[1].Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                {
                    continue;
                }

                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(parts[2]);
                if (item == null)
                {
                    _unresolvedEntries.Add(entry);
                    continue;
                }

                ItemModifier modifier = string.IsNullOrEmpty(parts[3]) ? null : MBObjectManager.Instance.GetObject<ItemModifier>(parts[3]);
                EquipmentElement element = new EquipmentElement(item, modifier);
                TrophyDisplay display = GetOrCreate(parts[0]);

                if (prefix == ArmourPrefix && index >= 0 && index < display.Armour.Length)
                {
                    display.Armour[index] = element;
                }
                else if (prefix == WeaponPrefix && index >= 0 && index < display.Weapons.Length)
                {
                    display.Weapons[index] = element;
                }
            }
        }
    }
}
