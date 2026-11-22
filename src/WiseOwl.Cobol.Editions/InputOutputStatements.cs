// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Editions;

/// <summary>The statement forms an edition's text can name as INPUT-OUTPUT STATEMENTS (kb/Work PB6032): one member per form a
/// definition lists, so each edition's set is a value of this type (<see cref="InputOutputStatementSet"/>). A form is the verb, or
/// the verb with the phrase the 1985 definition qualifies it by; the compiler sorts a statement into its form in one place
/// (<c>WiseOwl.Cobol.Binding.InputOutputStatementForms</c>).</summary>
[Flags]
public enum IoStatementForms
{
    None = 0,
    /// <summary>ACCEPT with the identifier phrase: the nucleus formats (<c>ACCEPT identifier-1 [FROM mnemonic-name-1]</c> and
    /// <c>ACCEPT identifier-2 FROM DATE|DAY|DAY-OF-WEEK|TIME</c>, §1985:VI.6.5), never the communication module's
    /// <c>ACCEPT cd-name-1 MESSAGE COUNT</c> (§1985:XIV.3.1), whose operand is a cd-name.</summary>
    AcceptIdentifier = 1 << 0,
    Close = 1 << 1,
    /// <summary>DELETE: the record statement, and from 2023 the DELETE FILE statement too (§2023:9.1.13.1 names "DELETE").</summary>
    Delete = 1 << 2,
    Disable = 1 << 3,
    Display = 1 << 4,
    Enable = 1 << 5,
    Open = 1 << 6,
    Purge = 1 << 7,
    Read = 1 << 8,
    /// <summary>The communication module's RECEIVE (§1985:XIV.3.5), not the 2023 message-control RECEIVE.</summary>
    Receive = 1 << 9,
    Rewrite = 1 << 10,
    /// <summary>The communication module's SEND (§1985:XIV.3.6), not the 2023 message-control SEND.</summary>
    Send = 1 << 11,
    /// <summary>SET with the TO ON or TO OFF phrase: the switch format (§1985:VI.6.23 format 3).</summary>
    SetSwitch = 1 << 12,
    Start = 1 << 13,
    Unlock = 1 << 14,
    Write = 1 << 15,
}

/// <summary>⛔ THE ONE HOME OF "WHICH STATEMENTS ARE INPUT-OUTPUT STATEMENTS" AT EACH EDITION (kb/Work PB6032), as data: one value
/// per edition, each derived from that edition's own text, so a rule that names the term asks <see cref="Of"/> and never spells a
/// list of its own. <c>InputOutputStatementSetDriftTests</c> re-reads each sentence below from the transcriptions on every run.
/// <list type="bullet">
/// <item>COBOL-85 DEFINES the term: "The input-output statements are: ACCEPT (with the identifier phrase), CLOSE, DELETE,
/// DISABLE, DISPLAY, ENABLE, OPEN, PURGE, READ, RECEIVE, REWRITE, SEND, SET (with the TO ON or TO OFF phrase), START, and WRITE"
/// (§1985:III.2 p139). The fifteen are the statements of the I-O modules (sequential §1985:VII.4, relative §1985:VIII.4, indexed
/// §1985:IX.4), of the communication module (§1985:XIV.3) less its ACCEPT MESSAGE COUNT, and the nucleus's ACCEPT, DISPLAY and
/// switch SET (§1985:VI.6.5, §1985:VI.6.10, §1985:VI.6.23).</item>
/// <item>COBOL-2002, 2014 and 2023 define no such list; the statements their text calls input-output statements are the ones whose
/// execution sets the I-O status, "a CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK or WRITE statement" (§2002:9.1.12,
/// §2014:9.1.12, §2023:9.1.13.1). Each edition keeps its own value though the three are equal today.</item>
/// </list></summary>
public static class InputOutputStatementSet
{
    /// <summary>§1985:III.2 p139: the fifteen forms the 1985 definition lists.</summary>
    public const IoStatementForms Cobol85 =
        IoStatementForms.AcceptIdentifier | IoStatementForms.Close | IoStatementForms.Delete | IoStatementForms.Disable
        | IoStatementForms.Display | IoStatementForms.Enable | IoStatementForms.Open | IoStatementForms.Purge
        | IoStatementForms.Read | IoStatementForms.Receive | IoStatementForms.Rewrite | IoStatementForms.Send
        | IoStatementForms.SetSwitch | IoStatementForms.Start | IoStatementForms.Write;

    /// <summary>§2002:9.1.12: the eight statements whose execution sets the I-O status.</summary>
    public const IoStatementForms Cobol2002 = IoStatusStatements;

    /// <summary>§2014:9.1.12: the same eight.</summary>
    public const IoStatementForms Cobol2014 = IoStatusStatements;

    /// <summary>§2023:9.1.13.1: the same eight.</summary>
    public const IoStatementForms Cobol2023 = IoStatusStatements;

    private const IoStatementForms IoStatusStatements =
        IoStatementForms.Close | IoStatementForms.Delete | IoStatementForms.Open | IoStatementForms.Read
        | IoStatementForms.Rewrite | IoStatementForms.Start | IoStatementForms.Unlock | IoStatementForms.Write;

    /// <summary>The input-output statements of edition <paramref name="year"/> (85, 2002, 2014 or 2023).</summary>
    public static IoStatementForms Of(int year) => year switch
    {
        85 => Cobol85,
        2002 => Cobol2002,
        2014 => Cobol2014,
        2023 => Cobol2023,
        _ => throw new ArgumentOutOfRangeException(nameof(year), year, "edition must be 85/2002/2014/2023"),
    };
}
