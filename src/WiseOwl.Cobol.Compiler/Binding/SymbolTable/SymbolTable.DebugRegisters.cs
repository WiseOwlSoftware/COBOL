// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>The X3.23-1985 DEBUG-ITEM special register and its members by name (§1985:XV.1.3.2: "The names of the subordinate
    /// data items in DEBUG-ITEM are also reserved words"), empty until a debugging section activates the family. The registers
    /// themselves, in the order of their implicit description, are <see cref="ImplicitRegisters.DebugRegisters"/>, a list keyed by nothing.</summary>
    private readonly Dictionary<CobolName, (DataItem Item, DebugRegisterMember Member)> _debugRegisters = [];

    /// <summary>Activates the DEBUG-ITEM register family, or widens DEBUG-CONTENTS to <paramref name="contentsWidth"/>
    /// (<see cref="ImplicitRegisters.ActivateDebugRegisters"/>), and keys its names here, where a reference finds them
    /// (§1985:XV.3.2.4 GR10: "DEBUG-ITEM has the following implicit description").</summary>
    internal void ActivateDebugRegisters(int contentsWidth = Runtime.DebugItem.MinContentsWidth)
    {
        _data.State.Implicit.ActivateDebugRegisters(_data.TakeUid, contentsWidth);
        _debugRegisters.Clear();
        foreach (var register in _data.State.Implicit.DebugRegisters)
            _debugRegisters[Key(register.Item.CobolName!)] = register;
    }

    /// <summary>The DEBUG-ITEM register or member one word names, as the <see cref="DebugRegisterPlace"/> a reference reads, or
    /// null.</summary>
    private DebugRegisterPlace? DebugRegisterNamed(CobolName key) =>
        _debugRegisters.TryGetValue(key, out var register) ? new DebugRegisterPlace(register.Item, register.Member) : null;
}
