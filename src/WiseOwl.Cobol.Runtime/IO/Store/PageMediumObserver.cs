// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO.Store;

/// <summary>⛔ THE ONE FAULT SEAM OF THE PAGED STORE ENGINE'S MEDIUM (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.12;
/// DESIGN-communication-facility.md §8): a test's wrapper of the medium every client of the engine opens its store through, per
/// async flow (the <see cref="FileRegistry.PauseObserver"/> precedent), so a case can refuse, tear or kill at any page write
/// through a real client — a keyed store behind a file connector (<c>KeyedStore</c>) or the message control system's store
/// (<c>McsStore</c>, DESIGN-communication-facility.md §9 item 3, not yet built) — with one fake medium (<c>DyingPageMedium</c>, <c>RefusingPageMedium</c>) and no second seam.</summary>
internal static class PageMediumObserver
{
    private static readonly AsyncLocal<Func<IPageMedium, IPageMedium>?> Wrapper = new();

    /// <summary>The wrapper installed in the calling flow, or null; a test sets it before opening a store and clears it
    /// in a <c>finally</c>.</summary>
    public static Func<IPageMedium, IPageMedium>? Value
    {
        get => Wrapper.Value;
        set => Wrapper.Value = value;
    }

    /// <summary>The medium a client of the engine hands its <see cref="PagedFile"/>: <paramref name="medium"/> itself, or the
    /// calling flow's wrapper of it. The ONE place a client reads the seam.</summary>
    public static IPageMedium Observe(IPageMedium medium) => Wrapper.Value?.Invoke(medium) ?? medium;
}
