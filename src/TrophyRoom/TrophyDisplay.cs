using TaleWorlds.Core;

namespace TrophyRoom
{
    // One fief's display: an armour set for the mannequin and a wall of weapon slots.
    public class TrophyDisplay
    {
        public const int WeaponSlotCount = 10;

        // Equipment slots used by the mannequin, in display order.
        public static readonly EquipmentIndex[] ArmourSlots =
        {
            EquipmentIndex.Head,
            EquipmentIndex.Cape,
            EquipmentIndex.Body,
            EquipmentIndex.Gloves,
            EquipmentIndex.Leg
        };

        public EquipmentElement[] Armour { get; } = new EquipmentElement[ArmourSlots.Length];

        public EquipmentElement[] Weapons { get; } = new EquipmentElement[WeaponSlotCount];

        // Copies every slot from another display.
        public void CopyFrom(TrophyDisplay other)
        {
            other.Armour.CopyTo(Armour, 0);
            other.Weapons.CopyTo(Weapons, 0);
        }

        // Builds an equipment set containing only the displayed armour, for the mannequin.
        public Equipment CreateMannequinEquipment()
        {
            Equipment equipment = new Equipment(Equipment.EquipmentType.Battle);
            for (int i = 0; i < ArmourSlots.Length; i++)
            {
                equipment[ArmourSlots[i]] = Armour[i];
            }
            return equipment;
        }
    }
}
