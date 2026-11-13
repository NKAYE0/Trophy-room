using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
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
        private string _armourInscriptionText = string.Empty;
        private bool _hasArmourInscription;
        private bool _canInscribeArmour;
        private float _hotkeyCooldown;

        private const int MaxInscriptionLength = 60;
        private const float HotkeyCooldownSeconds = 0.3f;

        public TrophyRoomVM(Settlement settlement, TrophyDisplay display, Action onClose)
        {
            _savedDisplay = display;
            _workingDisplay.CopyFrom(display);
            _onClose = onClose;
            _allCandidates = TrophyItemSources.Collect();

            TextObject title = new TextObject("{=trophyroom_screen_title}Hall of Trophies — {SETTLEMENT}");
            title.SetTextVariable("SETTLEMENT", settlement?.Name ?? new TextObject(string.Empty));
            TitleText = title.ToString();
            ClanBanner = new BannerImageIdentifierVM(Clan.PlayerClan?.Banner, nineGrid: true);
            AddArmourInscriptionText = new TextObject("{=trophyroom_add_armour_inscription}Inscribe this armour set").ToString();
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
                TrophySlotVM slot = new TrophySlotVM(false, i, EquipmentIndex.None, weaponLabel, OnSlotSelected, OnSlotCleared, OnSlotInscribe);
                slot.SetElement(_workingDisplay.Weapons[i]);
                slot.SetInscription(_workingDisplay.WeaponInscriptions[i]);
                (i < TrophyDisplay.WeaponSlotCount / 2 ? LeftWeapons : RightWeapons).Add(slot);
            }

            for (int i = 0; i < TrophyDisplay.ArmourSlots.Length; i++)
            {
                EquipmentIndex index = TrophyDisplay.ArmourSlots[i];
                TrophySlotVM slot = new TrophySlotVM(true, i, index, GetArmourLabel(index), OnSlotSelected, OnSlotCleared, null);
                slot.SetElement(_workingDisplay.Armour[i]);
                ArmourSlots.Add(slot);
            }

            RefreshMannequin();
            RefreshArmourInscription();
        }

        private static string GetArmourLabel(EquipmentIndex index)
        {
            switch (index)
            {
                case EquipmentIndex.Head:
                    return new TextObject("{=trophyroom_slot_head}Helmet").ToString();
                case EquipmentIndex.Cape:
                    return new TextObject("{=trophyroom_slot_shoulders}Shoulders").ToString();
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

                // The set inscription is dropped once no armour is left on display.
                if (!HasAnyArmour())
                {
                    _workingDisplay.ArmourInscription = string.Empty;
                }
                RefreshArmourInscription();
            }
            else
            {
                // A new or removed weapon starts without an inscription.
                _workingDisplay.Weapons[slot.Index] = element;
                _workingDisplay.WeaponInscriptions[slot.Index] = string.Empty;
                slot.SetInscription(string.Empty);
            }
        }

        private void RefreshMannequin()
        {
            Mannequin.SetEquipment(_workingDisplay.CreateMannequinEquipment());
        }

        private bool HasAnyArmour()
        {
            foreach (EquipmentElement element in _workingDisplay.Armour)
            {
                if (!element.IsEmpty)
                {
                    return true;
                }
            }
            return false;
        }

        private void RefreshArmourInscription()
        {
            string inscription = _workingDisplay.ArmourInscription ?? string.Empty;
            CanInscribeArmour = HasAnyArmour();
            HasArmourInscription = inscription.Length > 0;
            ArmourInscriptionText = HasArmourInscription ? "“" + inscription + "”" : string.Empty;
        }

        // Opens the text popup to engrave a weapon's plaque.
        private void OnSlotInscribe(TrophySlotVM slot)
        {
            if (slot.IsArmour || !slot.HasItem)
            {
                return;
            }

            TextObject title = new TextObject("{=trophyroom_inscribe_title}Engrave {ITEM}");
            title.SetTextVariable("ITEM", slot.NameText);
            ShowInscriptionPopup(title.ToString(), slot.Inscription, text =>
            {
                _workingDisplay.WeaponInscriptions[slot.Index] = text;
                slot.SetInscription(text);
            });
        }

        // Opens the text popup to engrave the armour set's plaque.
        public void ExecuteInscribeArmour()
        {
            if (!HasAnyArmour())
            {
                return;
            }

            string title = new TextObject("{=trophyroom_inscribe_armour_title}Engrave the armour set").ToString();
            ShowInscriptionPopup(title, _workingDisplay.ArmourInscription, text =>
            {
                _workingDisplay.ArmourInscription = text;
                RefreshArmourInscription();
            });
        }

        // Shows the game's text-entry popup; an empty entry removes the inscription.
        private void ShowInscriptionPopup(string title, string currentText, Action<string> onConfirm)
        {
            InformationManager.HideTooltip();
            IsInscriptionPopupOpen = true;

            string description = new TextObject("{=trophyroom_inscribe_desc}Write a short inscription for this plaque, or leave it empty to remove it.").ToString();
            string confirmText = new TextObject("{=trophyroom_inscribe_confirm}Engrave").ToString();

            InformationManager.ShowTextInquiry(new TextInquiryData(
                title,
                description,
                true,
                true,
                confirmText,
                CancelText,
                text =>
                {
                    onConfirm((text ?? string.Empty).Trim());
                    OnInscriptionPopupClosed();
                },
                OnInscriptionPopupClosed,
                false,
                ValidateInscription,
                string.Empty,
                currentText ?? string.Empty));
        }

        private static Tuple<bool, string> ValidateInscription(string text)
        {
            if (text != null && text.Length > MaxInscriptionLength)
            {
                TextObject error = new TextObject("{=trophyroom_inscribe_too_long}Inscriptions can be at most {MAX} characters.");
                error.SetTextVariable("MAX", MaxInscriptionLength);
                return new Tuple<bool, string>(false, error.ToString());
            }
            return new Tuple<bool, string>(true, string.Empty);
        }

        // Briefly ignores this screen's Enter/Esc keys so closing the popup doesn't also close the screen.
        private void OnInscriptionPopupClosed()
        {
            IsInscriptionPopupOpen = false;
            _hotkeyCooldown = HotkeyCooldownSeconds;
        }

        // True while this screen's keyboard shortcuts should be ignored.
        public bool AreHotkeysBlocked => IsInscriptionPopupOpen || _hotkeyCooldown > 0f;

        // Counts down the short key cooldown after the inscription popup closes.
        public void Tick(float dt)
        {
            if (_hotkeyCooldown > 0f)
            {
                _hotkeyCooldown -= dt;
            }
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
            ClanBanner?.OnFinalize();
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

        public bool IsInscriptionPopupOpen { get; private set; }

        [DataSourceProperty]
        public BannerImageIdentifierVM ClanBanner { get; }

        [DataSourceProperty]
        public string AddArmourInscriptionText { get; }

        [DataSourceProperty]
        public string ArmourInscriptionText
        {
            get => _armourInscriptionText;
            set
            {
                if (value != _armourInscriptionText)
                {
                    _armourInscriptionText = value;
                    OnPropertyChangedWithValue(value, nameof(ArmourInscriptionText));
                }
            }
        }

        [DataSourceProperty]
        public bool HasArmourInscription
        {
            get => _hasArmourInscription;
            set
            {
                if (value != _hasArmourInscription)
                {
                    _hasArmourInscription = value;
                    OnPropertyChangedWithValue(value, nameof(HasArmourInscription));
                }
            }
        }

        [DataSourceProperty]
        public bool CanInscribeArmour
        {
            get => _canInscribeArmour;
            set
            {
                if (value != _canInscribeArmour)
                {
                    _canInscribeArmour = value;
                    OnPropertyChangedWithValue(value, nameof(CanInscribeArmour));
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
