## Plan §0 says who runs what after the fuzz night of 2026-10-29

The operator stopped the loop at 22:30 PDT on 2026-10-29 for the owner's differential fuzz run and restarted it at
09:17 on 2026-10-30, after the run ended and the Mythos lane had returned its checkout to main. §0 now has one bullet
for the split of work between the two lanes, in place of a stale one. The operator's loop runs `-Cluster
PB1957,PB3838` beside the fix lane's waves. The Mythos lane runs the GAP campaign, each fuzz night's triage, PB4075
(MEAN rounding twice) and PB4092/PB4093 (EC-SIZE). The `blocked_by` holds on PB4047, PB4012 and PB2103 stay until the
A.1 chain lands.
