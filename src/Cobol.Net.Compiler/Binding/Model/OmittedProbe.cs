// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ <b>THE ONE PRESENCE FACT OF A FORMAL PARAMETER</b> — how the activated element learns whether the
/// argument corresponding to a formal was omitted (ISO §8.8.4.8.4 GR1: the OMITTED phrase was written, the
/// argument was a trailing one omitted from the activating statement, or it is itself a formal parameter for
/// which the omitted-argument condition is true).
/// <para>Two activation ABIs carry it, and they carry it differently (kb/Work PB757):</para>
/// <list type="bullet">
/// <item><see cref="Carrier"/> — a PROGRAM / FUNCTION formal crosses as a <c>ManagedPointer&lt;T&gt;</c> carrier
/// (COBOLNET_INTERPROGRAM_DESIGN D1/D2); the omitted argument is the NULL carrier (§14.9.4.4 GR11).</item>
/// <item><see cref="MethodFlag"/> — a METHOD formal crosses as a typed <c>ref T</c> parameter (COBOLNET_OO_DESIGN
/// D6), which has no omitted state of its own, so every formal is paired with a <c>bool</c> presence parameter
/// that is TRUE when the argument was omitted (§14.9.23.4 GR9).</item>
/// </list>
/// <para>Every consumer — the §8.8.4.8 condition, and the §8.8.4.8.4 GR1c forwarding of a formal as an argument
/// by CALL and by INVOKE — reads THIS record, so a third activation ABI is one new case here and a compile error
/// at every consumer's switch rather than a silently missing arm.</para>
/// </summary>
public abstract record OmittedProbe
{
    private OmittedProbe() { }

    /// <summary>The program/function arm: the formal's <c>ManagedPointer</c> carrier field (<c>__lnkpN</c>).</summary>
    public sealed record Carrier(string CarrierField) : OmittedProbe;

    /// <summary>The method arm: the name of the formal's <c>bool</c> omitted-presence parameter.</summary>
    public sealed record MethodFlag(string FlagParam) : OmittedProbe;
}
