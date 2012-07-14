using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LambdaLifter.Utility
{
    public class Pair<X, Y>
    {
        public X first;
        public Y second;

        public Pair(X first, Y second)
        {
            this.first = first;
            this.second = second;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj == this)
                return true;
            var other = obj as Pair<X, Y>;
            if (other == null)
                return false;

            return
                (((first == null) && (other.first == null))
                 || ((first != null) && first.Equals(other.first)))
                &&
                (((second == null) && (other.second == null))
                 || ((second != null) && second.Equals(other.second)));
        }

        public override int GetHashCode()
        {
            int hashcode = 0;
            if (first != null)
                hashcode += first.GetHashCode();
            if (second != null)
                hashcode += second.GetHashCode();

            return hashcode;
        }
    }
}