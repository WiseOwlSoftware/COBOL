## Entry 3002 — 2026-11-02 09:59 PST — kb/Work PB4452: the external-repository slice reservation holds restructuring waves on whole files

Filed PB4452. `inflight.slice_reservation` reserves DESIGN-external-repository §12's file set whole. As a result
`plan_wave.py --cluster PB2293` held all 22 open backend-seam steps behind two frontend branches that carry PB2103's
lead and change none of those files' lines. The hold contradicts the owner's rule that different parts of one file
merge (PB3670). The note names the fix, a member-level reservation and a re-derivation of the PB2103/PB2104 holds on
PB2296, PB2398 and PB2401.

