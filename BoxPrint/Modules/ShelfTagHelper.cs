using System.Text.RegularExpressions;

namespace BoxPrint.Modules
{
    public static class ShelfTagHelper
    {
        //Shelf Rack Tag 순서 Bank,Level,Bay => Bank,Bay,Level 로 수정. SK사양 순서대로 변경하여 혼동 방지
        public static string GetTag(string bank, string bay, string level)
        {
            {
                return string.Format("{0:D02}{1:D03}{2:D02}", bank, bay, level);
            }
        }
        public static string GetTag(int bank, int bay, int level)
        {
            {
                return string.Format("{0:D02}{1:D03}{2:D02}", bank, bay, level);
            }
        }
        public static int GetBank(string tag)
        {
            {
                if (!string.IsNullOrEmpty(tag) && tag.Length == 7)
                {
                    if (int.TryParse(tag.Substring(0, 2), out int ret))
                    {
                        return ret;
                    }

                    return -1;
                }
                else
                {
                    return -1;
                }
            }
        }
        public static int GetBay(string tag)
        {
            {
                if (!string.IsNullOrEmpty(tag) && tag.Length == 7)
                {
                    if (int.TryParse(tag.Substring(2, 3), out int ret))
                    {
                        return ret;
                    }

                    return -1;
                }
                else
                {
                    return -1;
                }
            }
        }
        public static int GetLevel(string tag)
        {
            {
                if (!string.IsNullOrEmpty(tag) && tag.Length == 7)
                {
                    if (int.TryParse(tag.Substring(5, 2), out int ret))
                    {
                        return ret;
                    }

                    return -1;
                }
                else
                {
                    return -1;
                }
            }
        }

    }
}
