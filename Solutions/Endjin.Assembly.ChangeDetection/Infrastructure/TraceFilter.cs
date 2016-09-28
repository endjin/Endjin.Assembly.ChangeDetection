using System;
using System.Diagnostics;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    internal class TraceFilter
    {
        private const int MATCHANY = -1; // Hash value that marks a *

        private readonly int[] myFilterHashes;

        private readonly Level myLevelFilter;

        private readonly MessageTypes myMsgTypeFilter = MessageTypes.None;

        private string myFilter;

        internal TraceFilter Next;

        protected TraceFilter()
        {
        }

        public TraceFilter(string typeFilter, MessageTypes msgTypeFilter, Level levelFilter, TraceFilter next)
        {
            if (string.IsNullOrEmpty(typeFilter))
            {
                throw new ArgumentException("typeFilter was null or empty");
            }

            this.myFilter = typeFilter;
            this.Next = next;
            this.myMsgTypeFilter = msgTypeFilter;
            this.myLevelFilter = levelFilter;

            var parts = typeFilter.Trim().ToLower().Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            this.myFilterHashes = new int[parts.Length];
            Debug.Assert(parts.Length > 0, "Type filter parts should be > 0");
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "*")
                {
                    this.myFilterHashes[i] = MATCHANY;
                }
                else
                {
                    this.myFilterHashes[i] = parts[i].GetHashCode();
                }
            }
        }

        public virtual bool IsMatch(TypeHashes type, MessageTypes msgTypeFilter, Level level)
        {
            var lret = ((level & this.myLevelFilter) != Level.None);

            if (lret)
            {
                var areSameSize = (this.myFilterHashes.Length == type.myTypeHashes.Length);

                for (var i = 0; i < this.myFilterHashes.Length; i++)
                {
                    if (this.myFilterHashes[i] == MATCHANY)
                    {
                        break;
                    }

                    if (i < type.myTypeHashes.Length)
                    {
                        // The current filter does not match exit
                        // otherwise we compare the next round.
                        if (this.myFilterHashes[i] != type.myTypeHashes[i])
                        {
                            lret = false;
                            break;
                        }

                        // We are still here when the last arry item matches
                        // This is a full match
                        if (i == this.myFilterHashes.Length - 1 && areSameSize)
                        {
                            break;
                        }
                    }
                    else // the filter string is longer than the domain. That can never match
                    {
                        lret = false;
                        break;
                    }
                }
            }

            if (lret)
            {
                lret = (msgTypeFilter & this.myMsgTypeFilter) != MessageTypes.None;
            }

            // If no match try next filter
            if (this.Next != null && lret == false)
            {
                lret = this.Next.IsMatch(type, msgTypeFilter, level);
            }

            return lret;
        }
    }
}