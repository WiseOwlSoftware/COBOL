// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>
/// The REPOSITORY paragraph's PROPERTY specifiers (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, the OO names";
/// kb/Work PB5932): each property-name-1 a specifier declares (ISO §2023:12.3.8.2), with the name the declared classes know the
/// property by (literal-4 when the specifier writes <c>AS literal-4</c>, kb/Work PB974, otherwise property-name-1 itself), inherited
/// by a contained unit and a class half with the configuration-section names (§2023:12.3.4 GR1), a unit's own specifier of a name
/// winning. Keyed by <see cref="CobolName"/>; its questions answer a fact (whether the word is specified, §2023:8.4.3.9.3 SR1, and
/// what the classes call it), never a symbol: the accessor is found on the receiver's roster by the name it answers.
/// </summary>
public sealed partial class SymbolTable
{
    private readonly Dictionary<CobolName, string> _propertySpecifiers = [];

    /// <summary>Declare the PROPERTY specifier of <paramref name="word"/>, known to the declared classes as
    /// <paramref name="knownAs"/>. The word stays declared (§2023:8.4.3.9.3 SR1) even when literal-4 was refused, and a later
    /// specifier of the same word replaces an earlier one.</summary>
    internal void DeclarePropertySpecifier(string word, string knownAs) => _propertySpecifiers[Key(word)] = knownAs;

    /// <summary>Whether a PROPERTY specifier in scope declares <paramref name="name"/> (§2023:8.4.3.9.3 SR1).</summary>
    internal bool DeclaresPropertySpecifier(string name) => _propertySpecifiers.ContainsKey(Key(name));

    /// <summary>The name the declared classes know the property <paramref name="name"/> by: its specifier's literal-4, else the
    /// name as written (also when no specifier declares it, whose reference SR1 refuses).</summary>
    internal string PropertyKnownAs(string name) => _propertySpecifiers.GetValueOrDefault(Key(name)) ?? name;

    /// <summary>The container's PROPERTY specifiers, each one this unit does not specify itself (§2023:12.3.4 GR1).</summary>
    private void InheritPropertySpecifiers(SymbolTable container)
    {
        foreach (var (name, knownAs) in container._propertySpecifiers) _propertySpecifiers.TryAdd(name, knownAs);
    }
}
