// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE FUNNEL FOR RESOLVING A WRITTEN EXCEPTION-NAME (kb/Work R05). Before it, the unknown-name
/// diagnostic (COBOLNET0711) and the introduction gate (COBOLNET0878) existed as FOUR verbatim copies each —
/// &gt;&gt;TURN (TurnState), the Format-3 USE builder (ProcedureTableBuilder), the exception-checking PERFORM's
/// WHEN list (EcBinder.ExceptionPerform), and RAISE / EXIT–GOBACK RAISING (EcBinder) — which is exactly the
/// shape under which R05's §15.33 width advisory would have been added to one arm of six
/// (feedback_two_arm_dispatch; feedback_one_rule_one_place: the extraction IS the fix).
///
/// <para>The RAISING-phrase sites (PROCEDURE DIVISION header §14.2.2 SR7, METHOD-ID — EcAddPdRaisingWord and
/// DataBinder.Oo) resolve through <see cref="ExceptionCatalog.TryGet"/> themselves because an unresolved word
/// may legally be a CLASS name there (SR8/SR9) — they call <see cref="Advise"/> directly on the names they
/// accept. <c>EcNameResolutionDriftTests</c> derives the caller list from the source, so a seventh resolution
/// site cannot appear un-funneled and un-advised.</para>
/// </summary>
internal static class EcNameResolution
{
    /// <summary>Resolve a written exception-name: COBOLNET0711 when it is neither in the §14.6.13.1 catalog nor
    /// a valid EC-USER-/EC-IMP- open-family name; optionally COBOLNET0710 when a level-1/-2 name stands where
    /// only level-3 is legal (the RAISE/RAISING contexts, §14.9.29.3 SR1 — checked BEFORE the introduction gate
    /// so the level error keeps priority for a level-2 name of a later family); COBOLNET0878 when the name's
    /// family postdates the targeted edition. Otherwise <see cref="Advise"/> decides: it carries the §15.33
    /// width advisory AND the EC-SCREEN refusal (COBOLNET1707), and returns false when it refused.</summary>
    public static bool TryResolve(EditionContext edition, string raw, string where, out EcInfo info,
        bool requireLevel3 = false)
    {
        if (!ExceptionCatalog.TryGet(raw, out info))
        {
            edition.Error(DiagnosticCatalog.EcNameUnknown, $"{where}: '{raw}' is not an exception-name of "
                + "ISO/IEC 1989 §14.6.13.1 (and not a valid EC-USER-/EC-IMP- name)");
            return false;
        }
        // The EC-LOCALE family (Annex A.4.9 item 1) WAS refused here by name while the locale module was documented
        // non-support (kb/Work PB100) and is LEGAL again since kb/Work PB64 T1 adopted the module: EC-LOCALE-MISSING,
        // EC-LOCALE-INVALID-PTR and EC-LOCALE-INCOMPATIBLE now have live raise sites (SET LOCALE §14.9.39.4 GR24 / GR21;
        // the IS LOCALE sequence §8.8.4.2.11), so a program can >>TURN them on, name them in a USE declarative, or match
        // them in a WHEN phrase — as EC-ORDER-NOT-SUPPORTED has been since PB101 T7. Refusing a name whose condition
        // this compiler raises would make the condition unobservable.
        // ⚠ THE EC-SCREEN FAMILY IS THE SAME SHAPE IN THE OTHER DIRECTION and lives in Advise (kb/Work PB260):
        // Annex A.4.2 is still Not claimed, those four names have no raise site and no reader, and accepting
        // them made a >>TURN or RAISE compile against a facility that does not exist. When the screen module is
        // ever claimed, delete that branch exactly as PB64 T1 deleted this one — the two are one rule.
        if (requireLevel3 && info.Level != 3)
        {
            edition.Error("COBOLNET0710", $"{where}: exception-name '{info.Name}' is a level-{info.Level} "
                + "name; only a LEVEL-3 exception-name may be raised (ISO §14.9.29.3 SR1)");
            return false;
        }
        if (info.IntroducedIn > edition.DialectLevel)
        {
            edition.Error(DiagnosticCatalog.EcNameIntroducedLater, $"{where}: exception-name {info.Name} was "
                + $"introduced by ISO/IEC 1989:{info.IntroducedIn} — it requires --std {info.IntroducedIn} or "
                + $"later (targeting COBOL-{edition.DialectLevel})");
            return false;
        }
        return Advise(edition, info, where);
    }

