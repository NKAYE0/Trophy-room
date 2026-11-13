using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade;

namespace TrophyRoom
{
    // Mod entry point: registers the custom border image provider and adds the trophy room behaviour to campaign games.
    public class SubModule : MBSubModuleBase
    {
        private bool _providersRegistered;

        // Re-scans texture providers once so the game finds this mod's border image provider.
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (!_providersRegistered)
            {
                TextureProviderFactory.RefreshProviderTypes();
                _providersRegistered = true;
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (game.GameType is Campaign && gameStarterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(new TrophyRoomCampaignBehavior());
            }
        }
    }
}
