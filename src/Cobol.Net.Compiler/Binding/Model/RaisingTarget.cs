// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>Which of the three alternatives of the procedure-division-header RAISING phrase (ISO §14.2.1) one
/// element took:
/// <code>
/// RAISING { exception-name-1                   } …
///         { [ FACTORY OF ] object-class-name-1 }
///         { interface-name-1                   }
/// </code>
/// (measured off the printed diagram, PDF page 557 / printed folio 527 — FACTORY underlined, OF not).</summary>
public enum RaisingTargetKind
{
    /// <summary>exception-name-1 — §14.2.2 SR7: "a level-3 exception-name for EC-USER".</summary>
    ExceptionName,

    /// <summary>[FACTORY OF] object-class-name-1 — §14.2.2 SR8: "the name of a class specified in the REPOSITORY
    /// paragraph".</summary>
    ObjectClass,

    /// <summary>interface-name-1 — §14.2.2 SR9: "the name of an interface specified in the REPOSITORY
    /// paragraph". Its alternative carries no FACTORY phrase.</summary>
    Interface,
}

/// <summary>
/// ⛔ ONE ELEMENT OF THE RAISING PHRASE, AS A TUPLE (kb/Work PB815/PB814) — kind × name × FACTORY, the axes
/// §14.2.1 prints. It replaced two parallel string lists (EC names, class names) that could not say FACTORY and
/// had no interface slot at all, so the rules that CONSUME the phrase had no expressible subject:
/// §14.9.18.3 SR4 a)/c) and §14.9.14.3 SR5 a)/c) compare "the presence or absence of the FACTORY phrase … in the
/// data description entry of identifier-1 [and] in the RAISING phrase", and SR4 b)/SR5 b) ask whether an
/// interface "conform[s] to an interface specified in the RAISING phrase". The same tuple
/// <see cref="ObjectRefDescriptor"/> gives the identifier side, so the comparison is flag-to-flag.
/// </summary>
/// <param name="Kind">Which alternative was written.</param>
/// <param name="Name">The name as written, uppercased (COBOL words are case-insensitive, §8.3.2.2) — for an
/// <see cref="RaisingTargetKind.ExceptionName"/> the catalog name.</param>
/// <param name="Factory">The FACTORY phrase — only ever true for <see cref="RaisingTargetKind.ObjectClass"/>.</param>
public readonly record struct RaisingTarget(RaisingTargetKind Kind, string Name, bool Factory)
{
    /// <summary>How the element reads back in a diagnostic — the phrase as written.</summary>
    public string Spelled => Kind switch
    {
        RaisingTargetKind.ObjectClass => (Factory ? "FACTORY OF " : "") + Name,
        _ => Name,
    };
}