    /// <summary>True for the EC-SCREEN family (the level-2 name and its four level-3 names) — the exception
    /// half of Annex A.4.2, which docs/CONFORMANCE.md §5 records as Not claimed. Written as ONE predicate over
    /// the catalog's own hierarchy rather than a name list, so a level-3 EC-SCREEN name added to Table 13 is
    /// covered without editing this file.</summary>
    private static bool IsDeclinedScreenName(EcInfo info) =>
        info.Name.Equals("EC-SCREEN", StringComparison.OrdinalIgnoreCase)
        || ExceptionCatalog.UnderLevel2(info.Name, "EC-SCREEN");

    /// <summary>The §15.33 width advisory (COBOLNET1636, Warning — legal source stays legal): §15.33.3 r1 fixes
    /// FUNCTION EXCEPTION-STATUS's value at 31 characters while COBOL-2023 words run to 63 (§8.3.2.1) and the
    /// §14.6.13.1.1 open-family suffixes are unbounded, so a level-3 name of 32..63 characters is LEGAL and
    /// indistinguishable from anything sharing its first 31 characters through that ONE function — checking,
    /// declarative selection and WHEN matching all use the full name (ExceptionState.LastName). The r1 width is
    /// implemented as written (EcFunctions.Status); this makes the collision visible at compile time instead of
    /// a documented-nowhere truncation (Phase-B F6). Below 2023 the word itself cannot reach here (the 31-char
    /// COBOL-2002 word limit rejects it first), so no edition guard is needed. One advisory per spelling —
    /// the same name legitimately appears at TURN + RAISE + USE in one program.</summary>
    /// <returns><see langword="false"/> when the name was REFUSED (today: the EC-SCREEN family) — every caller
    /// must treat that as an unresolved name.</returns>
    public static bool Advise(EditionContext edition, EcInfo info, string where = "exception-name")
    {
        // ⛔ THE EC-SCREEN REFUSAL LIVES HERE, NOT AT THE SIX WRITING SITES (Annex A.4.2 item 10 names all six:
        // the RAISING phrases of EXIT and GOBACK, the RAISING phrase of the procedure division header, the USE
        // statement, the WHEN phrase of PERFORM, RAISE, and the TURN directive). Putting it in the resolution
        // funnel is what makes it cover all of them at once — and the two RAISING sites that call Advise
        // DIRECTLY (EcAddPdRaisingWord, DataBinder.Oo) are exactly the arms a per-site check would have missed
        // (feedback_two_arm_dispatch). This is the shape the EC-LOCALE family had while A.4.9 was declined
        // (kb/Work PB100), removed when PB64 T1 claimed that module and gave its names real raise sites.
        if (IsDeclinedScreenName(info))
        {
            ScreenFacility.ReportExceptionName(edition, info.Name, where);
            return false;
        }
        if (info.Level != 3 || info.Name.Length <= 31) return true;
        if (edition.Warnings.Any(w =>
                w.Contains(DiagnosticCatalog.EcNameWiderThanStatus.Code) && w.Contains(info.Name))) return true;
        edition.Warning(DiagnosticCatalog.EcNameWiderThanStatus,
            $"exception-name {info.Name} is {info.Name.Length} characters long; FUNCTION EXCEPTION-STATUS "
            + "returns a 31-character value (ISO §15.33.3 r1), so this name and any other sharing its first 31 "
            + "characters are indistinguishable through that function (checking and declarative selection use "
            + "the full name)");
        return true;
    }
}
