// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Runtime.IO;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ <b>THE ONE ENFORCEMENT OF COBOL-85's MANDATORY EXCEPTION PHRASE</b> (kb/Work PB5076). Every I-O module of
/// X3.23-1985 closes the syntax rules of READ, WRITE, REWRITE, DELETE and START with one rule of the same shape — "The
/// INVALID KEY phrase or the AT END phrase must be specified if no applicable USE AFTER STANDARD EXCEPTION procedure is
/// specified for file-name-1" (§1985:IX.4.5.3 SR7) — and qualifies it for REWRITE and DELETE by the access mode
/// (§1985:VIII.4.6.3 SR4, §1985:VIII.4.3.3 SR2). COBOL-2002 and later print no such rule (§2023:14.9.30.4 GR24 d)
/// sends control to the end of the statement when neither a phrase nor a declarative takes the exception), so the
/// requirement is the 1985 dialect's alone.
/// <para><b>The rule has two halves and this type owns both.</b> WHICH statements need the phrase is the rule's
/// own scope: where the phrase CAN be written (<see cref="IoPhraseAdmission.AdmitsInvalidKey"/>, the question
/// FLAG-14 d asks of the same statements) and, for a sequential file's READ, the AT END phrase. WHEN a procedure is
/// "applicable" is the USE statement's scope (§1985:VII.4.6.4 GR5 a to e: the procedure runs for the named file, or for any
/// file open in the named mode): a declarative that names the file, or one whose open mode (INPUT, OUTPUT, I-O, EXTEND)
/// the file can be in when the verb executes — a READ or START
/// runs on a file open INPUT or I-O, a WRITE on one open OUTPUT, I-O or EXTEND, a REWRITE or DELETE on one open I-O.
/// A declarative of a containing program counts too: "Only a declarative within the separately compiled program that contains the statement" is invoked (§1985:VII.4.6.4 GR1), and a contained program is part of its container's separately compiled program. The test is the most permissive reading of "applicable"
/// that is still static: a procedure that no execution of the verb can reach is not applicable.</para>
/// <para>One instance per bound unit (<see cref="BinderContext.ExceptionPhrases"/>); every verb binder of the keyed and
/// sequential arms calls <see cref="Require"/> with the phrase fact its own parse tree spells.</para>
/// </summary>
internal sealed class ExceptionPhraseRequirement(BinderContext ctx)
{
    /// <summary>The editions that carry the rule: COBOL-85 only (<c>Year &lt; 2002</c>).</summary>
    private bool InForce => ctx.Edition.Edition.Year < 2002;

    /// <summary>Refuse a statement that omits its mandatory exception phrase. Reports and returns false when the rule is
    /// violated; true otherwise (a screen, not a branch: the caller keeps binding).</summary>
    /// <param name="file">The file the statement names.</param>
    /// <param name="verb">The statement.</param>
    /// <param name="phrases">The statement's conditional phrases. The rule is met by the AT END phrase of a sequential READ;
    /// by the INVALID KEY or the AT END phrase of a relative or indexed READ; by the INVALID KEY phrase of the others. A NOT
    /// phrase alone is not "the phrase": §1985 prints no NOT forms, and COBOL-2002's NOT twins are phrases of their own.</param>
    public bool Require(FileModel file, InvalidKeyVerb verb, Core.ConditionalPhrasesContext? phrases)
    {
        if (!InForce || Written(file, verb, phrases)) return true;
        if (!Rule.TryGet(verb, file, out var rule) || HasApplicableUse(file, verb)) return true;
        ctx.Edition.Error(DiagnosticCatalog.IoExceptionPhraseRequired,
            $"{rule.Statement} of {file.Organization.ToString().ToLowerInvariant()} file '{file.CobolName}' specifies no "
            + $"{rule.Phrase} phrase, and no applicable USE AFTER STANDARD EXCEPTION procedure is specified for the file "
            + $"({rule.Cite}: \"{rule.Words}\")");
        return false;
    }

    /// <summary>Whether <paramref name="phrases"/> holds a phrase that satisfies the rule for <paramref name="verb"/> on
    /// <paramref name="file"/> (see <see cref="Require"/>).</summary>
    private static bool Written(FileModel file, InvalidKeyVerb verb, Core.ConditionalPhrasesContext? phrases)
    {
        bool atEnd = ConditionalPhrases.Find(phrases, new(ConditionalPhraseFamily.AtEnd, Negated: false)) is not null;
        bool invalidKey = ConditionalPhrases.Find(phrases, new(ConditionalPhraseFamily.InvalidKey, Negated: false)) is not null;
        return verb == InvalidKeyVerb.Read
            ? atEnd || (invalidKey && file.Organization != FileOrganization.Sequential)
            : invalidKey;
    }

