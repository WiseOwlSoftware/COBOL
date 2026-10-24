// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Collation.Locale;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using Xunit;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The run unit's ONE locale state (Runtime/Control/LocaleState.cs; DESIGN-locale-facility §4.3; kb/Work PB64 T1):
/// the §14.6.6 state model (r1 a copy at activation, r3 category independence, §14.9.39.4 GR25 persistence), SET
/// format 11's four sources (GR22/GR23), format 12's saved-locale HANDLE (GR26/GR27; DETERMINATION L4) and its
/// per-category snapshot, the two runtime conditions the statements own — EC-LOCALE-MISSING (GR24) and
/// EC-LOCALE-INVALID-PTR (GR21) — through their checking flags (a raise only while checking is on; the state is
/// unchanged either way), DETERMINATION L1's normalization of external identifications, and the SORT snapshot
/// (§14.6.6 r5 — <c>CobolCollation.Snapshot</c>). The suite tests pb64t1_* (conformance-suite/cases/) prove the same
/// through compiled COBOL; this pins the runtime's contract directly.
/// </summary>
[Collection("process-globals")]   // reads the process-global locale configuration (the site tailoring directory variable a
                                  // CobolLocaleTests / LocaleManagerTests case points at a temp directory it then deletes; kb/Work PB126)
public sealed class LocaleStateTests
{
    [Fact]
    public void Activation_CopiesTheUserDefault_IntoEveryCategory_AndALaterUserDefaultChangeDoesNotMoveThem()
    {
        var st = new LocaleState();
        foreach (LocaleCategory c in Enum.GetValues<LocaleCategory>())
            Assert.Equal(st.UserDefault[c], st.Current(c));                              // §14.6.6 r1
        st.SetUserDefaultFromLocale("es-ES");                                               // GR22
        Assert.Equal("es-ES", st.UserDefault.Collate);
        Assert.Equal(LocaleValue.Of(st.UserDefault.Collate == "es-ES" ? st.CurrentLocale.Collate : "").Collate, st.Current(LocaleCategory.Collate));   // unchanged (a copy, not a reference)
        st.SetFromUserDefault(LocaleCategorySet.Collate);                                   // GR23b
        Assert.Equal("es-ES", st.Current(LocaleCategory.Collate));
        Assert.NotEqual("es-ES", st.Current(LocaleCategory.Time));                          // only the named category
    }

    [Fact]
    public void Set_SwitchesOnlyTheNamedCategories_AndTheyPersist()
    {
        var st = new LocaleState();
        string before = st.Current(LocaleCategory.Collate);
        st.SetFromLocale(LocaleCategorySet.Numeric | LocaleCategorySet.Time, "sv-SE");
        Assert.Equal("sv-SE", st.Current(LocaleCategory.Numeric));
        Assert.Equal("sv-SE", st.Current(LocaleCategory.Time));
        Assert.Equal(before, st.Current(LocaleCategory.Collate));                           // §14.6.6 r3
        Assert.Equal(before, st.Current(LocaleCategory.Monetary));
        st.SetFromLocale(LocaleCategorySet.Collate, "es-ES");
        Assert.Equal("sv-SE", st.Current(LocaleCategory.Time));                             // GR25 — still in effect
        st.SetFromSystemDefault(LocaleCategorySet.All);
        Assert.Equal(st.SystemDefault, st.CurrentLocale);
        Assert.False(st.CurrentLocale.IsUniform && st.CurrentLocale.Collate == "es-ES");
    }

