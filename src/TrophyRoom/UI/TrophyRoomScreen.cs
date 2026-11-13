using System.Collections.Generic;
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
        // Vanilla art used by the layout: inventory slots and silhouettes, smithy art, clan edit icon, encyclopedia star.
        private static readonly string[] RequiredSpriteCategories = { "ui_inventory", "ui_crafting", "ui_clan", "ui_encyclopedia" };

        private readonly TrophyRoomState _state;
        private readonly List<SpriteCategory> _loadedCategories = new List<SpriteCategory>();
        private GauntletLayer _layer;
        private TrophyRoomVM _dataSource;

        public TrophyRoomScreen(TrophyRoomState state)
        {
            _state = state;
            _state.RegisterListener(this);
        }

        void IGameStateListener.OnActivate()
        {
            base.OnActivate();
            LoadSpriteCategories();

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
            UnloadSpriteCategories();
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

        // Loads the art this screen needs, remembering only the categories it loaded itself.
        private void LoadSpriteCategories()
        {
            foreach (string name in RequiredSpriteCategories)
            {
                SpriteCategory category = UIResourceManager.GetSpriteCategory(name);
                if (category != null && !category.IsLoaded)
                {
                    _loadedCategories.Add(UIResourceManager.LoadSpriteCategory(name));
                }
            }
        }

        // Unloads only the art this screen loaded, so other screens are unaffected.
        private void UnloadSpriteCategories()
        {
            foreach (SpriteCategory category in _loadedCategories)
            {
                category?.Unload();
            }
            _loadedCategories.Clear();
        }

        // Esc closes the picker first, then cancels (edit mode) or leaves (display mode); Enter saves and closes.
        // Ignored while the inscription popup is in use.
        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if (_layer == null || _dataSource == null)
            {
                return;
            }

            _dataSource.Tick(dt);
            if (_dataSource.AreHotkeysBlocked)
            {
                return;
            }

            if (_layer.Input.IsHotKeyReleased("Exit"))
            {
                if (_dataSource.IsInspectOpen)
                {
                    _dataSource.ExecuteCloseInspect();
                }
                else if (_dataSource.IsPickerOpen)
                {
                    _dataSource.ExecuteClosePicker();
                }
                else if (_dataSource.IsEditMode)
                {
                    _dataSource.ExecuteCancel();
                }
                else
                {
                    // Display mode has nothing to discard, so Esc simply leaves (keeping any earlier edits).
                    _dataSource.ExecuteDone();
                }
            }
            else if (_layer.Input.IsHotKeyReleased("Confirm") && !_dataSource.IsPickerOpen && !_dataSource.IsInspectOpen)
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
