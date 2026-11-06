// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO.Store;

/// <summary>⛔ WHAT A PAGED STORE IS, the meta page's organization byte (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4;
/// DESIGN-communication-facility.md §8): <c>R</c> and <c>I</c> for the RELATIVE and INDEXED files of §9.1.6's organizations,
/// whose <see cref="File"/> attributes the meta records, and <c>M</c> for the message control system's store, which is
/// no file, has no record frames or keys, and so carries no <see cref="FixedFileAttributes"/>. The engine is one for every
/// kind (pages, ledger, statement, adoption rule); only the meta page's layout of the kind differs.
/// <para>A message store is never a file of §9.1.6's organizations: <see cref="MetaPage.Describes"/> answers
/// <see cref="StoreFormat.Message"/> for it, so a COBOL OPEN of the path answers '39' (§14.9.27.4 GR10's conflict), and
/// <see cref="PagedFile.Open"/> refuses a store of the other family.</para></summary>
internal sealed record StoreKind
{
    private StoreKind(FixedFileAttributes? file) => File = file;

    /// <summary>The message store: no attributes, no keys, no alternate roots.</summary>
    public static StoreKind Message { get; } = new((FixedFileAttributes?)null);

    /// <summary>The RELATIVE or INDEXED file store recording <paramref name="attributes"/>.</summary>
    public static StoreKind Of(FixedFileAttributes attributes) => new(attributes);

    /// <summary>The §9.1.6 attributes a file store records; null for the message store.</summary>
    public FixedFileAttributes? File { get; }

    /// <summary>Whether this is the message store.</summary>
    public bool IsMessage => File is null;

    /// <summary>The attributes of a file store. A method, not a property: a record's synthesized ToString reads every public property, and the message store's must not throw.</summary>
    /// <exception cref="InvalidOperationException">The message store has none: a caller of a file asks only a file store.</exception>
    public FixedFileAttributes RequireAttributes() =>
        File ?? throw new InvalidOperationException("the message store records no fixed file attributes");

    /// <summary>The alternate keys the store has: every key after the prime key of an INDEXED file; none for a RELATIVE
    /// file (whose record-number tree is the prime root) and none for the message store.</summary>
    public int AlternateKeyCount => File is null ? 0 : AlternateKeysOf(File);

    /// <summary>The alternate keys of <paramref name="attributes"/>: the one rule behind <see cref="AlternateKeyCount"/>.</summary>
    public static int AlternateKeysOf(FixedFileAttributes attributes) => Math.Max(0, attributes.Keys.Count - 1);

    /// <summary>Whether the kind's meta fits its page: a file store's key descriptors must; the message store has none.</summary>
    public bool Fits => File is null || MetaPage.Fits(File);

    /// <summary>Whether <paramref name="other"/> cannot interpret this kind's pages: a different family, or file attributes
    /// that conflict (<see cref="FixedFileAttributes.Conflicts"/>).</summary>
    public bool Conflicts(StoreKind other) => (File, other.File) switch
    {
        (null, null) => false,
        ({ } mine, { } theirs) => mine.Conflicts(theirs),
        _ => true,
    };
}
