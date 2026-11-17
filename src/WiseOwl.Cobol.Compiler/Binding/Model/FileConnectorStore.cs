// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>A data item an input-output statement stores into through its file connector without naming it (kb/Work PB5885):
/// the statement names the file, and the rule of the verb or of the file's clause makes the store. Placed by
/// <c>PlaceBuilder.ConnectorStores</c>; which verb stores which is <c>BoundStores</c>'s.</summary>
public enum FileConnectorStore
{
    /// <summary>The record area: every record description of the file and of a record-area SAME clause's files
    /// (§2023:14.9.30.4 GR13 for READ, §2023:14.9.34.4 GR3 for RETURN).</summary>
    RecordArea,
    /// <summary>The FILE STATUS item (§2023:12.4.5.8.4 GR1), set by CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK and
    /// WRITE (§2023:9.1.13.1).</summary>
    Status,
    /// <summary>The RELATIVE KEY item of a relative file (READ: §2023:14.9.30.4 GR25; sequential WRITE: §2023:14.9.51.4 GR29 a)).</summary>
    RelativeKey,
    /// <summary>The RECORD VARYING DEPENDING ON item (§2023:13.18.43.4 GR15: READ and RETURN).</summary>
    Depending,
}
