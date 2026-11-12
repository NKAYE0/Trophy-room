using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TrophyRoom.UI
{
    // Game state for the trophy room screen; pauses the campaign like other menu screens.
    public class TrophyRoomState : GameState
    {
        public Settlement Settlement { get; set; }

        public TrophyDisplay Display { get; set; }

        public override bool IsMenuState => true;
    }
}
