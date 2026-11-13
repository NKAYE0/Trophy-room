using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TrophyRoom
{
    // A mannequin stance the player can pick (a vanilla animation and its display name).
    public class TrophyPose
    {
        public TrophyPose(string actionId, TextObject name)
        {
            ActionId = actionId;
            Name = name;
        }

        // Animation id, or null for the automatic choice.
        public string ActionId { get; }

        public TextObject Name { get; }
    }

    // The list of mannequin stances, plus the automatic stance chosen from the featured weapon.
    // Only vanilla preview/portrait poses are used: battle idles belong to alternative groups that the
    // engine swaps out over time, which leaves a preview character lying flat.
    public static class TrophyPoses
    {
        public const string RelaxedAction = "act_inventory_idle_start";

        private const string SwordAndShield = "act_pose_infantry_sword_shield";
        private const string TwoHanded = "act_pose_infantry_sword_twohanded";
        private const string Spear = "act_pose_troop_spear";
        private const string Javelin = "act_pose_infantry_javelin";
        private const string Crossbow = "act_pose_archer_crossbow";

        private static readonly string[] Numerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };

        private static List<TrophyPose> _all;

        // All selectable stances; the first entry is Automatic.
        public static IReadOnlyList<TrophyPose> All => _all ?? (_all = Build());

        private static List<TrophyPose> Build()
        {
            List<TrophyPose> poses = new List<TrophyPose>
            {
                new TrophyPose(null, new TextObject("{=trophyroom_pose_auto}Automatic")),
                new TrophyPose(RelaxedAction, new TextObject("{=trophyroom_pose_relaxed}Relaxed")),

                new TrophyPose(SwordAndShield, new TextObject("{=trophyroom_pose_sword_shield}Sword and shield")),
                new TrophyPose("act_pose_defend_hand_shield_up_1h_passive_down", new TextObject("{=trophyroom_pose_shield_up}Shield raised")),
                new TrophyPose(TwoHanded, new TextObject("{=trophyroom_pose_2h}Two-handed")),
                new TrophyPose("act_pose_troop_infantry_sword2h", new TextObject("{=trophyroom_pose_2h_rest}Two-handed, resting")),
                new TrophyPose(Spear, new TextObject("{=trophyroom_pose_spear}Spear")),
                new TrophyPose("act_pose_infantry_staff", new TextObject("{=trophyroom_pose_staff}Polearm")),
                new TrophyPose("act_pose_infantry_staff_shield", new TextObject("{=trophyroom_pose_staff_shield}Polearm and shield")),
                new TrophyPose(Javelin, new TextObject("{=trophyroom_pose_javelin}Javelin")),
                new TrophyPose("act_pose_archer_bow", new TextObject("{=trophyroom_pose_bow}Bow")),
                new TrophyPose(Crossbow, new TextObject("{=trophyroom_pose_crossbow}Crossbow")),
                new TrophyPose("act_pose_troop_crossbow", new TextObject("{=trophyroom_pose_crossbow_rest}Crossbow, resting")),

                new TrophyPose("act_pose_lord_3", new TextObject("{=trophyroom_pose_lord_soldier}Soldierly")),
                new TrophyPose("act_pose_lord_6", new TextObject("{=trophyroom_pose_lord_honorable}Honourable")),
                new TrophyPose("act_pose_lord_7", new TextObject("{=trophyroom_pose_lord_relaxed}At ease")),
                new TrophyPose("act_pose_lord_8", new TextObject("{=trophyroom_pose_lord_reckless}Reckless")),
                new TrophyPose("act_pose_lord_2", new TextObject("{=trophyroom_pose_lord_calculating}Calculating")),
                new TrophyPose("act_pose_lord_1", new TextObject("{=trophyroom_pose_lord_mercy}Merciful")),
                new TrophyPose("act_pose_lord_4", new TextObject("{=trophyroom_pose_lord_miser}Miserly")),
                new TrophyPose("act_pose_lord_5", new TextObject("{=trophyroom_pose_lord_uncertain}Uncertain"))
            };

            for (int i = 1; i <= 8; i++)
            {
                TextObject name = new TextObject("{=trophyroom_pose_portrait}Portrait {N}");
                name.SetTextVariable("N", Numerals[i - 1]);
                poses.Add(new TrophyPose("act_tableau_pose_" + i, name));
            }

            return poses;
        }

        // Position of a saved stance in the list (0 = Automatic if it isn't recognised).
        public static int IndexOf(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
            {
                return 0;
            }
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].ActionId == actionId)
                {
                    return i;
                }
            }
            return 0;
        }

        // Automatic stance: a vanilla preview pose suited to the held weapon, so it doesn't clip the legs.
        public static string GetAutomatic(ItemObject weapon)
        {
            if (weapon == null)
            {
                return RelaxedAction;
            }

            switch (weapon.ItemType)
            {
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.Shield:
                    return SwordAndShield;
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                    return TwoHanded;
                case ItemObject.ItemTypeEnum.Polearm:
                    return Spear;
                case ItemObject.ItemTypeEnum.Crossbow:
                    return Crossbow;
                case ItemObject.ItemTypeEnum.Thrown:
                    return Javelin;
                default:
                    return RelaxedAction;
            }
        }
    }
}
