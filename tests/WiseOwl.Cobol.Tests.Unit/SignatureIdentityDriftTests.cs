// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE CANONICAL SIGNATURE IDENTITY AGREES WITH THE ONE SAME-SIGNATURE RELATION (DESIGN-external-repository §6.4, §17.2;
/// kb/Work PB2103). A restricted program- or function-pointer's TYPE is its prototype's signature (ISO §13.18.60.4 GR25: "the
/// address of a program with the same signature as that identified by the specified program-prototype-name"; GR26 its
/// function twin), and the run-time relations compare the identity a description carries, so
/// <c>PrototypeSignatures.CanonicalIdentity</c> must name two prototypes alike exactly when
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
        comp.SignatureClasses.Identity(new RestrictedPrototype(u.IsFunction, u.ExternalizedName, signature));

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

    /// <summary>The same signatures as <see cref="Perturbations"/>' PBASE, PPTRB and PSELF, compiled in ANOTHER group under
    /// other names, other data-names and another intermediate grouping (§13.7.3 SR2 matches descriptions, not names; a
    /// group's description is its elementary items, §8.5.3.1), and with the recursion through a second prototype.</summary>
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
               PROGRAM-ID. QPTRB IS PROTOTYPE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM QBASE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 QPT IS TYPEDEF USAGE PROGRAM-POINTER TO QBASE.
               01 QP TYPE QPT.
               PROCEDURE DIVISION USING QP.
               END PROGRAM QPTRB.
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
    /// JUSTIFIED and PICTURE, a group's elementary layout, the RETURNING item's presence, the formal count, and the signature
    /// a restricted pointer formal is typed by — changes the identity.</summary>
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

    [Fact]
    public void APointerFormal_IsIdentifiedByTheSignatureItIsTypedBy_NeverByItsPrototypesName()
    {
        var ids = Identities(Perturbations);
        Assert.NotEqual(ids["PPTRB"], ids["PPTRM"]);   // TO PBASE and TO PMODE: two signatures
        Assert.NotEqual(ids["PPTRB"], ids["PSELF"]);
    }

    /// <summary>Data-names, intermediate grouping, prototype names and the group that compiled them change nothing: QBASE is
    /// PBASE, QPTRB (restricted to QBASE) is PPTRB (restricted to PBASE), and a pointer recursion through two prototypes
    /// that the coarsest stable partition makes one class is the self-recursive PSELF.</summary>
    [Fact]
    public void NamesGroupingAndTheCompilingGroup_DoNotChangeTheIdentity()
    {
        var p = Identities(Perturbations);
        var q = Identities(Renamed);
        Assert.Equal(p["PBASE"], q["QBASE"]);
        Assert.Equal(p["PPTRB"], q["QPTRB"]);
        Assert.Equal(p["PSELF"], q["QODD"]);
        Assert.Equal(p["PSELF"], q["QEVEN"]);
    }
}
