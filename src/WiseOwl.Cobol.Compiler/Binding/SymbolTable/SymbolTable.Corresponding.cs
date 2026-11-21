// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>The pairing of a CORRESPONDING statement (§2023:14.7.6): the subordinates of two resolved groups are paired by their
/// declared data-names, at one qualification level at a time, so the names are compared by the table's key
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Equality of COBOL names is private too"). No written name is resolved:
/// both operands were identified where they were written.</summary>
public sealed partial class SymbolTable
{
    /// <summary>The members of one operand's qualification level, by data-name, each list in the order given.</summary>
    internal sealed class CorrespondingMembers<T>
    {
        private readonly Dictionary<CobolName, List<T>> _byName = [];

        internal CorrespondingMembers(IEnumerable<T> members, Func<T, DataItem> itemOf)
        {
            foreach (var m in members)
            {
                var key = Key(itemOf(m).CobolName!);
                if (!_byName.TryGetValue(key, out var list)) _byName[key] = list = [];
                list.Add(m);
            }
        }

        /// <summary>The members named as <paramref name="item"/> is; null when none is.</summary>
        public IReadOnlyList<T>? NamedAs(DataItem item) => _byName.GetValueOrDefault(Key(item.CobolName!));
    }

    /// <summary>How many data items answer to each data-name at one qualification level of an operand (§2023:14.7.6 GR6's
    /// universe; <c>CorrespondingBinder.NamesakeCounts</c> says which items count).</summary>
    internal sealed class NamesakeCounts
    {
        private readonly Dictionary<CobolName, int> _counts = [];

        /// <summary>Counts one item named <paramref name="name"/>.</summary>
        public void Count(string name) => _counts[Key(name)] = _counts.GetValueOrDefault(Key(name)) + 1;

        /// <summary>How many counted items are named as <paramref name="item"/> is.</summary>
        public int Of(DataItem item) => _counts[Key(item.CobolName!)];
    }
}
