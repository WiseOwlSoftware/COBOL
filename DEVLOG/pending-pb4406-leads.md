## PB4406-PB4408 filed: PB4383's positioning reach, its unpinned STOP RUN arm, and plan_wave's integrate-branch exclusion

Filed three open notes from the PB4383 branch review (R1-1 as PB4406, R1-2 as PB4407) and the wave 20261811 handoff
(PB4408). No code changed. A grep of `kb/Work` found no existing note for any of the three mechanisms, so no note was amended.
PB4408's cause was read in `plan_wave.py` but the stage that admitted the five ids was not traced; the note says so.
