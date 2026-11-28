// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Compiler.Symbols;

namespace WiseOwl.Cobol.Compiler.Binding;

public sealed partial class SymbolTable
{
    /// <summary>The X3.23-1985 DEBUG-ITEM special register and its members by name (§1985:XV.1.3.2: "The names of the subordinate
    /// data items in DEBUG-ITEM are also reserved words"), empty until a debugging section activates the family. The registers
    /// themselves, in the order of their implicit description, are <see cref="ImplicitRegisters.DebugRegisters"/>, a list keyed by nothing.</summary>
    private readonly Dictionary<CobolName, (DataItem Item, DebugRegisterMember Member)> _debugRegisters = [];

    /// <summary>Each register name's entry, made when the family is first activated: the standard declares the names implicitly
    /// (§1985:XV.3.2.4 GR10), so an entry stands at no position of the source, and a re-activation that widens DEBUG-CONTENTS makes a
    /// new item under the same name, never a second entry.</summary>
    private readonly Dictionary<DebugRegisterMember, SymbolId> _debugEntries = [];

    /// <summary>Activates the DEBUG-ITEM register family, or widens DEBUG-CONTENTS to <paramref name="contentsWidth"/>
    /// (<see cref="ImplicitRegisters.ActivateDebugRegisters"/>), and keys its names here, where a reference finds them
    /// (§1985:XV.3.2.4 GR10: "DEBUG-ITEM has the following implicit description").</summary>
    internal void ActivateDebugRegisters(int contentsWidth = Runtime.DebugItem.MinContentsWidth)
    {
        _data.State.Implicit.ActivateDebugRegisters(_data.TakeUid, contentsWidth);
        _debugRegisters.Clear();
        foreach (var register in _data.State.Implicit.DebugRegisters)
        {
            _debugRegisters[Key(register.Item.CobolName!)] = register;
            if (!_debugEntries.ContainsKey(register.Member))
                _debugEntries[register.Member] = Enter(NameClass.DebugRegister, register.Item.CobolName!, default,
                    new NameDeclaration<DebugRegisterMember>(register.Member));
        }
    }

    /// <summary>The DEBUG-ITEM register or member one word names, as the <see cref="DebugRegisterPlace"/> a reference reads, or
    /// null.</summary>
    private (object? Symbol, SymbolId Id) DebugRegisterNamed(CobolName key) =>
        _debugRegisters.TryGetValue(key, out var register)
            ? (new DebugRegisterPlace(register.Item, register.Member), _debugEntries[register.Member])
            : default;
}
