// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>⛔ THE THREE FILE ORGANIZATIONS, ONE ENUM (kb/Work PB771, PB3459, PB3471). §2023:9.1.6: <i>"There are three
/// organizations: sequential, relative, and indexed"</i>, and §2023:9.1.15 names none of them — <i>"The successful opening of
/// a file establishes a file lock for the applicable sharing rules"</i> is owed by every organization there is. RELATIVE
/// and INDEXED once owed it and paid nothing (they held no long-lived host handle between OPEN and CLOSE), which is why a
/// measurement of the file lock, of a sharing refusal or of a record lock is a theory over THIS enum. A test of the keyed
/// store alone iterates <see cref="FileHarness.KeyedOrgs"/>.</summary>
public enum FileOrg { Sequential, Relative, Indexed }
