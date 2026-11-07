// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE CANONICAL SIGNATURE IDENTITY AGREES WITH THE ONE SAME-SIGNATURE RELATION (DESIGN-external-repository §6.4, §17.2;
/// kb/Work PB2103, PB4491). What a restricted program- or function-pointer may CONTAIN is bounded by its prototype's
/// signature (ISO §2023:13.18.60.4 GR25: "the address of a program with the same signature as that identified by the specified
/// program-prototype-name"; GR26 its function twin), and the run-time relations compare the identity a description carries,
/// so <c>PrototypeSignatures.Identity</c> must name two prototypes alike exactly when
/// <c>PrototypeSignatures.Same</c> says they are the same — across compilation groups, because two separately compiled
/// modules' descriptions meet at run time. Two halves: every prototype of §17.2's corpus against every other (each corpus
/// case is its own compilation group, so most pairs cross groups), and a perturbation fixture — one variant per description
/// property <c>Same</c> reads, each of which must change the identity, and one per data-name and grouping change, none of
/// which may.
/// </summary>
public sealed class SignatureIdentityDriftTests
{
    private sealed record Seen(string Case, string Name, bool Function, CalleeSignature Signature, string Identity);

    private static string IdentityOf(BoundCompilation comp, BoundUnit u, CalleeSignature signature) =>
        PrototypeSignatures.Identity(u.IsFunction, signature);

    private static CalleeSignature SignatureOf(BoundUnit u) => new(u.Data.Linkage.Formals, u.Data.Linkage.Returning);

