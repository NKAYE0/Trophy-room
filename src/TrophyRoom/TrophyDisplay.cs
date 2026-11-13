using TaleWorlds.Core;

namespace TrophyRoom
{
    // One fief's display: an armour set for the mannequin, a wall of weapon slots and an optional featured weapon.
    public class TrophyDisplay
    {
        public const int WeaponSlotCount = 10;
        public const int NoFeaturedWeapon = -1;

        // Equipment slot the featured weapon is placed in on the mannequin.
        public const EquipmentIndex FeaturedWeaponSlot = EquipmentIndex.Weapon0;

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

        // Player-written inscription shared by the whole armour set.
        public string ArmourInscription { get; set; } = string.Empty;

        // Player-written inscription for each weapon slot.
        public string[] WeaponInscriptions { get; } = CreateEmptyInscriptions();

        // Weapon slot held by the mannequin, or NoFeaturedWeapon.
        public int FeaturedWeaponIndex { get; set; } = NoFeaturedWeapon;

        // Mannequin stance chosen by the player (animation id), or null for automatic.
        public string Pose { get; set; }

        private static string[] CreateEmptyInscriptions()
        {
            string[] inscriptions = new string[WeaponSlotCount];
            for (int i = 0; i < inscriptions.Length; i++)
            {
                inscriptions[i] = string.Empty;
            }
            return inscriptions;
        }

        // The featured weapon, or an empty element if none is chosen.
        public EquipmentElement FeaturedWeapon =>
            FeaturedWeaponIndex >= 0 && FeaturedWeaponIndex < Weapons.Length ? Weapons[FeaturedWeaponIndex] : EquipmentElement.Invalid;

        // Copies every slot, inscription, the featured weapon and the stance from another display.
        public void CopyFrom(TrophyDisplay other)
        {
            other.Armour.CopyTo(Armour, 0);
            other.Weapons.CopyTo(Weapons, 0);
            other.WeaponInscriptions.CopyTo(WeaponInscriptions, 0);
            ArmourInscription = other.ArmourInscription;
            FeaturedWeaponIndex = other.FeaturedWeaponIndex;
            Pose = other.Pose;
        }

        // Builds the mannequin's equipment: the displayed armour plus the featured weapon.
        public Equipment CreateMannequinEquipment()
        {
            Equipment equipment = new Equipment(Equipment.EquipmentType.Battle);
            for (int i = 0; i < ArmourSlots.Length; i++)
            {
                equipment[ArmourSlots[i]] = Armour[i];
            }
            equipment[FeaturedWeaponSlot] = FeaturedWeapon;
            return equipment;
        }
    }
}
