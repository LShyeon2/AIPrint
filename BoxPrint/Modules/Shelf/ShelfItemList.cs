using BoxPrint.DataList;
using System;
using System.Collections.Generic;
using System.Linq;


namespace BoxPrint.Modules.Shelf
{
    public class ShelfItemList : ICollection<ShelfItem>, IDisposable
    {
        private SafeObservableCollection<ShelfItem> _items = new SafeObservableCollection<ShelfItem>();
        public SafeObservableCollection<ShelfItem> Items
        {
            get
            {
                return _items;
            }
        }
        private int _MinBay = 0;
        public int MinBay
        {
            get
            {
                if (_MinBay == 0)
                {
                    //220418 HHJ SCS 개선     //- LayOut 설정 관리 추가]
                    if (_items.Count > 0)
                        _MinBay = _items.Min(s => s.ShelfBay);
                }
                return _MinBay;
            }
        }

        private int _MinLevel = 0;
        public int MinLevel
        {
            get
            {
                if (_MinLevel == 0)
                {
                    //220418 HHJ SCS 개선     //- LayOut 설정 관리 추가
                    if (_items.Count > 0)
                        _MinLevel = _items.Min(s => s.ShelfLevel);
                }
                return _MinLevel;
            }
        }

        private int _MaxBay = 0;
        public int MaxBay
        {
            get
            {
                if (_MaxBay == 0)
                {
                    //220418 HHJ SCS 개선     //- LayOut 설정 관리 추가
                    //_MaxBay = _items.Max(s => s.ShelfBay);
                    if (_items.Count > 0)
                        _MaxBay = _items.Max(s => s.ShelfBay);
                }
                return _MaxBay;
            }
        }

        private int _MaxLevel = 0;
        public int MaxLevel
        {
            get
            {
                if (_MaxLevel == 0)
                {
                    //220418 HHJ SCS 개선     //- LayOut 설정 관리 추가
                    //_MaxLevel = _items.Max(s => s.ShelfLevel);
                    if (_items.Count > 0)
                        _MaxLevel = _items.Max(s => s.ShelfLevel);
                }
                return _MaxLevel;
            }
        }

        public ShelfItem this[int ModeID]
        {
            get
            {
                if (_items != null && _items.Where(r => r.number == ModeID).Count() > 0)
                {
                    return _items.Where(r => r.number == ModeID).First();
                }
                return null;
            }
        }

        //220322 HHJ SCS 개발     //- Shelf Control 기능 추가
        public ShelfItem this[string TagName]
        {
            get
            {
                if (_items != null && _items.Where(r => r.TagName == TagName).Count() > 0)
                {
                    return _items.Where(r => r.TagName == TagName).First();
                }
                return null;
            }
        }

        public ShelfItem GetShelfItem(int bank, int bay, int level)
        {
            var sItem = _items.Where(s => s.ShelfBank == bank && s.ShelfLevel == level && s.ShelfBay == bay).FirstOrDefault();
            return sItem;
        }
        public ShelfItem GetShelfItem(string ShelfTag)
        {
            var sItem = _items.Where(s => s.TagName == ShelfTag).FirstOrDefault();
            return sItem;
        }




        #region ICollection<ShelfItem> Members

        public void Add(ShelfItem item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(ShelfItem item)
        {
            return _items.Contains(item);
        }

        public bool ContainKey(string Tagname)
        {
            bool result = false;
            if (_items != null && _items.Where(r => r.TagName == Tagname).Count() > 0)
            {
                result = true;
            }
            return result;
        }

        public void CopyTo(ShelfItem[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(ShelfItem item)
        {
            return _items.Remove(item);
        }

        public bool Remove(int number)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (Items[i].number == number)
                {
                    _items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region IEnumerable<ShelfItem> Members

        public IEnumerator<ShelfItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        ~ShelfItemList()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(false);
        }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        void IDisposable.Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
