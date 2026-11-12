using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TrophyRoom.UI
{
    // One row in the item picker list.
    public class TrophyCandidateVM : ViewModel
    {
        private readonly Action<TrophyCandidateVM> _onSelect;

        public TrophyCandidateVM(TrophyCandidate candidate, Action<TrophyCandidateVM> onSelect)
        {
            Element = candidate.Element;
            _onSelect = onSelect;
            ImageIdentifier = new ItemImageIdentifierVM(candidate.Element.Item);
            NameText = candidate.Element.GetModifiedItemName().ToString();
            SourceText = candidate.Source?.ToString() ?? string.Empty;
        }

        public EquipmentElement Element { get; }

        public void ExecuteSelect()
        {
            InformationManager.HideTooltip();
            _onSelect?.Invoke(this);
        }

        public void ExecuteBeginHint()
        {
            InformationManager.ShowTooltip(typeof(ItemObject), Element);
        }

        public void ExecuteEndHint()
        {
            InformationManager.HideTooltip();
        }

        [DataSourceProperty]
        public ItemImageIdentifierVM ImageIdentifier { get; }

        [DataSourceProperty]
        public string NameText { get; }

        [DataSourceProperty]
        public string SourceText { get; }
    }
}
