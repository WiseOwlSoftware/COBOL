// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Exceptions;

// ⛔ The exception engine's own §2023:14.6.13.1.4 #3 callback contract, beside the DispatchResult protocol it answers
// (kb/Work PB2743): a lower layer declares the contract it calls through, and the program ABI (ICobolProgram) and the
// OO method prologue implement it from above.

/// <summary>The §2023:14.6.13.1.4 #3 selection of ONE runtime element, seen from a RUNTIME raise site: the activation
/// now executing installs its selector in <see cref="ExceptionState.NonfatalDispatcher"/> and restores
/// its activator's on return, so a nonfatal condition detected inside the runtime selects over the declaratives of
/// the source element containing the raising statement (§2023:14.9.49.4 GR4a) and never its activator's. A PROGRAM
/// is its own selector (<see cref="ICobolProgram"/>, installed by <c>ProgramTable</c>); an OO METHOD is a runtime
/// element too (§2023:15.65.4 r5 — "an INVOKE statement" activates one) and installs a <see cref="NonfatalSelectorFn"/>
/// over its own selection — or <see cref="NonfatalSelectorFn.None"/> when it declares none (kb/Work PB1010).</summary>
public interface INonfatalSelector
{
    /// <summary>The <see cref="DispatchResult"/> protocol: <c>Normal</c> completed normally, <c>ResumeNext</c> RESUME AT NEXT STATEMENT,
    /// <c>NoHandler</c> no qualifying declarative, a value <c>≥ 0</c> RESUME AT that pc (see <see cref="ICobolProgram"/>).</summary>
    int NonfatalDispatch(string ec);
}

/// <summary>A method activation's <see cref="INonfatalSelector"/> — the method's own generated selection (a local
/// function capturing the activation's data) behind the runtime's one interface (kb/Work PB1010).</summary>
public sealed class NonfatalSelectorFn(Func<string, int> select) : INonfatalSelector
{
    /// <summary>The selector of an element that declares no USE procedures and no exception-checking PERFORM:
    /// "no qualifying declarative", shared, allocation-free.</summary>
    public static readonly NonfatalSelectorFn None = new(static _ => DispatchResult.NoHandler);

    public int NonfatalDispatch(string ec) => select(ec);
}
