using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALO_Core.Tools
{
    static class ClassExtensions
    {
        public static void AppendRange<T>(this LinkedList<T> source,
                                      IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                source.AddLast(item);
            }
        }

        public static void PrependRange<T>(this LinkedList<T> source,
                                           IEnumerable<T> items)
        {
            LinkedListNode<T> first = source.First;
            foreach (T item in items)
            {
                source.AddBefore(first, item);
            }
        }

        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }
    }
}
