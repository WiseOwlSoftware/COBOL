// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Identifies which DATA DIVISION storage area a data item resides in.
/// Determines lifetime, initialization, and accessibility of the item.
/// </summary>
public enum StorageAreaKind
{
    /// <summary>WORKING-STORAGE SECTION: persistent data allocated once per program load.</summary>
    WorkingStorage,
    /// <summary>FILE SECTION: record buffers associated with FD file descriptors.</summary>
    FileSection,
}