    [Fact]
    public void SaveAndRestore_IsAPerCategorySnapshot_ThroughAManagedHandle()
    {
        var st = new LocaleState();
        st.SetFromLocale(LocaleCategorySet.Collate, "es-ES");
        var p = st.Save(userDefault: false);                                                 // GR26
        Assert.False(p.IsNull);
        Assert.Same(st, p.Owner);
        Assert.Equal("es-ES", p.Value.Collate);
        Assert.Equal(st.UserDefault.Time, p.Value.Time);
        st.SetFromSystemDefault(LocaleCategorySet.All);
        st.SetFromSaved(LocaleCategorySet.Collate, p);                                       // GR23a via identifier-10
        Assert.Equal("es-ES", st.Current(LocaleCategory.Collate));
        st.SetFromLocale(LocaleCategorySet.Time, "sv-SE");
        st.SetFromSaved(LocaleCategorySet.Time, p);                                          // the snapshot's LC_TIME (root), not sv
        Assert.Equal(p.Value.Time, st.Current(LocaleCategory.Time));
        var u = st.Save(userDefault: true);                                                 // GR27
        Assert.Equal(st.UserDefault, u.Value);
        Assert.True(u.Handle > p.Handle);                                                   // monotonic, never reused
        st.SetUserDefaultFromSaved(p);                                                       // GR22 with identifier-10
        Assert.Equal(p.Value, st.UserDefault);
        Assert.True(st.IsSavedLocale(p));
        Assert.False(st.IsSavedLocale(ManagedPointer.Null));
        Assert.False(new LocaleState().IsSavedLocale(p));                                   // another run unit's handle
    }

    [Fact]
    public void InvalidPointer_RaisesOnlyWithCheckingOn_AndLeavesTheStateUnchanged()
    {
        RunUnit.Run(ru =>
        {
            var st = ru.Locale;
            st.SetFromLocale(LocaleCategorySet.Collate, "es-ES");
            var foreign = new LocaleState().Save(false);
            var cell = ManagedPointer.At(new StorageCell { Ref = "abcd" }, 0);
            // checking OFF: unsuccessful, silent, unchanged (§14.6.13.1.1)
            st.SetFromSaved(LocaleCategorySet.Collate, ManagedPointer.Null);
            st.SetFromSaved(LocaleCategorySet.Collate, cell);
            st.SetFromSaved(LocaleCategorySet.Collate, foreign);
            st.SetUserDefaultFromSaved(null);
            Assert.Equal("es-ES", st.Current(LocaleCategory.Collate));
            Assert.Null(ExceptionState.LastName);
            ExceptionState.LocaleInvalidPtrChecking = true;
            try
            {
                var ex = Assert.Throws<CobolFatalException>(() => st.SetFromSaved(LocaleCategorySet.All, cell));
                Assert.Equal("EC-LOCALE-INVALID-PTR", ex.EcName);
                Assert.Equal("EC-LOCALE-INVALID-PTR", ExceptionState.LastName);
                Assert.Throws<CobolFatalException>(() => st.SetFromSaved(LocaleCategorySet.All, foreign));
                Assert.Throws<CobolFatalException>(() => st.SetUserDefaultFromSaved(ManagedPointer.Null));
                Assert.Equal("es-ES", st.Current(LocaleCategory.Collate));                  // GR21 — unsuccessful
                st.SetFromSaved(LocaleCategorySet.All, st.Save(false));                      // a live handle: fine
            }
            finally { ExceptionState.LocaleInvalidPtrChecking = false; }
        });
    }

    [Fact]
    public void UnavailableLocale_RaisesMissingOnlyWithCheckingOn_AndLeavesTheStateUnchanged()
    {
        RunUnit.Run(ru =>
        {
            var st = ru.Locale;
            string before = st.Current(LocaleCategory.Collate);
            st.SetFromLocale(LocaleCategorySet.All, "xx-NOWHERE");                           // checking off: silent
            Assert.Equal(before, st.Current(LocaleCategory.Collate));
            st.SetUserDefaultFromLocale("xx-NOWHERE");
            Assert.Equal(LocaleValue.Of(before).Collate, st.UserDefault.Collate);
            ExceptionState.LocaleMissingChecking = true;
            try
            {
                var ex = Assert.Throws<CobolFatalException>(() => st.SetFromLocale(LocaleCategorySet.Collate, "xx-NOWHERE"));
                Assert.Equal("EC-LOCALE-MISSING", ex.EcName);
                Assert.Throws<CobolFatalException>(() => st.SetUserDefaultFromLocale("xx-NOWHERE"));
                Assert.Equal(before, st.Current(LocaleCategory.Collate));
                // A NAMED IS LOCALE sequence raises at USE (L1 item 4) — and the root-bound one ("") never does.
                Assert.Throws<CobolFatalException>(() => new LocaleCollation("xx-NOWHERE").Compare("a", "b"));
                Assert.True(new LocaleCollation("").Compare("a", "b") < 0);
                Assert.True(new LocaleCollation("es_ES.UTF-8").Compare("ñu", "nz") > 0);   // L1-normalized at use
            }
            finally { ExceptionState.LocaleMissingChecking = false; }
        });
    }

