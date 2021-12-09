using System.Collections.Generic;
using System.Xml;

namespace dl_cs.Util
{
    public static class StringTimes
    {
        public static string Times(this string str, int times)
        {
            return string.Join(str, new string[times + 1]);


        }
        
    }
}