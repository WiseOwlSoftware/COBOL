// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// Which of a unit's range-collation carriers a THROUGH range's <c>IN alphabet-name-1</c> phrase named (ISO §2023:14.7.8 rule 2):
/// its position in <see cref="DataBindResult.RangeCollations"/>, the carriers in the order first named. The range's node carries
/// it (<see cref="Bound.BoundRangeMembership.Collation"/>, <see cref="Condition88.Collation"/>), so the backend reads the carrier by
/// position and looks no alphabet-name up (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 4": a name table is
/// the symbol table's, and the backend cannot read the binder's).
/// </summary>
/// <param name="Index">The carrier's position in <see cref="DataBindResult.RangeCollations"/>.</param>
public readonly record struct RangeCollationId(int Index);
