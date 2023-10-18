using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace OrkestraLib
{   
    public class Utils{
        private static System.Random random = new System.Random();
        public static List<Action<string>> Splice(List<Action<string>> source,int index,int count)
        {
        var items = source.GetRange(index, count);
        source.RemoveRange(index,count);
        return items;

        }
        public static List<string> Splice(List<string> source,int index,int count)
        {
        var items = source.GetRange(index, count);
        source.RemoveRange(index,count);
        return items;

        }
       
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
        .Select(s => s[random.Next(s.Length)]).ToArray());
    }
  }
  
}