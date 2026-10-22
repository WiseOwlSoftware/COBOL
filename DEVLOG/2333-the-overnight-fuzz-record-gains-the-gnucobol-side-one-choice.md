## Entry 2333 — 2026-10-21 14:29 PDT — The overnight fuzz record gains the GnuCOBOL side: one choice, three bugs, and a fifth hole in the net

The owner asked whether the four most common "WiseOwl correct" output rows of the first overnight differential fuzz run
(`docs/rearchitecture/evidence/fleet-optimization/2026-10-21-differential-fuzz-overnight.md`) are GnuCOBOL bugs or GnuCOBOL
choices. Read in GnuCOBOL's source at its 3.2 release tag and checked with one probe under the WSL 3.2: `0 ** 0` → 1 is a bug
(compile-time constant folding bypasses a rule the runtime implements, comment `/* 0 ^ 0 */` and all); `2 ** 3 ** 4` grouped
from the right is an undocumented grammar choice; `DATE-TO-YYYYMMDD(0)` accepted is a bug (`year < 0` where the standard says
positive, under a comment claiming the standard); `EXCEPTION-STATUS` set inside `ON SIZE ERROR` is a bug the source itself
marks `FIXME`. The probe also found a fifth hole: with variable operands GnuCOBOL takes the SIZE ERROR branch AND stores 1
into the receiving item.

The record gains that section and the probe (`gnucobol-side/`), so the next triage recognizes these rows instead of re-deriving
them. Nothing is filed: GnuCOBOL's defects are not this project's (the differential is a net, never authority). The record's
triage duration is corrected from "about three hours" to about 45 minutes (10:16 to 10:59 by the scratchpad and commit stamps).

