using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TrophyRoom.UI
{
    // Main view model for the trophy room screen: mannequin, weapon walls and the item picker.
    public class TrophyRoomVM : ViewModel
    {
        private readonly TrophyDisplay _savedDisplay;
        private readonly TrophyDisplay _workingDisplay = new TrophyDisplay();
        private readonly List<TrophyCandidate> _allCandidates;
        private readonly Action _onClose;

        private TrophySlotVM _selectedSlot;
        private bool _isPickerOpen;
        private bool _hasNoCandidates;
        private string _pickerTitleText;
        private InputKeyItemVM _doneInputKey;
        private InputKeyItemVM _cancelInputKey;

        public TrophyRoomVM(Settlement settlement, TrophyDisplay display, Action onClose)
        {
            _savedDisplay = display;
            _workingDisplay.CopyFrom(display);
            _onClose = onClose;
            _allCandidates = TrophyItemSources.Collect();

            TextObject title = new TextObject("{=trophyroom_screen_title}Trophy Room of {SETTLEMENT}");
            title.SetTextVariable("SETTLEMENT", settlement?.Name ?? new TextObject(string.Empty));
            TitleText = title.ToString();
            DoneText = GameTexts.FindText("str_done").ToString();
            CancelText = GameTexts.FindText("str_cancel").ToString();
            EmptySlotText = new TextObject("{=trophyroom_empty_slot}Empty").ToString();
            NoCandidatesText = new TextObject("{=trophyroom_no_candidates}You have nothing that fits this slot.").ToString();
            _pickerTitleText = string.Empty;

            Mannequin = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
            Mannequin.FillFrom(CharacterObject.PlayerCharacter);

            LeftWeapons = new MBBindingList<TrophySlotVM>();
            RightWeapons = new MBBindingList<TrophySlotVM>();
            ArmourSlots = new MBBindingList<TrophySlotVM>();
            Candidates = new MBBindingList<TrophyCandidateVM>();

            string weaponLabel = new TextObject("{=trophyroom_slot_weapon}Weapon").ToString();
            for (int i = 0; i < TrophyDisplay.WeaponSlotCount; i++)
            {
                TrophySlotVM slot = new TrophySlotVM(false, i, EquipmentIndex.None, weaponLabel, OnSlotSelected, OnSlotCleared);
                slot.SetElement(_workingDisplay.Weapons[i]);
                (i < TrophyDisplay.WeaponSlotCount / 2 ? LeftWeapons : RightWeapons).Add(slot);
            }

            for (int i = 0; i < TrophyDisplay.ArmourSlots.Length; i++)
            {
                EquipmentIndex index = TrophyDisplay.ArmourSlots[i];
                TrophySlotVM slot = new TrophySlotVM(true, i, index, GetArmourLabel(index), OnSlotSelected, OnSlotCleared);
                slot.SetElement(_workingDisplay.Armour[i]);
                ArmourSlots.Add(slot);
            }

            RefreshMannequin();
        }

        private static string GetArmourLabel(EquipmentIndex index)
        {
            switch (index)
            {
                case EquipmentIndex.Head:
                    return new TextObject("{=trophyroom_slot_head}Helmet").ToString();
                case EquipmentIndex.Cape:
                    return new TextObject("{=trophyroom_slot_cape}Cape").ToString();
                case EquipmentIndex.Body:
                    return new TextObject("{=trophyroom_slot_body}Body").ToString();
                case EquipmentIndex.Gloves:
                    return new TextObject("{=trophyroom_slot_gloves}Gloves").ToString();
                case EquipmentIndex.Leg:
                    return new TextObject("{=trophyroom_slot_legs}Boots").ToString();
                default:
                    return string.Empty;
            }
        }

        // Opens the picker filled with items that fit the chosen slot.
        private void OnSlotSelected(TrophySlotVM slot)
        {
            if (_selectedSlot != null)
            {
                _selectedSlot.IsSelected = false;
            }

            _selectedSlot = slot;
            slot.IsSelected = true;

            Candidates.Clear();
            foreach (TrophyCandidate candidate in _allCandidates)
            {
                if (slot.Accepts(candidate.Element.Item))
                {
                    Candidates.Add(new TrophyCandidateVM(candidate, OnCandidateSelected));
                }
            }

            TextObject pickerTitle = new TextObject("{=trophyroom_picker_title}Choose: {SLOT}");
            pickerTitle.SetTextVariable("SLOT", slot.SlotLabel);
            PickerTitleText = pickerTitle.ToString();
            HasNoCandidates = Candidates.Count == 0;
            IsPickerOpen = true;
        }

        // Places the chosen item into the selected slot.
        private void OnCandidateSelected(TrophyCandidateVM candidate)
        {
            if (_selectedSlot == null)
            {
                return;
            }

            SetSlot(_selectedSlot, candidate.Element);
            ExecuteClosePicker();
        }

        private void OnSlotCleared(TrophySlotVM slot)
        {
            InformationManager.HideTooltip();
            SetSlot(slot, EquipmentElement.Invalid);
        }

        private void SetSlot(TrophySlotVM slot, EquipmentElement element)
        {
            slot.SetElement(element);
            if (slot.IsArmour)
            {
                _workingDisplay.Armour[slot.Index] = element;
                RefreshMannequin();
            }
            else
            {
                _workingDisplay.Weapons[slot.Index] = element;
            }
        }

        private void RefreshMannequin()
        {
            Mannequin.SetEquipment(_workingDisplay.CreateMannequinEquipment());
        }

        public void ExecuteClosePicker()
        {
            if (_selectedSlot != null)
            {
                _selectedSlot.IsSelected = false;
                _selectedSlot = null;
            }

            InformationManager.HideTooltip();
            IsPickerOpen = false;
            Candidates.Clear();
        }

        // Saves the changes and closes the screen.
        public void ExecuteDone()
        {
            _savedDisplay.CopyFrom(_workingDisplay);
            Close();
        }

        // Discards the changes and closes the screen.
        public void ExecuteCancel()
        {
            Close();
        }

        private void Close()
        {
            InformationManager.HideTooltip();
            _onClose?.Invoke();
        }

        // Sets the key hints shown on the Done and Cancel buttons.
        public void SetInputKeys(HotKey doneKey, HotKey cancelKey)
        {
            DoneInputKey = doneKey != null ? InputKeyItemVM.CreateFromHotKey(doneKey, true) : null;
            CancelInputKey = cancelKey != null ? InputKeyItemVM.CreateFromHotKey(cancelKey, true) : null;
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            Mannequin?.OnFinalize();
            DoneInputKey?.OnFinalize();
            CancelInputKey?.OnFinalize();
        }

        [DataSourceProperty]
        public InputKeyItemVM DoneInputKey
        {
            get => _doneInputKey;
            set
            {
                if (value != _doneInputKey)
                {
                    _doneInputKey = value;
                    OnPropertyChangedWithValue(value, nameof(DoneInputKey));
                }
            }
        }

        [DataSourceProperty]
        public InputKeyItemVM CancelInputKey
        {
            get => _cancelInputKey;
            set
            {
                if (value != _cancelInputKey)
                {
                    _cancelInputKey = value;
                    OnPropertyChangedWithValue(value, nameof(CancelInputKey));
                }
            }
        }

        [DataSourceProperty]
        public string TitleText { get; }

        [DataSourceProperty]
        public string DoneText { get; }

        [DataSourceProperty]
        public string CancelText { get; }

        [DataSourceProperty]
        public string EmptySlotText { get; }

        [DataSourceProperty]
        public string NoCandidatesText { get; }

        [DataSourceProperty]
        public CharacterViewModel Mannequin { get; }

        [DataSourceProperty]
        public MBBindingList<TrophySlotVM> LeftWeapons { get; }

        [DataSourceProperty]
        public MBBindingList<TrophySlotVM> RightWeapons { get; }

        [DataSourceProperty]
        public MBBindingList<TrophySlotVM> ArmourSlots { get; }

        [DataSourceProperty]
        public MBBindingList<TrophyCandidateVM> Candidates { get; }

        [DataSourceProperty]
        public bool IsPickerOpen
        {
            get => _isPickerOpen;
            set
            {
                if (value != _isPickerOpen)
                {
                    _isPickerOpen = value;
                    OnPropertyChangedWithValue(value, nameof(IsPickerOpen));
                }
            }
        }

        [DataSourceProperty]
        public bool HasNoCandidates
        {
            get => _hasNoCandidates;
            set
            {
                if (value != _hasNoCandidates)
                {
                    _hasNoCandidates = value;
                    OnPropertyChangedWithValue(value, nameof(HasNoCandidates));
                }
            }
        }

        [DataSourceProperty]
        public string PickerTitleText
        {
            get => _pickerTitleText;
            set
            {
                if (value != _pickerTitleText)
                {
                    _pickerTitleText = value;
                    OnPropertyChangedWithValue(value, nameof(PickerTitleText));
                }
            }
        }
    }
}
