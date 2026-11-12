using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TrophyRoom.UI
{
    // Screen that shows the trophy room layout while TrophyRoomState is active.
    [GameStateScreen(typeof(TrophyRoomState))]
    public class TrophyRoomScreen : ScreenBase, IGameStateListener
    {
        private readonly TrophyRoomState _state;
        private GauntletLayer _layer;
        private TrophyRoomVM _dataSource;
        private SpriteCategory _inventoryCategory;

        public TrophyRoomScreen(TrophyRoomState state)
        {
            _state = state;
            _state.RegisterListener(this);
        }

        void IGameStateListener.OnActivate()
        {
            base.OnActivate();
            SpriteCategory inventoryCategory = UIResourceManager.GetSpriteCategory("ui_inventory");
            if (inventoryCategory != null && !inventoryCategory.IsLoaded)
            {
                _inventoryCategory = UIResourceManager.LoadSpriteCategory("ui_inventory");
            }

            GameKeyContext panelKeys = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
            _dataSource = new TrophyRoomVM(_state.Settlement, _state.Display, CloseScreen);
            _dataSource.SetInputKeys(panelKeys.GetHotKey("Confirm"), panelKeys.GetHotKey("Exit"));
            _layer = new GauntletLayer("TrophyRoomScreen", 1, true);
            _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _layer.Input.RegisterHotKeyCategory(panelKeys);
            _layer.LoadMovie("TrophyRoomScreen", _dataSource);
            _layer.IsFocusLayer = true;
            AddLayer(_layer);
            ScreenManager.TrySetFocus(_layer);
        }

        void IGameStateListener.OnDeactivate()
        {
            base.OnDeactivate();
            if (_layer != null)
            {
                _layer.InputRestrictions.ResetInputRestrictions();
                _layer.IsFocusLayer = false;
                ScreenManager.TryLoseFocus(_layer);
                RemoveLayer(_layer);
            }
            // Only unload the inventory sprites if this screen was the one that loaded them.
            _inventoryCategory?.Unload();
            _inventoryCategory = null;
        }

        void IGameStateListener.OnInitialize()
        {
        }

        void IGameStateListener.OnFinalize()
        {
            _dataSource?.OnFinalize();
            _dataSource = null;
            _layer = null;
        }

        // Esc closes the picker first, then the screen; Enter saves and closes.
        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if (_layer == null || _dataSource == null)
            {
                return;
            }

            if (_layer.Input.IsHotKeyReleased("Exit"))
            {
                if (_dataSource.IsPickerOpen)
                {
                    _dataSource.ExecuteClosePicker();
                }
                else
                {
                    _dataSource.ExecuteCancel();
                }
            }
            else if (_layer.Input.IsHotKeyReleased("Confirm") && !_dataSource.IsPickerOpen)
            {
                _dataSource.ExecuteDone();
            }
        }

        private void CloseScreen()
        {
            Game.Current.GameStateManager.PopState();
        }
    }
}
