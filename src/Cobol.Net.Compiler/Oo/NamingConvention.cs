// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

/// <summary>
/// The ONE home for every OO C# / file-key naming convention (P9 R8 — kills the four scattered
/// <c>__GET_</c>/<c>__SET_</c> accessor-name builders and the loose <c>__FACTORY</c>/band strings).
/// The <c>__</c> prefix cannot appear in a COBOL-derived name (§8.3.1: user words never carry consecutive
/// leading underscores through <see cref="DataItem.Sanitize"/>'s mapping), so none of these collide with user
/// names — the §8.4.5/§11.7.4 GR1a implementor-defined externalized-name license.
/// NOTE — the <c>::EXT::</c> band is a WIRE CONTRACT with the runtime: <c>CobolFile.Register*</c> and
/// <c>ExternalStore</c> recognize the prefix on the emitted key (ISO §13.18.22.4 GR4a run-unit sharing), so the
/// runtime carries its own copy of that spelling; change either side only with the other.
/// </summary>
public static class NamingConvention
{
    /// <summary>The factory singleton class suffix (brief D11 — <c>FOO__FACTORY</c>, a REAL sibling class).</summary>
    public const string FactorySuffix = "__FACTORY";

    /// <summary>The factory singleton field (§9.3.14.2 — "created before it is first referenced").</summary>
    public const string FactoryInstanceField = "__Instance";

    /// <summary>The predefined New creation method (§16.2.1 GR1 — ACTIVE-CLASS covariant creation).</summary>
    public const string FactoryNewMethod = "__New";

    /// <summary>The run-unit EXTERNAL file-connector key band (§13.18.22.4 GR4a — ONE connector per run unit;
    /// the runtime recognizes this prefix: the wire contract noted above).</summary>
    public const string ExternalFileBand = "::EXT::";

    /// <summary>The per-object INSTANCE file-connector key band (M2-OO-1i, §9.1.4 — one connector per object).</summary>
    public const string InstanceFileBand = "::INST::";

    /// <summary>The FACTORY file-connector key band (one connector per class singleton).</summary>
    public const string FactoryFileBand = "::FACT::";

    /// <summary>The pinned accessor-roster name of a property GET method (§11.7.4 GR1a —
    /// <c>__GET_&lt;P&gt;</c>; override/0829/implements machinery applies to accessors unchanged).</summary>
    public static string GetAccessorName(string propertyName) =>
        "__GET_" + DataItem.Sanitize(propertyName).ToUpperInvariant();

    /// <summary>The pinned accessor-roster name of a property SET method (<c>__SET_&lt;P&gt;</c>).</summary>
    public static string SetAccessorName(string propertyName) =>
        "__SET_" + DataItem.Sanitize(propertyName).ToUpperInvariant();
}
