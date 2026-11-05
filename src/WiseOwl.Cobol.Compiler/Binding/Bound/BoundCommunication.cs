// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime.Communication;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>The statements of the communication facility (ISO/IEC 1989:2002 Annex A.2).</summary>
public enum CommunicationVerb
{
    AcceptMessageCount,
    Enable,
    Disable,
    Purge,
    Receive,
    Send,
}

/// <summary>One statement of the communication facility (kb/Work PB4677). They are one node because they are one operation: hand the MCS the CD's record area and store back
/// what it leaves there, every one reading and writing <see cref="Area"/>, the CD's implicit description; the optional members are the operands each statement's format prints.</summary>
public sealed record BoundCommunicationStatement(CommunicationVerb Verb, CommunicationDescription Cd, Place Area) : BoundStatement
{
    /// <summary>ENABLE and DISABLE: the path the statement names.</summary>
    public CommunicationPath Path { get; init; }

    /// <summary>ENABLE and DISABLE at COBOL-85: the KEY phrase's operand.</summary>
    public BoundOperand? Key { get; init; }

    /// <summary>SEND: identifier-1, the text the text length measures.</summary>
    public BoundOperand? Text { get; init; }

    /// <summary>SEND: the written ESI, EMI or EGI, when <see cref="IndicatorItem"/> does not decide it at run time.</summary>
    public SendIndicator Indicator { get; init; }

    /// <summary>SEND: identifier-2 (ISO/IEC 1989:2002 Table A.3).</summary>
    public BoundOperand? IndicatorItem { get; init; }

    /// <summary>RECEIVE: the SEGMENT phrase is written rather than MESSAGE.</summary>
    public bool Segment { get; init; }

    /// <summary>RECEIVE: identifier-1, the receiving area.</summary>
    public Place? Into { get; init; }

    /// <summary>RECEIVE: imperative-statement-1 of the NO DATA phrase; its presence decides whether the statement waits for data (A.2.5.3 GR5).</summary>
    public IReadOnlyList<BoundStatement>? NoData { get; init; }

    /// <summary>RECEIVE: imperative-statement-2 of the WITH DATA phrase.</summary>
    public IReadOnlyList<BoundStatement>? WithData { get; init; }
}
