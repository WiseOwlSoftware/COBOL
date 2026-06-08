// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Control-flow signal for <c>STOP RUN</c> (and a main program's <c>GOBACK</c>): it terminates the run unit,
/// unwinding out of however many nested paragraph/PERFORM method calls are active (ISO §14.9.43). A program's
/// entry point catches it; reaching the end of the procedure division falls through normally without throwing.
/// </summary>
public sealed class StopRun : Exception;
