## Entry 2693 — 2026-10-28 10:57 PDT — File the operator's leads of 2026-10-27: PB3994, PB3995 and a PB2913 recurrence

PB2913 gained a dated paragraph: on 2026-10-27 a steer could not make the campaign unit run PB3838 among the lanes
`-Cluster PB1957,PB3608,PB3838`, because `next_unit.py` picks the lead and a steer names only the unit.

PB3994 records that a `plan_wave.py --cluster` wave binds every group it plans to the campaign's OWNER REQUIREMENTS
(`group_requirements`' `always`), so wave 20261731 group B (PB2100, a PB1086 member) was told to meet campaign PB3838's
upstream-record bar.

PB3995 is the upstream record PB3989 owes: GnuCOBOL 3.2 keeps a DISPLAY receiver but overwrites a COMP-5 one under a
NOT-only SIZE ERROR statement. Reproduced in WSL on cobc 3.2.0: `X5=043`, `XD=99`. Nothing is posted upstream.

