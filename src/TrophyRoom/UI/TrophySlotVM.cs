using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TrophyRoom.UI
{
    // A single display slot (weapon or armour piece) on the trophy room screen.
    public class TrophySlotVM : ViewModel
    {
        private readonly Action<TrophySlotVM> _onSelect;
        private readonly Action<TrophySlotVM> _onClear;
        private readonly Action<TrophySlotVM> _onInscribe;
        private readonly Action<TrophySlotVM> _onToggleFeatured;

        private bool _hasItem;
        private bool _isSelected;
        private string _nameText;
        private ItemImageIdentifierVM _imageIdentifier;
        private string _slotLabel;
        private string _inscriptionText;
        private bool _hasInscription;
        private bool _isEditMode;
        private bool _showEditControls;
        private bool _isSlotVisible;
        private bool _isFeatured;
        private bool _showFeatureToggle;

        public TrophySlotVM(bool isArmour, int index, EquipmentIndex armourSlot, string slotLabel, Action<TrophySlotVM> onSelect, Action<TrophySlotVM> onClear, Action<TrophySlotVM> onInscribe, Action<TrophySlotVM> onToggleFeatured)
        {
            IsArmour = isArmour;
            Index = index;
            ArmourSlot = armourSlot;
            _slotLabel = slotLabel;
            _onSelect = onSelect;
            _onClear = onClear;
            _onInscribe = onInscribe;
            _onToggleFeatured = onToggleFeatured;
            _nameText = string.Empty;
            _inscriptionText = string.Empty;
        }

        public bool IsArmour { get; }

        public int Index { get; }

        public EquipmentIndex ArmourSlot { get; }

        public EquipmentElement Element { get; private set; }

        // Raw inscription text without decoration.
        public string Inscription { get; private set; } = string.Empty;

        // Shows the given item in this slot (or empties it).
        public void SetElement(EquipmentElement element)
        {
            Element = element;
            HasItem = !element.IsEmpty;
            ImageIdentifier = element.IsEmpty ? null : new ItemImageIdentifierVM(element.Item);
            NameText = element.IsEmpty ? string.Empty : element.GetModifiedItemName().ToString();
            RefreshVisibility();
        }

        // Switches between edit mode (controls and empty slots shown) and display mode (filled slots only).
        public void SetEditMode(bool isEditMode)
        {
            IsEditMode = isEditMode;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            ShowEditControls = IsEditMode && HasItem;
            ShowFeatureToggle = ShowEditControls && _onToggleFeatured != null;
            IsSlotVisible = IsEditMode || HasItem;
        }

        // Shows the given inscription under the item name.
        public void SetInscription(string inscription)
        {
            Inscription = inscription ?? string.Empty;
            HasInscription = Inscription.Length > 0;
            InscriptionText = HasInscription ? "“" + Inscription + "”" : string.Empty;
        }

        // True if the item is allowed in this slot.
        public bool Accepts(ItemObject item)
        {
            return IsArmour
                ? TrophyItemSources.GetArmourSlot(item) == ArmourSlot
                : TrophyItemSources.IsDisplayWeapon(item);
        }

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }

        public void ExecuteClear()
        {
            _onClear?.Invoke(this);
        }

        public void ExecuteToggleFeatured()
        {
            _onToggleFeatured?.Invoke(this);
        }

        public void ExecuteInscribe()
        {
            _onInscribe?.Invoke(this);
        }

        public void ExecuteBeginHint()
        {
            if (!Element.IsEmpty)
            {
                InformationManager.ShowTooltip(typeof(ItemObject), Element);
            }
        }

        public void ExecuteEndHint()
        {
            InformationManager.HideTooltip();
        }

        // Which silhouette an empty armour slot shows.
        [DataSourceProperty]
        public bool IsHeadSlot => ArmourSlot == EquipmentIndex.Head;

        [DataSourceProperty]
        public bool IsCapeSlot => ArmourSlot == EquipmentIndex.Cape;

        [DataSourceProperty]
        public bool IsBodySlot => ArmourSlot == EquipmentIndex.Body;

        [DataSourceProperty]
        public bool IsGlovesSlot => ArmourSlot == EquipmentIndex.Gloves;

        [DataSourceProperty]
        public bool IsLegsSlot => ArmourSlot == EquipmentIndex.Leg;

        [DataSourceProperty]
        public ItemImageIdentifierVM ImageIdentifier
        {
            get => _imageIdentifier;
            set
            {
                if (value != _imageIdentifier)
                {
                    _imageIdentifier = value;
                    OnPropertyChangedWithValue(value, nameof(ImageIdentifier));
                }
            }
        }

        [DataSourceProperty]
        public bool HasItem
        {
            get => _hasItem;
            set
            {
                if (value != _hasItem)
                {
                    _hasItem = value;
                    OnPropertyChangedWithValue(value, nameof(HasItem));
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                }
            }
        }

        [DataSourceProperty]
        public string NameText
        {
            get => _nameText;
            set
            {
                if (value != _nameText)
                {
                    _nameText = value;
                    OnPropertyChangedWithValue(value, nameof(NameText));
                }
            }
        }

        [DataSourceProperty]
        public string SlotLabel
        {
            get => _slotLabel;
            set
            {
                if (value != _slotLabel)
                {
                    _slotLabel = value;
                    OnPropertyChangedWithValue(value, nameof(SlotLabel));
                }
            }
        }

        [DataSourceProperty]
        public string InscriptionText
        {
            get => _inscriptionText;
            set
            {
                if (value != _inscriptionText)
                {
                    _inscriptionText = value;
                    OnPropertyChangedWithValue(value, nameof(InscriptionText));
                }
            }
        }

        // True for the weapon the mannequin holds.
        [DataSourceProperty]
        public bool IsFeatured
        {
            get => _isFeatured;
            set
            {
                if (value != _isFeatured)
                {
                    _isFeatured = value;
                    OnPropertyChangedWithValue(value, nameof(IsFeatured));
                }
            }
        }

        // Featured-weapon star: only on filled weapon slots in edit mode.
        [DataSourceProperty]
        public bool ShowFeatureToggle
        {
            get => _showFeatureToggle;
            set
            {
                if (value != _showFeatureToggle)
                {
                    _showFeatureToggle = value;
                    OnPropertyChangedWithValue(value, nameof(ShowFeatureToggle));
                }
            }
        }

        [DataSourceProperty]
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (value != _isEditMode)
                {
                    _isEditMode = value;
                    OnPropertyChangedWithValue(value, nameof(IsEditMode));
                }
            }
        }

        // Remove and engrave buttons: only in edit mode on filled slots.
        [DataSourceProperty]
        public bool ShowEditControls
        {
            get => _showEditControls;
            set
            {
                if (value != _showEditControls)
                {
                    _showEditControls = value;
                    OnPropertyChangedWithValue(value, nameof(ShowEditControls));
                }
            }
        }

        // Empty slots are hidden in display mode.
        [DataSourceProperty]
        public bool IsSlotVisible
        {
            get => _isSlotVisible;
            set
            {
                if (value != _isSlotVisible)
                {
                    _isSlotVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsSlotVisible));
                }
            }
        }

        [DataSourceProperty]
        public bool HasInscription
        {
            get => _hasInscription;
            set
            {
                if (value != _hasInscription)
                {
                    _hasInscription = value;
                    OnPropertyChangedWithValue(value, nameof(HasInscription));
                }
            }
        }
    }
}
