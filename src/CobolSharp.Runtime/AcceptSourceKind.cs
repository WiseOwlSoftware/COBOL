// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// Source kind for ACCEPT FROM statement (DATE, TIME, DAY, DAY-OF-WEEK).
/// Shared between compiler and runtime.
/// </summary>
public enum AcceptSourceKind
{
    /// <summary>Not a system source — ACCEPT FROM identifier.</summary>
    None,
    /// <summary>ACCEPT FROM DATE — six-digit YYMMDD or eight-digit YYYYMMDD.</summary>
    Date,
    /// <summary>ACCEPT FROM TIME — eight-digit HHMMSScc (hours, minutes, seconds, centiseconds).</summary>
    Time,
    /// <summary>ACCEPT FROM DAY — five-digit YYDDD or seven-digit YYYYDDD (Julian day).</summary>
    Day,
    /// <summary>ACCEPT FROM DAY-OF-WEEK — single digit 1 (Monday) through 7 (Sunday).</summary>
    DayOfWeek
}
