// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// An implementor-defined fatal stop that raises NO exception condition — the §14.6.13.1.1 NOTE 3
/// undefined-results latitude, exercised where continuing would be worse than stopping.
///
/// <para><b>Why a distinct type, and why it carries no EC name.</b> Every other runtime exception here names
/// a condition — <see cref="CobolFatalException"/> takes one, <c>CobolCallException</c> defaults to
/// EC-PROGRAM-IMP, <c>CobolSizeError</c> to EC-SIZE-OVERFLOW — and the emitted statement guards select on
/// exactly that name (<c>catch (CobolFatalException e) when (e.EcName == …)</c>). So a stop that must NOT
/// attribute a condition cannot reuse any of them, and <see cref="CobolFatalException"/> with an empty name
/// would silently defeat the guard's match rather than express the intent.</para>
///
/// <para><b>The case that forced it</b> is §14.9.23.4 GR7c: EC-OO-UNIVERSAL "is set to exist IF CHECKING FOR
/// IT IS ENABLED IN BOTH the activated method and the activating runtime element". When it is not enabled in
/// both, no exception condition exists — yet a nonconforming universal INVOKE still cannot be allowed to
/// cross into typed-native code with mismatched descriptors. This type is that stop: loud, greppable, and
/// unmistakably not a raised condition.</para>
/// </summary>
public sealed class CobolImplementorFatalException(string detail) : Exception(detail);
