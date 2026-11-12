using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TrophyRoom.UI;

namespace TrophyRoom
{
    // Adds the Trophy Room option to owned fiefs, saves each fief's display, and clears it if the fief is lost.
    public class TrophyRoomCampaignBehavior : CampaignBehaviorBase
    {
        private const string SaveKey = "TrophyRoom_Displays";

        private readonly TrophyStorage _storage = new TrophyStorage();
        private List<string> _loadedEntries;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        }

        // Saves displays as plain text so saves stay loadable if the mod is removed.
        public override void SyncData(IDataStore dataStore)
        {
            List<string> entries = dataStore.IsSaving ? _storage.Serialize() : null;
            dataStore.SyncData(SaveKey, ref entries);

            if (dataStore.IsLoading)
            {
                _loadedEntries = entries;
            }
        }

        // Rebuilds displays from the save and registers the menu options.
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ApplyLoadedEntries();

            starter.AddGameMenuOption("town_keep", "trophy_room_open", "{=trophyroom_open}Trophy Room", TrophyRoomCondition, TrophyRoomConsequence, false, 4);
            starter.AddGameMenuOption("castle", "trophy_room_open", "{=trophyroom_open}Trophy Room", TrophyRoomCondition, TrophyRoomConsequence, false, 5);
        }

        // Converts saved text entries into displays once game items are available.
        private void ApplyLoadedEntries()
        {
            if (_loadedEntries == null)
            {
                return;
            }

            _storage.Deserialize(_loadedEntries);
            _loadedEntries = null;
        }

        // Shows the option only in fortifications owned by the player's clan.
        private static bool TrophyRoomCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            Settlement settlement = Settlement.CurrentSettlement;
            return settlement != null
                && settlement.IsFortification
                && settlement.Town != null
                && !settlement.Town.IsOwnerUnassigned
                && settlement.OwnerClan == Clan.PlayerClan;
        }

        // Opens the trophy room screen for the current settlement.
        private void TrophyRoomConsequence(MenuCallbackArgs args)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null)
            {
                return;
            }

            TrophyRoomState state = Game.Current.GameStateManager.CreateState<TrophyRoomState>();
            state.Settlement = settlement;
            state.Display = _storage.GetOrCreate(settlement.StringId);
            Game.Current.GameStateManager.PushState(state);
        }

        // Clears a fief's display when it leaves the player's clan, so it starts empty if regained.
        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (settlement == null || newOwner?.Clan == Clan.PlayerClan)
            {
                return;
            }

            ApplyLoadedEntries();
            _storage.Remove(settlement.StringId);
        }
    }
}
