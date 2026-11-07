// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// A data-pointer VALUE decoded from eight positions this run unit never imaged (kb/Work PB4838): the token a file
/// carried in from another run unit, or any positions that are not an address this run unit handed out. The owner decided that
/// such a token keeps its image and never reads as NULL, and that a reference through it raises: it addresses no storage, so
/// <see cref="CobolPtr.Deref"/> answers EC-BOUND-PTR (§2023:13.18.5.4 GR4 — the address is not a valid address of data storage),
/// the outcome of every pointer that is not a window over a live cell. It is equal only to a foreign address of the same image.
/// </summary>
internal sealed class ForeignAddress(string image) : ManagedPointer
{
    /// <summary>The eight positions the pointer was decoded from, and images back as.</summary>
    internal string Image { get; } = image;
}
