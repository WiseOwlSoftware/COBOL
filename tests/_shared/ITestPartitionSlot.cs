// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>One partition slot: a compile-time integer, carried as a type argument so a partition class can be a
/// single line. Adding a slot is one line here; nothing else in the mechanism has a fixed upper bound.</summary>
public interface ITestPartitionSlot
{
    /// <summary>This slot's zero-based index. Must equal the numeric suffix of the implementing type's name —
    /// <see cref="TestPartitionAudit"/> enforces it, because a slot whose name and index disagree would make every
    /// partition class read as the wrong partition.</summary>
    static abstract int Index { get; }
}

// The ladder: 32 identical one-line declarations, kept honest by TestPartitionAudit.AuditSlotLadder. Its top is set
// by the widest family, ConformanceSuiteTests (32 partitions, kb/Work PB2527, PB3177).
public readonly struct Slot0 : ITestPartitionSlot { public static int Index => 0; }
public readonly struct Slot1 : ITestPartitionSlot { public static int Index => 1; }
public readonly struct Slot2 : ITestPartitionSlot { public static int Index => 2; }
public readonly struct Slot3 : ITestPartitionSlot { public static int Index => 3; }
public readonly struct Slot4 : ITestPartitionSlot { public static int Index => 4; }
public readonly struct Slot5 : ITestPartitionSlot { public static int Index => 5; }
public readonly struct Slot6 : ITestPartitionSlot { public static int Index => 6; }
public readonly struct Slot7 : ITestPartitionSlot { public static int Index => 7; }
public readonly struct Slot8 : ITestPartitionSlot { public static int Index => 8; }
public readonly struct Slot9 : ITestPartitionSlot { public static int Index => 9; }
public readonly struct Slot10 : ITestPartitionSlot { public static int Index => 10; }
public readonly struct Slot11 : ITestPartitionSlot { public static int Index => 11; }
public readonly struct Slot12 : ITestPartitionSlot { public static int Index => 12; }
public readonly struct Slot13 : ITestPartitionSlot { public static int Index => 13; }
public readonly struct Slot14 : ITestPartitionSlot { public static int Index => 14; }
public readonly struct Slot15 : ITestPartitionSlot { public static int Index => 15; }
public readonly struct Slot16 : ITestPartitionSlot { public static int Index => 16; }
public readonly struct Slot17 : ITestPartitionSlot { public static int Index => 17; }
public readonly struct Slot18 : ITestPartitionSlot { public static int Index => 18; }
public readonly struct Slot19 : ITestPartitionSlot { public static int Index => 19; }
public readonly struct Slot20 : ITestPartitionSlot { public static int Index => 20; }
public readonly struct Slot21 : ITestPartitionSlot { public static int Index => 21; }
public readonly struct Slot22 : ITestPartitionSlot { public static int Index => 22; }
public readonly struct Slot23 : ITestPartitionSlot { public static int Index => 23; }
public readonly struct Slot24 : ITestPartitionSlot { public static int Index => 24; }
public readonly struct Slot25 : ITestPartitionSlot { public static int Index => 25; }
public readonly struct Slot26 : ITestPartitionSlot { public static int Index => 26; }
public readonly struct Slot27 : ITestPartitionSlot { public static int Index => 27; }
public readonly struct Slot28 : ITestPartitionSlot { public static int Index => 28; }
public readonly struct Slot29 : ITestPartitionSlot { public static int Index => 29; }
public readonly struct Slot30 : ITestPartitionSlot { public static int Index => 30; }
public readonly struct Slot31 : ITestPartitionSlot { public static int Index => 31; }