    /// <summary>Every prototype definition of the corpus (each case's group bound at every edition it is accepted at, the
    /// enumeration §17.2's round trip reads) against a representative of every identity met so far with its formal count
    /// and RETURNING presence (the relation is an equivalence, so agreeing with one member of a class is agreeing with the
    /// class): <c>Same</c> is true exactly when the identities are equal.</summary>
    [Fact]
    public void EveryPrototypeOfTheCorpus_HasTheSameIdentity_ExactlyWhenItHasTheSameSignature()
    {
        var cases = RepositoryRoundTripCorpusTests.Corpus()
            .Where(c => File.ReadAllText(c.Path).Contains("PROTOTYPE", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var seen = new ConcurrentBag<Seen>();
        var failures = new ConcurrentBag<string>();
        Parallel.ForEach(cases, c =>
        {
            try
            {
                var comp = RepositoryCodecTests.BindFile(c.Path, c.Edition, c.Format);
                foreach (var u in comp.Units.Where(u => u.IsPrototype && u.Parent is null))
                {
                    var signature = SignatureOf(u);
                    seen.Add(new($"{c.Edition}/{c.Id}", u.ExternalizedName, u.IsFunction, signature, IdentityOf(comp, u, signature)));
                }
            }
            catch (Exception e)
            {
                failures.Add($"{c.Edition}/{c.Id}: {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }
        });
        Assert.True(failures.IsEmpty, string.Join("\n", failures.Take(20)));
        Assert.True(seen.Count > 50, $"only {seen.Count} prototypes were found in {cases.Count} cases — the enumeration measured nothing");

        var representatives = new Dictionary<(bool, int, bool), List<Seen>>();
        int compared = 0;
        foreach (var s in seen.OrderBy(s => s.Case, StringComparer.Ordinal).ThenBy(s => s.Name, StringComparer.Ordinal))
        {
            Assert.StartsWith(PrototypeSignatures.CanonicalPrefix, s.Identity, StringComparison.Ordinal);
            // The key describes a pointer formal by its TO phrase, never by another identity, so an identity is a function
            // of the signature's text alone and the order the pointers are stamped in changes none (kb/Work PB4491).
            Assert.DoesNotContain(PrototypeSignatures.CanonicalPrefix, PrototypeSignatures.SignatureKey(s.Signature), StringComparison.Ordinal);
            var bucketKey = (s.Function, s.Signature.Formals.Count, s.Signature.Returning is null);
            if (!representatives.TryGetValue(bucketKey, out var bucket)) representatives[bucketKey] = bucket = [];
            bool joined = false;
            foreach (var r in bucket)
            {
                compared++;
                bool same = PrototypeSignatures.Same(r.Signature, s.Signature);
                bool equal = r.Identity == s.Identity;
                if (same != equal)
                    failures.Add($"{s.Case} {s.Name} and {r.Case} {r.Name}: Same is {same}, identities {(equal ? "equal" : "differ")}");
                joined |= equal;
            }
            if (!joined) bucket.Add(s);
        }
        Assert.True(failures.IsEmpty, $"{failures.Count} pair(s) where the identity and the relation disagree:\n  "
            + string.Join("\n  ", failures.Order(StringComparer.Ordinal).Take(40)));
        Assert.True(compared > 100, $"only {compared} pairs were compared");
    }

    private const string Perturbations = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PBASE IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 R PIC X(2).
               PROCEDURE DIVISION USING A G RETURNING R.
               END PROGRAM PBASE.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PMODE IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 R PIC X(2).
               PROCEDURE DIVISION USING BY VALUE A BY REFERENCE G RETURNING R.
               END PROGRAM PMODE.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. POPT IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 R PIC X(2).
               PROCEDURE DIVISION USING A OPTIONAL G RETURNING R.
               END PROGRAM POPT.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PPIC IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(5).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 R PIC X(2).
               PROCEDURE DIVISION USING A G RETURNING R.
               END PROGRAM PPIC.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PUSAGE IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4) USAGE BINARY.
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 R PIC X(2).
               PROCEDURE DIVISION USING A G RETURNING R.
               END PROGRAM PUSAGE.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PSIGN IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC S9(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 R PIC X(2).
               PROCEDURE DIVISION USING A G RETURNING R.
               END PROGRAM PSIGN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PJUST IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 R PIC X(2) JUSTIFIED RIGHT.
               PROCEDURE DIVISION USING A G RETURNING R.
               END PROGRAM PJUST.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PLAYOUT IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               01 G.
                  05 G2 PIC 9(2).
                  05 G1 PIC X(2).
               01 R PIC X(2).
               PROCEDURE DIVISION USING A G RETURNING R.
               END PROGRAM PLAYOUT.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PNORET IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               PROCEDURE DIVISION USING A G.
               END PROGRAM PNORET.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PRETPIC IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 R PIC X(3).
               PROCEDURE DIVISION USING A G RETURNING R.
               END PROGRAM PRETPIC.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PCOUNT IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               01 B PIC 9(4).
               01 R PIC X(2).
               PROCEDURE DIVISION USING A G B RETURNING R.
               END PROGRAM PCOUNT.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PPTRB IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM PBASE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO PBASE.
               01 P TYPE PT.
               PROCEDURE DIVISION USING P.
               END PROGRAM PPTRB.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PPTRM IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM PMODE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO PMODE.
               01 P TYPE PT.
               PROCEDURE DIVISION USING P.
               END PROGRAM PPTRM.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PSELF IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM PSELF.
               DATA DIVISION.
               LINKAGE SECTION.
               01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO PSELF.
               01 P TYPE PT.
               PROCEDURE DIVISION USING P.
               END PROGRAM PSELF.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PHOLD.
               PROCEDURE DIVISION.
                   GOBACK.
               END PROGRAM PHOLD.
               """;

    /// <summary>The same signatures as <see cref="Perturbations"/>' PBASE and PPTRB, compiled in ANOTHER group under other
    /// names, other data-names and another intermediate grouping (§2023:13.7.3 SR2 matches descriptions, not names; a group's
    /// description is its elementary items, §2023:8.5.3.1): QBASE and this group's own PBASE are PBASE's signature, QPTRB is
    /// restricted to PBASE through PPTRB's type-name PT, QPTRT to PBASE through another type-name, and QPTRQ to QBASE, ANOTHER
    /// prototype of that signature, through PT. QODD and QEVEN name each other.</summary>
    private const string Renamed = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. QBASE IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 QA PIC 9(4).
               01 QG.
                  05 QH.
                     10 QG1 PIC X(2).
                     10 QG2 PIC 9(2).
               01 QR PIC X(2).
               PROCEDURE DIVISION USING QA QG RETURNING QR.
               END PROGRAM QBASE.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PBASE IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 ZA PIC 9(4).
               01 ZG.
                  05 ZG1 PIC X(2).
                  05 ZH.
                     10 ZG2 PIC 9(2).
               01 ZR PIC X(2).
               PROCEDURE DIVISION USING ZA ZG RETURNING ZR.
               END PROGRAM PBASE.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. QPTRB IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM PBASE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO PBASE.
               01 QP TYPE PT.
               PROCEDURE DIVISION USING QP.
               END PROGRAM QPTRB.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. QPTRT IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM PBASE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 QPT IS TYPEDEF USAGE PROGRAM-POINTER TO PBASE.
               01 QP TYPE QPT.
               PROCEDURE DIVISION USING QP.
               END PROGRAM QPTRT.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. QPTRQ IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM QBASE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO QBASE.
               01 QP TYPE PT.
               PROCEDURE DIVISION USING QP.
               END PROGRAM QPTRQ.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. QODD IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM QEVEN.
               DATA DIVISION.
               LINKAGE SECTION.
               01 QPT IS TYPEDEF USAGE PROGRAM-POINTER TO QEVEN.
               01 QP TYPE QPT.
               PROCEDURE DIVISION USING QP.
               END PROGRAM QODD.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. QEVEN IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM QODD.
               DATA DIVISION.
               LINKAGE SECTION.
               01 QPT IS TYPEDEF USAGE PROGRAM-POINTER TO QODD.
               01 QP TYPE QPT.
               PROCEDURE DIVISION USING QP.
               END PROGRAM QEVEN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. QHOLD.
               PROCEDURE DIVISION.
                   GOBACK.
               END PROGRAM QHOLD.
               """;

    private static Dictionary<string, string> Identities(string source)
    {
        var comp = RepositoryCodecTests.Bind(source, 2023);
        return comp.Units.Where(u => u.IsPrototype).ToDictionary(u => u.Name, u => IdentityOf(comp, u, SignatureOf(u)));
    }

    /// <summary>Each property <c>Same</c> reads — a formal's mode, OPTIONAL, PICTURE, USAGE, SIGN, a returning item's
    /// JUSTIFIED and PICTURE, a group's elementary layout, the RETURNING item's presence, the formal count, and the prototype
    /// a restricted pointer formal's TO phrase names — changes the identity.</summary>
    [Theory]
    [InlineData("PMODE")]
    [InlineData("POPT")]
    [InlineData("PPIC")]
    [InlineData("PUSAGE")]
    [InlineData("PSIGN")]
    [InlineData("PJUST")]
    [InlineData("PLAYOUT")]
    [InlineData("PNORET")]
    [InlineData("PRETPIC")]
    [InlineData("PCOUNT")]
    public void EveryPropertyTheRelationReads_ChangesTheIdentity(string variant)
    {
        var ids = Identities(Perturbations);
        Assert.NotEqual(ids["PBASE"], ids[variant]);
    }

    /// <summary>⛔ A POINTER FORMAL IS DESCRIBED BY THE PROTOTYPE ITS TO PHRASE NAMES (kb/Work PB4491; docs/CONFORMANCE.md
    /// D-RP1): §2023:13.7.3 SR2 matches "the description of the formal parameters", and a restricted pointer's description is its
    /// USAGE clause, whose TO phrase §2023:14.8.2.3.2 and §2023:8.5.3.1 compare by name. So a pointer restricted to PBASE and one
    /// restricted to QBASE are two signatures although PBASE and QBASE are one, two prototypes that name each other (QODD,
    /// QEVEN) are two signatures, and neither is the self-referring PSELF. The type declaration a typed pointer formal
    /// references is in the signature too (§2023:8.13: "type declarations required for the description of parameters"), and
    /// §2023:8.5.3.1 makes two of them equivalent only under one type-name, so TO PBASE through PT and through QPT are two
    /// signatures (branch review R1-1 of kb/Work PB4491).</summary>
    [Fact]
    public void APointerFormal_IsIdentifiedByThePrototypeItsToPhraseNames()
    {
        var p = Identities(Perturbations);
        var q = Identities(Renamed);
        Assert.NotEqual(p["PPTRB"], p["PPTRM"]);   // TO PBASE and TO PMODE
        Assert.NotEqual(p["PPTRB"], p["PSELF"]);
        Assert.Equal(q["PBASE"], q["QBASE"]);       // one signature, two names ...
        Assert.NotEqual(q["QPTRB"], q["QPTRQ"]);   // ... so TO PBASE and TO QBASE are two descriptions
        Assert.NotEqual(p["PPTRB"], q["QPTRT"]);   // TO PBASE through type PT and through type QPT (§2023:8.13, §2023:8.5.3.1)
        Assert.NotEqual(q["QODD"], q["QEVEN"]);
        Assert.NotEqual(p["PSELF"], q["QODD"]);
        Assert.NotEqual(p["PSELF"], q["QEVEN"]);
    }

    /// <summary>Data-names, intermediate grouping, the prototype's OWN name and the group that compiled it change nothing:
    /// QBASE and the other group's PBASE are PBASE, and QPTRB, restricted to PBASE in another group, is PPTRB.</summary>
    [Fact]
    public void NamesGroupingAndTheCompilingGroup_DoNotChangeTheIdentity()
    {
        var p = Identities(Perturbations);
        var q = Identities(Renamed);
        Assert.Equal(p["PBASE"], q["QBASE"]);
        Assert.Equal(p["PBASE"], q["PBASE"]);
        Assert.Equal(p["PPTRB"], q["QPTRB"]);
    }
}
