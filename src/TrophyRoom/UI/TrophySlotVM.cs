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

        private bool _hasItem;
        private bool _isSelected;
        private string _nameText;
        private ItemImageIdentifierVM _imageIdentifier;
        private string _slotLabel;

        public TrophySlotVM(bool isArmour, int index, EquipmentIndex armourSlot, string slotLabel, Action<TrophySlotVM> onSelect, Action<TrophySlotVM> onClear)
        {
            IsArmour = isArmour;
            Index = index;
            ArmourSlot = armourSlot;
            _slotLabel = slotLabel;
            _onSelect = onSelect;
            _onClear = onClear;
            _nameText = string.Empty;
        }

        public bool IsArmour { get; }

        public int Index { get; }

        public EquipmentIndex ArmourSlot { get; }

        public EquipmentElement Element { get; private set; }

        // Shows the given item in this slot (or empties it).
        public void SetElement(EquipmentElement element)
        {
            Element = element;
            HasItem = !element.IsEmpty;
            ImageIdentifier = element.IsEmpty ? null : new ItemImageIdentifierVM(element.Item);
            NameText = element.IsEmpty ? string.Empty : element.GetModifiedItemName().ToString();
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
    }
}
