- **D-WRITE-NO-END-OF-PAGE — WRITE with no END-OF-PAGE phrase (§2023:14.9.51; E.2 item 30)**: when the END-OF-PAGE condition occurs and no
  END-OF-PAGE phrase is present, control falls through to the next statement (the natural code path — no branch is
  emitted for an absent phrase). Version-invariant; met without a gate (no `>>FLAG-14` option exists for it).