    /// <summary>Does a USE declarative of this unit (or a containing program's) apply to
    /// <paramref name="verb"/> on <paramref name="file"/>: it names the file, or its open mode is one the file can be in
    /// when the verb executes.</summary>
    private bool HasApplicableUse(FileModel file, InvalidKeyVerb verb)
    {
        foreach (var d in ctx.Table.Declaratives)
            if (Covers(d, file, verb)) return true;
        if (ctx.ContainerUses is { } containers)
            foreach (var d in containers())
                if (Covers(d, file, verb)) return true;
        return false;

        static bool Covers(BoundDeclarative d, FileModel file, InvalidKeyVerb verb) =>
            d.Files.Any(f => ReferenceEquals(f, file))
            || (d.ModeIndex is { } mode && OpenModesOf(verb).Contains((FileOpenMode)mode));
    }

    /// <summary>The open modes a file can be in while <paramref name="verb"/> executes on it (§1985 OPEN: INPUT reads, OUTPUT and
    /// EXTEND write, I-O reads, writes, rewrites and deletes).</summary>
    private static FileOpenMode[] OpenModesOf(InvalidKeyVerb verb) => verb switch
    {
        InvalidKeyVerb.Read or InvalidKeyVerb.Start => [FileOpenMode.Input, FileOpenMode.IO],
        InvalidKeyVerb.Write => [FileOpenMode.Output, FileOpenMode.IO, FileOpenMode.Extend],
        _ => [FileOpenMode.IO],   // REWRITE, DELETE
    };

    /// <summary>One row of the rule: the statement's name, the phrase the rule names, and the 1985 syntax rule that words
    /// it for this organization.</summary>
    private sealed record Rule(string Statement, string Phrase, string Cite, string Words)
    {
        private const string UseIfNone = "if no applicable USE AFTER STANDARD EXCEPTION procedure is specified for file-name-1";

        /// <summary>The row for <paramref name="verb"/> on <paramref name="file"/>, or false where the 1985 module prints no
        /// requirement: a WRITE, REWRITE or START of a sequential file (no invalid-key condition exists), a REWRITE or
        /// DELETE in sequential access mode (the phrase cannot be written, <see cref="IoPhraseAdmission.AdmitsInvalidKey"/>),
        /// and LINE SEQUENTIAL, which X3.23-1985 does not define.</summary>
        public static bool TryGet(InvalidKeyVerb verb, FileModel file, out Rule rule)
        {
            rule = null!;
            var org = file.Organization;
            if (org is FileOrganization.LineSequential) return false;
            if (verb != InvalidKeyVerb.Read && !IoPhraseAdmission.AdmitsInvalidKey(verb, file)) return false;
            bool indexed = org == FileOrganization.Indexed;
            rule = verb switch
            {
                InvalidKeyVerb.Read when org == FileOrganization.Sequential => new("READ", "AT END", "§1985:VII.4.4.3 SR2",
                    $"The AT END phrase must be specified {UseIfNone}."),
                InvalidKeyVerb.Read => new("READ", "INVALID KEY or AT END", indexed ? "§1985:IX.4.5.3 SR7" : "§1985:VIII.4.5.3 SR5",
                    $"The INVALID KEY phrase or the AT END phrase must be specified {UseIfNone}."),
                InvalidKeyVerb.Write => new("WRITE", "INVALID KEY", indexed ? "§1985:IX.4.9.3 SR3" : "§1985:VIII.4.9.3 SR3",
                    "The INVALID KEY phrase must be specified if an applicable USE AFTER STANDARD EXCEPTION procedure is not "
                    + "specified for the associated file-name."),
                InvalidKeyVerb.Rewrite => new("REWRITE", "INVALID KEY", indexed ? "§1985:IX.4.6.3 SR3" : "§1985:VIII.4.6.3 SR4",
                    indexed
                        ? "The INVALID KEY phrase must be specified in the REWRITE statement for indexed files for which an "
                          + "appropriate USE AFTER STANDARD EXCEPTION procedure is not specified."
                        : "The INVALID KEY phrase must be specified in the REWRITE statement for relative files in the random "
                          + "or dynamic access mode for which an appropriate USE AFTER STANDARD EXCEPTION procedure is not specified."),
                InvalidKeyVerb.Delete => new("DELETE", "INVALID KEY", indexed ? "§1985:IX.4.3.3 SR2" : "§1985:VIII.4.3.3 SR2",
                    "The INVALID KEY phrase must be specified for a DELETE statement which references a file which is not in "
                    + "sequential access mode and for which an applicable USE AFTER STANDARD EXCEPTION procedure is not specified."),
                InvalidKeyVerb.Start => new("START", "INVALID KEY", indexed ? "§1985:IX.4.7.3 SR3" : "§1985:VIII.4.7.3 SR3",
                    $"The INVALID KEY phrase must be specified {UseIfNone}."),
                _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null),
            };
            return true;
        }
    }
}
