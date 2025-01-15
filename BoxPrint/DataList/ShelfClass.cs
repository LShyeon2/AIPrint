using BoxPrint.Modules.Shelf;
using System;

namespace BoxPrint.DataList
{
    public class ShelfClass
    {
        public ShelfItem ShelfItem; // shelf 티칭 데이터
        public string Tagname; // 이름을 나타내는 필드 

        public ShelfClass(ShelfItem S, string Tag)
        {
            ShelfItem = S;
            Tagname = Tag;
        }

        public void CurTagName() // 
        {
            Console.WriteLine("{0} : TagName", Tagname);
        }

    }
}
