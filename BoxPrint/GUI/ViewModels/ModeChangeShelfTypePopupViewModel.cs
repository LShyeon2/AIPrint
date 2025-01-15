using BoxPrint.Modules.Shelf;

namespace BoxPrint.GUI.ViewModels
{
    public class ModeChangeShelfTypePopupViewModel : ViewModelBase
    {
        private ShelfItem curShelf;

        private bool _ShelfTypeShort;
        public bool ShelfTypeShort
        {
            get => _ShelfTypeShort;
            set => Set("ShelfTypeShort", ref _ShelfTypeShort, value);
        }
        private bool _ShelfTypeLong;
        public bool ShelfTypeLong
        {
            get => _ShelfTypeLong;
            set => Set("ShelfTypeLong", ref _ShelfTypeLong, value);
        }
        private bool _ShelfTypeBoth;
        public bool ShelfTypeBoth
        {
            get => _ShelfTypeBoth;
            set => Set("ShelfTypeBoth", ref _ShelfTypeBoth, value);
        }

        public ModeChangeShelfTypePopupViewModel(ShelfItem shelf)
        {
            curShelf = shelf;
            switch (curShelf.ShelfType)
            {
                case eShelfType.Short:
                    ShelfTypeShort = true;
                    break;
                case eShelfType.Long:
                    ShelfTypeLong = true;
                    break;
                case eShelfType.Both:
                    ShelfTypeBoth = true;
                    break;
            }
        }
    }
}
