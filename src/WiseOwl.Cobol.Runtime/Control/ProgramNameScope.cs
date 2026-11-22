// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>Which arm of the program-name scope rule (<see cref="ProgramNameScope"/>) reached a program.</summary>
public enum ProgramNameArm
{
    /// <summary>(1) A program directly contained in the referencing program.</summary>
    Contained,
    /// <summary>(2) The referencing program itself, when it has the recursive attribute.</summary>
    RecursiveSelf,
    /// <summary>(3) A COMMON program directly contained in a container of the referencing program, the nearest container first,
    /// under the exception <see cref="ProgramNameScope.CommonProgramReferable"/> states.</summary>
    Common,
}

/// <summary>
/// ⛔ THE ONE SPELLING OF THE SCOPE OF A PROGRAM-NAME (ISO/IEC 1989:2023 §2023:8.4.6.3; kb/Work PB1460, PB5965): the programs a
/// CALL or CANCEL of a program-name reaches from the referencing program, in the order the arms are asked. (1) a program directly
/// contained in the caller; (2) the caller itself when it is RECURSIVE; (3) a COMMON program directly contained in a container of
/// the caller, the nearest container first, "except that the program possessing the common attribute and any programs contained
/// within it may reference the program-name only if the program possesses the recursive attribute"; (4) an outermost program of
/// the run unit, which each side looks up in its own population (the compilation group at bind time, the registered and probed
/// modules at run time), so arm 4 is the caller's.
/// <para>Three readers ask it and must agree: the run-time resolver (<see cref="ProgramTable"/>'s <c>ResolveVisible</c>), the
/// compiler's literal-target scope (<c>BinderDriver.ProgramVisibleFrom</c>, which the listing's CALL graph reads) and the AS NESTED
/// callee table (<c>BinderDriver.NestedCallablesOf</c>, §2023:14.9.4.3 SR15, arms 1 and 3). Each used to write the arms itself; only
/// the COMMON exception was shared (PB1460, after the bind-time and run-time copies had drifted), so an arm reordered or dropped in one
/// copy would have listed an edge the run time refuses with every listing test green (PB5965). The containment tree is a parameter:
/// each side has its own.</para>
/// </summary>
public static class ProgramNameScope
{
    /// <summary>The first program arms 1 to 3 reach from <paramref name="caller"/> that <paramref name="accept"/> takes, in the arms'
    /// order, or null. <paramref name="accept"/> sees each candidate with the arm that reached it, so a reader that asks only some arms
    /// (AS NESTED asks 1 and 3) filters by arm, and an enumerating reader (one that collects every candidate) returns false throughout.</summary>
    /// <param name="childrenOf">The programs directly contained in a program.</param>
    /// <param name="containerOf">A program's directly containing program, or null for an outermost program.</param>
    /// <param name="isRecursive">Whether a program has the recursive attribute.</param>
    /// <param name="isCommon">Whether a program has the common attribute.</param>
    public static T? FirstVisible<T>(T caller, Func<T, ProgramNameArm, bool> accept, Func<T, IEnumerable<T>> childrenOf,
        Func<T, T?> containerOf, Func<T, bool> isRecursive, Func<T, bool> isCommon) where T : class
    {
        foreach (var child in childrenOf(caller))                                   // arm 1
            if (accept(child, ProgramNameArm.Contained)) return child;
        if (isRecursive(caller) && accept(caller, ProgramNameArm.RecursiveSelf)) return caller;   // arm 2
        for (var container = containerOf(caller); container is not null; container = containerOf(container))   // arm 3, nearest first
            foreach (var sibling in childrenOf(container))
                if (isCommon(sibling) && CommonProgramReferable(sibling, isRecursive(sibling), caller, containerOf)
                    && accept(sibling, ProgramNameArm.Common))
                    return sibling;
        return null;
    }

    /// <summary>May <paramref name="caller"/> reference the COMMON program <paramref name="common"/> — which the
    /// caller's containment chain already reaches through a container of <paramref name="common"/> — under §2023:8.4.6.3
    /// 2)? True unless the caller IS the common program or is contained (directly or indirectly) within it, in which
    /// case only a RECURSIVE common program is referable.</summary>
    /// <param name="containerOf">The caller side's own containment edge: a program's directly containing program,
    /// or null for an outermost program.</param>
    public static bool CommonProgramReferable<T>(T common, bool commonIsRecursive, T caller, Func<T, T?> containerOf)
        where T : class
    {
        if (commonIsRecursive) return true;
        for (T? p = caller; p is not null; p = containerOf(p))
            if (ReferenceEquals(p, common)) return false;   // the common program itself, or a program within it
        return true;
    }
}