    [Fact]
    public void IncompatibleOperand_RaisesOnlyWithCheckingOn_AndStillOrders()
    {
        RunUnit.Run(_ =>
        {
            var seq = new LocaleCollation("es-ES");
            string lone = "a\uD800";                                                         // an unpaired surrogate
            int off = seq.Compare(lone, "a");                                                // checking off: an order, no raise
            Assert.NotEqual(0, off);
            ExceptionState.LocaleIncompatibleChecking = true;
            try
            {
                var ex = Assert.Throws<CobolFatalException>(() => seq.Compare(lone, "a"));
                Assert.Equal("EC-LOCALE-INCOMPATIBLE", ex.EcName);
                Assert.Throws<CobolFatalException>(() => seq.KeyOf(lone));
                Assert.True(seq.Compare("a", "b") < 0);                                      // well-formed: no raise
            }
            finally { ExceptionState.LocaleIncompatibleChecking = false; }
        });
    }

    [Theory]
    [InlineData("fr_FR", "fr-FR")]
    [InlineData("fr_FR.UTF-8", "fr-FR")]
    [InlineData("fr-FR", "fr-FR")]
    [InlineData("  sr_Latn_RS.utf8 ", "sr-Latn-RS")]
    [InlineData("de_DE@phonebook", "de-DE-u-co-phonebook")]
    [InlineData("zh@pinyin", "zh-u-co-pinyin")]
    [InlineData("de-u-kf-upper@phonebk", "de-u-kf-upper-co-phonebook")]
    [InlineData("INVARIANT", "")]
    [InlineData("root", "")]
    [InlineData("C", "")]
    [InlineData("POSIX", "")]
    [InlineData("", "")]
    public void ExternalIdentification_IsNormalized_PerDeterminationL1(string external, string tag)
    {
        Assert.Equal(tag, LocaleIdentification.Normalize(external));
    }

    [Fact]
    public void Availability_IsTheOneKnownLocaleRule()
    {
        Assert.True(LocaleIdentification.IsAvailable(""));
        Assert.True(LocaleIdentification.IsAvailable("es_ES.UTF-8"));
        Assert.True(LocaleIdentification.IsAvailable("de-DE@phonebook"));
        Assert.False(LocaleIdentification.IsAvailable("xx-NOWHERE"));
        Assert.False(LocaleIdentification.IsAvailable("no-Such-TABLE"));                     // the parent chain never makes a tag known
        Assert.False(LocaleIdentification.IsAvailable("de@nonsense"));                       // an unknown modifier → unavailable
    }

