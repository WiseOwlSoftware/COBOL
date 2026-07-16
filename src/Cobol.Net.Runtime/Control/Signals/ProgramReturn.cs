// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The called-program return signal (ISO §14.9.18 GR2 / §14.9.14 GR3): GOBACK (or EXIT PROGRAM in a called
/// program) raises it; the raising program's activation entry catches it and returns control to the activator.
/// In a MAIN program the activation entry is the run-unit wrapper, so a main-program GOBACK terminates the run
/// unit (§14.9.18 GR3 — GOBACK in a main program acts as a STOP statement). Distinct from <see cref="StopRun"/>,
/// which unwinds the WHOLE run unit from anywhere (§14.9.43).
/// </summary>
public sealed class ProgramReturn : Exception;
