using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
        private const string InscriptionPrefix = "N";
        private const string FeaturedTag = "FW";
        private const string PoseTag = "PS";

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

        // Converts all displays to text lines.
        // Items: settlementId|A0 or W0|itemId|modifierId. Inscriptions: settlementId|NA or NW0|base64 text|.
        // Featured weapon: settlementId|FW|weapon slot index|. Mannequin stance: settlementId|PS|animation id|.
        public List<string> Serialize()
        {
            List<string> entries = new List<string>(_unresolvedEntries);

            foreach (KeyValuePair<string, TrophyDisplay> pair in _displays)
            {
                AddEntries(entries, pair.Key, ArmourPrefix, pair.Value.Armour);
                AddEntries(entries, pair.Key, WeaponPrefix, pair.Value.Weapons);
                AddInscription(entries, pair.Key, InscriptionPrefix + ArmourPrefix, pair.Value.ArmourInscription);

                for (int i = 0; i < pair.Value.WeaponInscriptions.Length; i++)
                {
                    AddInscription(entries, pair.Key, InscriptionPrefix + WeaponPrefix + i.ToString(CultureInfo.InvariantCulture), pair.Value.WeaponInscriptions[i]);
                }

                if (!string.IsNullOrEmpty(pair.Value.Pose))
                {
                    entries.Add(string.Join(Separator.ToString(), pair.Key, PoseTag, pair.Value.Pose, string.Empty));
                }

                if (pair.Value.FeaturedWeaponIndex != TrophyDisplay.NoFeaturedWeapon)
                {
                    entries.Add(string.Join(Separator.ToString(), pair.Key, FeaturedTag, pair.Value.FeaturedWeaponIndex.ToString(CultureInfo.InvariantCulture), string.Empty));
                }
            }

            return entries;
        }

        // Inscriptions are Base64-encoded so any characters the player types are stored safely.
        private static void AddInscription(List<string> entries, string settlementId, string slot, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            entries.Add(string.Join(Separator.ToString(), settlementId, slot, encoded, string.Empty));
        }

        private static string DecodeInscription(string encoded)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        // Applies a saved inscription entry (slot "NA" or "NW0".."NW9") to its display.
        private void ApplyInscription(string settlementId, string slot, string encoded)
        {
            string text = DecodeInscription(encoded);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            TrophyDisplay display = GetOrCreate(settlementId);
            string target = slot.Substring(InscriptionPrefix.Length);

            if (target == ArmourPrefix)
            {
                display.ArmourInscription = text;
            }
            else if (target.StartsWith(WeaponPrefix)
                && int.TryParse(target.Substring(WeaponPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                && index >= 0 && index < display.WeaponInscriptions.Length)
            {
                display.WeaponInscriptions[index] = text;
            }
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

                if (parts[1] == PoseTag)
                {
                    if (TrophyPoses.IndexOf(parts[2]) > 0)
                    {
                        GetOrCreate(parts[0]).Pose = parts[2];
                    }
                    continue;
                }

                if (parts[1] == FeaturedTag)
                {
                    if (int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int featured)
                        && featured >= 0 && featured < TrophyDisplay.WeaponSlotCount)
                    {
                        GetOrCreate(parts[0]).FeaturedWeaponIndex = featured;
                    }
                    continue;
                }

                if (parts[1].StartsWith(InscriptionPrefix))
                {
                    ApplyInscription(parts[0], parts[1], parts[2]);
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