    [Fact]
    public void Snapshot_FreezesTheCurrentLocaleForm_AndLeavesBoundFormsAlone()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.SetFromLocale(LocaleCategorySet.Collate, "es-ES");
            var snap = LocaleCollation.Current.Snapshot();                                   // §14.6.6 r5
            var bound = new LocaleCollation("sv");
            var pinned = bound.Snapshot();                                                     // a NEW sequence: it pins
            Assert.NotSame(bound, pinned);
            Assert.Equal("sv", Assert.IsType<LocaleCollation>(pinned).LocaleTag);              // the same locale
            Assert.Same(pinned, pinned.Snapshot());                                            // a snapshot is its own
            ru.Locale.SetFromSystemDefault(LocaleCategorySet.All);                            // a switch DURING the statement
            Assert.True(snap.Compare("ñu", "nz") > 0);                                       // still Spanish
            Assert.True(LocaleCollation.Current.Compare("ñu", "nz") < 0);                    // the live form followed
            // And CobolSort takes the snapshot at Init — a switch between Init and Sort has no effect.
            ru.Locale.SetFromSystemDefault(LocaleCategorySet.All);
            CobolSort.Init("SNAP-TEST", [new CobolSort.Key(0, 2, false, CobolSort.KeyClass.Alphanumeric, default)],
                LocaleCollation.Current, national: null);
            CobolSort.Release("SNAP-TEST", "nz", 0, 0, int.MaxValue);
            ru.Locale.SetFromLocale(LocaleCategorySet.Collate, "es-ES");
            CobolSort.Release("SNAP-TEST", "ñu", 0, 0, int.MaxValue);
            CobolSort.Sort("SNAP-TEST", false);
            Assert.True(CobolSort.Return("SNAP-TEST", out string? first));
            Assert.Equal("ñu", first);                                                        // root order: ñu before nz
        });
    }

    /// <summary>kb/Work PB3331 (review w1123b R1-1): a snapshot (§14.6.6 r5; §12.4.5.7.4 GR1 for an open indexed file)
    /// resolves ONE collator, at its first use, and keeps it. The engine's caches can be dropped between two uses
    /// (<c>LocaleManager.ClearCache</c>), which makes the next resolution a NEW collator, and
    /// <see cref="CobolNet.Runtime.Collation.CollationKey.CompareTo"/> refuses keys of two collators: the live form shows
    /// the hazard (its keys from before and after a clear do not compare), the snapshot's keys always do, in Danish
    /// order (aa is å, after z).</summary>
    [Fact]
    public void Snapshot_PinsOneCollator_SoKeysBuiltAcrossACacheClearCompare()
    {
        var live = new LocaleCollation("da");
        var snap = live.Snapshot();
        var liveBefore = live.KeyOf("aa");
        var snapBefore = snap.KeyOf("aa")!;
        var collatorBefore = live.Resolve();
        try
        {
            LocaleManager.ClearCache();
            Assert.NotSame(collatorBefore, live.Resolve());                                   // the live form resolves anew
            Assert.Throws<ArgumentException>(() => liveBefore.CompareTo(live.KeyOf("zz")));   // the hazard the pin removes
            Assert.True(snapBefore.CompareTo(snap.KeyOf("zz")) > 0);                           // da: aa (å) after zz
            Assert.Same(Assert.IsType<LocaleCollation>(snap).Resolve(), snapBefore.Collator);   // one collator throughout
        }
        finally { LocaleManager.ClearCache(); }
    }

    /// <summary>kb/Work PB3331 (review w1123b R1-1), the SORT arm of the pin: a sort larger than its memory limit merges
    /// its runs while RETURN reads them, and each run head's key is built when the head advances
    /// (<c>SortKeyColumns.Set</c>). A cache clear between two RETURNs gave the heads keys of two collators, and the merge's
    /// comparison threw; through the statement's snapshot every key is one collator's, so the RETURNs complete in the
    /// key's LOCALE order (§14.9.40.4 GR8, §8.8.4.2.11), stable on equal keys (GR3 b).</summary>
    [Fact]
    public void ASortMergingRuns_KeepsOneCollator_AcrossACacheClearBetweenReturns()
    {
        const string sd = "PB3331-PIN";
        string dir = ScratchDirectory.Create("pb3331-pin-");
        var rng = new Random(7);
        var records = new List<string>(6000);
        string[] syllables = ["aa", "ab", "zz", "za", "æa", "øa"];
        for (int i = 0; i < 6000; i++)
            records.Add($"{syllables[rng.Next(syllables.Length)]}{syllables[rng.Next(syllables.Length)]}{i:D7}{new string('x', 24)}");
        var da = new LocaleCollation("da");
        var expected = records.OrderBy(r => r[..4], Comparer<string>.Create((x, y) => da.Compare(x, y))).ToList();
        try
        {
            RunUnit.Run(unit =>
            {
                unit.SortFiles.Memory = new SortMemory(SortMemory.MinimumLimitBytes, dir);
                CobolSort.Init(sd, [new CobolSort.Key(0, 4, false, CobolSort.KeyClass.Alphanumeric, default)], da);
                CobolSort.EnterProcedure(sd, output: false);
                foreach (var r in records) CobolSort.ReleaseStatement(sd, r, 0, r.Length);
                CobolSort.Sort(sd, duplicatesInOrder: true);
                Assert.True(unit.SortFiles.Get(sd).Work.RunsWritten > 2, "the sort did not merge runs");
                CobolSort.EnterProcedure(sd, output: true);
                var got = new List<string>();
                while (CobolSort.Return(sd, out var image))
                {
                    got.Add(image);
                    if (got.Count % 1000 == 1) LocaleManager.ClearCache();                        // between two RETURNs
                }
                Assert.Equal(expected, got);
                CobolSort.Close(sd);
            });
        }
        finally
        {
            LocaleManager.ClearCache();
            ScratchDirectory.Remove(dir);
        }
    }
}
