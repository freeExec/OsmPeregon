using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmPeregon
{
    public static class PairwiseExtension
    {
        public struct Pair<T> : IEquatable<Pair<T>>
        {
            private readonly T previous;
            private readonly T current;

            public T Previous => previous;
            public T Current => current;

            public Pair(T previous, T current)
            {
                this.previous = previous;
                this.current = current;
            }

            public override int GetHashCode()
            {
                var comparer = EqualityComparer<T>.Default;

                int h0;
                h0 = comparer.GetHashCode(previous);
                h0 = (h0 << 5) + h0 ^ comparer.GetHashCode(current);
                return h0;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Pair<T>)) return false;

                return Equals((Pair<T>)obj);
            }

            public bool Equals(Pair<T> other)
            {
                var comparer = EqualityComparer<T>.Default;

                return comparer.Equals(previous, other.Previous) &&
                       comparer.Equals(current, other.Current);
            }

            public override string ToString() => $"{previous} - {current}";
        }

        /// <summary>Projects old and new element of a sequence into a new form</summary>
        public static IEnumerable<Pair<T>> Pairwise<T>(this IEnumerable<T> source)
        {
            bool isFirst = true;
            T prev = default;
            foreach (var element in source)
            {
                if (isFirst)
                {
                    prev = element;
                    isFirst = false;
                    continue;
                }

                yield return new Pair<T>(prev, element);
                prev = element;
            }
        }
    }
}
