// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>⛔ THE ONE PLACE A METHOD NAME IS LOOKED UP ON A RECEIVER'S ROSTER (kb/Work PB4893). A receiver of a known
/// description selects ONE roster (§2023:14.9.23.3 SR4): a class's FACTORY interface or its INSTANCE interface, both searched
/// over the inheritance chain (§2023:9.3.6), or an interface's prototype closure (SR4 e)). The roster is keyed by the
/// EXTERNALIZED name (§2023:14.9.23.4 GR2a → §2023:8.3.2.2; §2023:11.7.4 GR1b: "literal-1, if specified, is the name of
/// method that is externalized"), on every arm alike — the interface arm once compared method-name-1, so
/// <c>INVOKE I "Y"</c> of a prototype <c>METHOD-ID. X AS "Y"</c> was refused while the class arm found it.
/// <para>Every reader that selects a method by name — an invocation through a typed receiver, SELF or SUPER, a property's
/// accessor, the diagnostic-free probe of the statement funnel and the conformance walks — asks <see cref="Find"/>, never a
/// <c>factory ? FindFactoryMethod : FindMethod</c> of its own (pinned by <c>MethodRosterLookupDriftTests</c>).</para></summary>
public readonly record struct MethodRoster(OoClassSymbol? Class, OoInterfaceSymbol? Interface, bool Factory)
{
    /// <summary>The factory (<paramref name="factory"/>) or instance interface of <paramref name="cls"/> (§2023:9.3.6).</summary>
    public static MethodRoster Of(OoClassSymbol cls, bool factory) => new(cls, null, factory);

    /// <summary>The prototype closure of <paramref name="iface"/> (§2023:9.3.8.2.2), which has no factory half.</summary>
    public static MethodRoster Of(OoInterfaceSymbol iface) => new(null, iface, false);

    /// <summary>The method this roster holds under the externalized name <paramref name="externalizedName"/>, or null.</summary>
    public OoMethodSymbol? Find(string externalizedName) =>
        Interface is { } iface
            ? iface.AllPrototypes().FirstOrDefault(p => WiseOwl.Cobol.Runtime.CobolNames.Same(p.ExternalizedName, externalizedName))
            : Class is not { } cls ? null
            : Factory ? cls.FindFactoryMethod(externalizedName) : cls.FindMethod(externalizedName);
}
