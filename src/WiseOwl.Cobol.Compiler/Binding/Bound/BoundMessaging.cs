// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Binding.Bound;

// The statements of the asynchronous messaging facility (ISO/IEC 1989:2023 §14.9.38 SEND and §14.9.31 RECEIVE; DESIGN-message-control.md §4.3;
// kb/Work PB4743). SEND's two general formats are two nodes because they are two operations: Format 1 starts an exchange with a server and
// receives a server tag, Format 2 answers a requestor whose tag this run unit holds. Each node carries its [NOT] ON EXCEPTION phrases as bound
// statement lists, so the generated StatementChildren walk reaches them.

/// <summary>SEND Format 1, to-message-server (§14.9.38.2): <c>SEND [TO] { literal-1 | message-server-name-1 } FROM identifier-1 RETURNING data-name-1</c>.
/// The server is named once: <see cref="ServerName"/> is message-server-name-1 as written (§14.9.38.3 SR2: "Message-server-name-1 is the name of a
/// message server run unit", no data item), <see cref="ServerLiteral"/> is literal-1 (SR1: alphanumeric or national).</summary>
public sealed record BoundSendToServer(
    string? ServerName, BoundOperand? ServerLiteral, Place From, Place Returning,
    IReadOnlyList<BoundStatement>? OnException, IReadOnlyList<BoundStatement>? NotOnException) : BoundStatement;

/// <summary>SEND Format 2, message-server-response (§14.9.38.2): <c>SEND [TO] data-name-2 FROM identifier-1 [RAISING { EXCEPTION exception-name-1 | LAST
/// [EXCEPTION] }]</c>. <see cref="To"/> holds the requestor's tag (§14.9.38.4 GR7); <see cref="Raising"/> is the written exception-name, and
/// <see cref="RaisingLast"/> the LAST phrase (GR8: "exception-name-1 or the name of the last exception raised in the current run unit is sent to the
/// requestor program"); neither written sends no exception.</summary>
public sealed record BoundSendResponse(
    Place To, Place From, EcInfo? Raising, bool RaisingLast,
    IReadOnlyList<BoundStatement>? OnException, IReadOnlyList<BoundStatement>? NotOnException) : BoundStatement;

/// <summary>How long a RECEIVE waits for a message (§14.9.31.2's CONTINUE phrase, §14.9.31.4 GR2): <see cref="Forever"/> when no CONTINUE phrase is
/// written, <see cref="Seconds"/> for <c>CONTINUE AFTER arithmetic-expression-1 [SECONDS]</c>, and <see cref="Poll"/> for <c>CONTINUE AFTER MESSAGE
/// RECEIVED</c>, which completes the statement as soon as the MCS reports that no message is present.</summary>
public enum ReceiveWait { Forever, Seconds, Poll }

/// <summary>RECEIVE (§14.9.31.2): <c>RECEIVE [FROM] data-name-1 GIVING identifier-1 data-name-2 [CONTINUE …]</c>. <see cref="From"/> is the message tag
/// (NULL waits for any requestor, GR1 a), <see cref="Into"/> the message's receiving area, <see cref="Length"/> the integer item that takes the message's
/// length (GR3), and <see cref="Seconds"/> the interval expression when <see cref="Wait"/> is <see cref="ReceiveWait.Seconds"/>.</summary>
public sealed record BoundReceiveMessage(
    Place From, Place Into, Place Length, ReceiveWait Wait, BoundExpr? Seconds,
    IReadOnlyList<BoundStatement>? OnException, IReadOnlyList<BoundStatement>? NotOnException) : BoundStatement;
