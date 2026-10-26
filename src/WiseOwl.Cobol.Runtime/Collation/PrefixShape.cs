// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Collation;

/// <summary>The PREFIX SHAPE of one text under one <see cref="Collator"/> (<see cref="Collator.PrefixShapeOf"/>): for
/// every CUT p of the text (a position no mapping straddles), the walk of its first p characters on their own yields at
/// least p − <see cref="Deficit"/> level-1 weights, and they are the first level-1 weights of the whole text; and a cut
/// lies within <see cref="Overhang"/> + 1 characters at or before every position. Together: the first
/// k − Deficit − Overhang level-1 weights of the text are level-1 weights of its first k characters walked on their own,
/// for every k up to its length — the bound the keyed store's partial-key START takes under a locale-based sequence
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4).</summary>
/// <param name="Deficit">The largest (cut − level-1 weights before it); 0 for text whose characters each weigh one
/// primary, more for ignorable characters, less (never below 0) for expansions.</param>
/// <param name="Overhang">The longest run of characters between two consecutive cuts, less one: 0 when no mapping spans
/// two characters.</param>
internal readonly record struct PrefixShape(int Deficit, int Overhang);
