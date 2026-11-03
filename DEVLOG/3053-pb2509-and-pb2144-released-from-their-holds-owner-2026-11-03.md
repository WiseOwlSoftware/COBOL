## Entry 3053 — 2026-11-03 14:52 PST — PB2509 and PB2144 released from their holds (owner, 2026-11-03)

The owner listed the ten open compiler defects in the attended operator session and approved all four recommended
steps ("yes, do all four steps"). Two of the ten waited on holds that could not clear on their own:

- **PB2509** (the record sequential print stream writes each record less its trailing spaces: a wrong answer) waited
  on PB3420, the SORT and file I/O completion plan, whose tier 3 the owner had already released on 2026-10-23. It is
  released the way PB2556 was on 2026-10-29: PB3420 leaves its `blocked_by`. PB3420 stays `status: owner` because, as
  its note explains, as `open` a campaign wave would plan the plan itself.
- **PB2144** (ADD, SUBTRACT and DIVIDE report the Format-2 shape error as a raw COBOL0001 parse error where MULTIPLY
  names it COBOL1689) waited on PB2350, the token-stream and parse-tree differential. PB2350 belongs to the PB2119
  Delete program, which PB2911 holds until zero GAP and zero known defects, so the wait was circular. The owner exempts
  PB2144 from the differential; its oracle is the whole gate plus the four verbs' error-form goldens.
  DESIGN-architecture-review §8.4 records the exemption.

Both notes now appear in `work.py next`. PB3600 and PB3957 were already actionable; the loop is steered to plan all
four in its next wave.

