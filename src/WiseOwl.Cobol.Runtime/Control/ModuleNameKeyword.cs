// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The keyword argument of FUNCTION MODULE-NAME (ISO §2023:15.65.2), the ONE type both ends name (kb/Work PB2742): the binder binds
/// the written keyword to it once (<c>IntrinsicBinder.BindModuleName</c>), the bound node carries it, the emitted call names the
/// member, and <see cref="ModuleStack.Name"/> switches on it. Before this type the keyword crossed as a bare int 0..4 that the binder
/// encoded and the runtime decoded with a single-space default arm, so a reordered switch on one side compiled and answered another
/// keyword's value.
/// </summary>
public enum ModuleNameKeyword
{
    /// <summary>CURRENT: the outermost program of the currently running compilation unit (§2023:15.65.4 RV7).</summary>
    Current,
    /// <summary>ACTIVATING: the runtime element that activated the current one (§2023:15.65.4 RV5, RV6).</summary>
    Activating,
    /// <summary>NESTED: the most recently activated nested program still running (§2023:15.65.4 RV8); written only within a
    /// nested program (§2023:15.65.3 AR1).</summary>
    Nested,
    /// <summary>STACK: the list r9 composes from CURRENT, the ACTIVATING chain and TOP-LEVEL (§2023:15.65.4 RV9).</summary>
    Stack,
    /// <summary>TOP-LEVEL: the runtime element the operating environment activated to begin the run unit (§2023:15.65.4 RV10).</summary>
    TopLevel,
}
