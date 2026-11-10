// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The five statements whose FROM or INTO operand may not be the file's own record area (kb/Work PB5077).</summary>
public enum RecordAreaVerb { Read, Return, Write, Rewrite, Release }

/// <summary>
/// ⛔ <b>THE ONE WORDING AND SCOPE OF "THE OPERAND SHALL NOT BE THE FILE'S RECORD AREA"</b> (kb/Work PB5077). X3.23-1985 and
/// ISO/IEC 1989:2002 print it as a syntax rule of five statements — READ and RETURN: "The storage area associated with
/// identifier-1 and the record area associated with file-name-1 must not be the same storage area" (§1985:IX.4.5.3 SR1,
/// §1985:XI.4.3.3 SR1; §2002:14.8.29.2 SR2, §2002:14.8.32.2 SR1); WRITE, REWRITE and RELEASE: "Record-name-1 and identifier-1 must
/// not reference the same storage area" (§1985:IX.4.9.3 SR1, §1985:IX.4.6.3 SR1, §1985:XI.4.2.3 SR3; §2002:14.8.47.2 SR4,
/// §2002:14.8.33.2 SR1, §2002:14.8.30.2 SR2). The 1985 text repeats the READ, WRITE and REWRITE rules in each I-O module
/// (sequential VII, relative VIII, indexed IX), so the citation depends on the file's organization. COBOL-2023 prints no such
/// rule: its overlapping-operand rules (§2023:14.6.10, §2023:14.9.30.4 NOTE 1) leave the result undefined and the program legal,
/// so the rule ends at <c>Year &lt; 2023</c>. The decision it guards is <see cref="StorageSharing.IsRecordAreaOf"/>, asked by
/// <c>StatementValidation.CheckPhraseOperandIsNotRecordArea</c> from the one MOVE binder that binds all five phrases.
/// </summary>
internal static class RecordAreaRule
{
    /// <summary>Whether the edition carries the rule: every edition before COBOL-2023.</summary>
    public static bool InForce(int editionYear) => editionYear < 2023;

    /// <summary>The syntax rule that forbids the operand for <paramref name="verb"/> on a file of <paramref name="organization"/>
    /// at <paramref name="editionYear"/>, with the sentence it prints — the 1985 wording uses "must", the 2002 one "shall".</summary>
    public static (string Cite, string Words) Describe(RecordAreaVerb verb, FileOrganization organization, int editionYear)
    {
        bool is1985 = editionYear < 2002;
        string must = is1985 ? "must" : "shall";
        string words = verb is RecordAreaVerb.Read or RecordAreaVerb.Return
            ? $"The storage area associated with identifier-1 and the record area associated with file-name-1 {must} not be the "
              + "same storage area."
            : $"Record-name-1 and identifier-1 {must} not reference the same storage area.";
        return (is1985 ? Cite1985(verb, organization) : Cite2002(verb), words);
    }

    private static string Cite1985(RecordAreaVerb verb, FileOrganization organization)
    {
        // The module of the organization: sequential I-O is chapter VII, relative VIII, indexed IX; the sort-merge
        // module (XI) is organization-free. LINE SEQUENTIAL is no X3.23-1985 organization and reads as sequential.
        string module = organization switch { FileOrganization.Relative => "VIII", FileOrganization.Indexed => "IX", _ => "VII" };
        return verb switch
        {
            RecordAreaVerb.Read => $"§1985:{module}.4.{(module == "VII" ? 4 : 5)}.3 SR1",
            RecordAreaVerb.Write => $"§1985:{module}.4.{(module == "VII" ? 7 : 9)}.3 SR1",
            RecordAreaVerb.Rewrite => $"§1985:{module}.4.{(module == "VII" ? 5 : 6)}.3 SR1",
            RecordAreaVerb.Release => "§1985:XI.4.2.3 SR3",
            RecordAreaVerb.Return => "§1985:XI.4.3.3 SR1",
            _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null),
        };
    }

    private static string Cite2002(RecordAreaVerb verb) => verb switch
    {
        RecordAreaVerb.Read => "§2002:14.8.29.2 SR2",
        RecordAreaVerb.Release => "§2002:14.8.30.2 SR2",
        RecordAreaVerb.Return => "§2002:14.8.32.2 SR1",
        RecordAreaVerb.Rewrite => "§2002:14.8.33.2 SR1",
        RecordAreaVerb.Write => "§2002:14.8.47.2 SR4",
        _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null),
    };
}
