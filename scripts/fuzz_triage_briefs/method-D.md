- `THEY_REJECT`: a <<EDITION>> feature GnuCOBOL 3.2 lacks (an ISO feature it refuses as unsupported) -> `GNUCOBOL-SIDE`
  with its message, class `feature-missing` or `rejects-legal-source`; but when WiseOwl ACCEPTED a program that is NOT legal
  (GnuCOBOL's refusal was right) -> `WISEOWL-DEFECT` (under_rejects: WiseOwl accepts illegal source) with the rule.
  Sample at least the first program of every signature of 2 rows or more and the single rows whose GnuCOBOL message is not
  a plain "unexpected <word>".
- `THEY_FAIL_RUN`: GnuCOBOL compiled but its run failed where WiseOwl ran: name the GnuCOBOL failure (`GNUCOBOL-SIDE`, class
  `crash` or `not-implemented-at-run-time` when the standard requires the result); a WiseOwl run that ALSO looks wrong by
  the standard is a `WISEOWL-DEFECT`.
- `THEY_HANG` / `BOTH_HANG`: confirm each is the program's own loop by the standard (`MODEL-ERROR`) or say what else it is.
