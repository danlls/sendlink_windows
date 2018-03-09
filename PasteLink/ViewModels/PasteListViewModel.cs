using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasteLink
{
    class PasteListViewModel : ViewModelBase
    {

        private SQLiteHelper sQLiteHelper;
        private int currentCount;

        public PasteListViewModel()
        {
            sQLiteHelper = new SQLiteHelper();
            //_pasteList = sQLiteHelper.GetAllPastes();
            _pasteList = new ObservableCollection<Paste>();
            currentCount = 0;
            LoadPastes(0);
            DeletePasteCommand = new RelayCommand((paste) => DeletePaste(paste));
        }

        private ObservableCollection<Paste> _pasteList;

        public ObservableCollection<Paste> Pastes
        {
            get { return _pasteList; }
            set { _pasteList = value; NotifyPropertyChanged("Pastes"); }
        }

        public RelayCommand DeletePasteCommand { get; set; }

        private void DeletePaste(object paste)
        {
            Paste mPaste = (Paste)paste;
            _pasteList.Remove(mPaste);
       
            sQLiteHelper.DeletePaste(mPaste.ID);
            NotifyPropertyChanged("Pastes");
        }

        public void LoadPastes(int count)
        {
            List<Paste> resultList = sQLiteHelper.LoadPasteByOffset(currentCount - 1, 10);
            currentCount += resultList.Count;
            foreach(Paste paste in resultList)
            {
                _pasteList.Add(paste);
            }
        }

    }
}
