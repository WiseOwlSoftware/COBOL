## PB1652: a golden pins that a SUPPRESSed or all-absent GENERATE still consumes GROUP INDICATE

PB1244 made GROUP INDICATE a per-detail-group condition consumed by every GENERATE of the group. No golden
exercised a GENERATE that prints nothing, so a regression that consumed the condition only on a printed line
would have passed every test. Wave 20261791 group D closed the gap.

**Re-probe.** The sweep's probe file was gone, so the program was rewritten and run on a fresh Debug build. D1 (a
detail with `GROUP INDICATE`) has its first GENERATE inhibited by a USE BEFORE REPORTING declarative executing
SUPPRESS PRINTING; D2's only LINE carries `PRESENT WHEN WS-SHOW = 1` and is absent on its first GENERATE. The
report read back prints `[ 2]` `[ 3]` `[ 5]` `[ 6]`. Each is the line §13.18.28.4 GR1 derives (the first GENERATE
after INITIATE is the "first occasion", whether or not it printed, so the next finds the condition false and
the indicated item absent, and the relative item after it lands in column 2 by §13.18.14.4 GR7). The compiler
was already right (`ReportWriter.GenerateCore` clears `GroupIndicatePending` after `PresentBody`); no source
changed.

**Goldens.** `conformance-suite/cases/13.18.28-GROUP-INDICATE/pb1652_group_indicate_suppressed_generate` (accept
from 2002, read back as pb1244's golden does) and the 85 refusal `pb1652-group-indicate-below-2002` (COBOL0900,
the version-matrix introduction gate). Every citation was run through `cite.py --check`.

**Register.** PB1652 flips to landed with `closes_rows: []` (it held no inventory rows).
