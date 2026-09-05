// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The four LINAGE clause operand VALUES as the runtime element executing a statement sees them (ISO §13.18.34
/// GR6): page size (GR2), footing start (GR3 — 0 = the FOOTING phrase is absent, GR1), top margin (GR4) and
/// bottom margin (GR5).
/// <para>⛔ THIS TRAVELS WITH THE STATEMENT AND IS NEVER STORED ON THE CONNECTOR. GR6 b) fixes the times the
/// operand values are read — <i>"the value is the content of the data item referenced by the associated
/// data-name at the following times when the indicated statement references the associated file: 1. At the
/// completion of an OPEN statement with the OUTPUT phrase. 2. During the execution of a WRITE statement that is
/// specified with the ADVANCING PAGE phrase. 3. During the execution of a WRITE statement that causes a page
/// overflow condition."</i> — and every one of those times is a STATEMENT, executed by one runtime element in
/// one activation. A file connector is not: an EXTERNAL one is a single object shared by every describing
/// runtime element in the run unit (§13.18.22.4 GR4 a), and a RECURSIVE unit's internal one is unit-scoped
/// across activations (§8.6.4 / §14.6.2.3.3). A connector-held evaluator closure therefore answered with
/// whichever element/activation installed it LAST rather than the one executing the statement (kb/Work PB673;
/// the shape it replaced is kb/Work PB168's unguarded install).</para>
/// <para>What the connector DOES keep is the page model most recently established — GR6's <i>"When a value is
/// determined for the page size, top margin, footing start, and bottom margin, the value applies to the next
/// logical page"</i> — see <see cref="SequentialConnector"/>'s <c>_pageBody</c> / <c>_footing</c> /
/// <c>_top</c> / <c>_bottom</c>. That is a page property, not an operand source.</para>
/// </summary>
/// <param name="Body">Page size — the number of lines that may be written or spaced on the logical page
/// (§13.18.34 GR2, integer-1 / data-name-1).</param>
/// <param name="Footing">The line number within the page body at which the footing area begins (GR3,
/// integer-2 / data-name-2); 0 when the WITH FOOTING phrase is absent (GR1 — no end-of-page condition
/// independent of page overflow).</param>
/// <param name="Top">The top margin (GR4, integer-3 / data-name-3); 0 when the phrase is absent (GR1).</param>
/// <param name="Bottom">The bottom margin (GR5, integer-4 / data-name-4); 0 when absent (GR1).</param>
public readonly record struct LinagePage(int Body, int Footing, int Top, int Bottom);
