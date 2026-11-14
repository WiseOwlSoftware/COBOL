- **D-PERFORM-RESUME-NEXT-WHEN — Exception-checking PERFORM — RESUME NEXT STATEMENT in a WHEN skips WHEN COMMON (§2023:14.9.28.4 GR17/GR19, spec silent)**:
  GR17 passes control to imp-4 (WHEN COMMON) "at the completion of the execution of imperative-statement-2"; a RESUME
  (§2023:14.9.33) is a transfer of control OUT of imp-2, so imp-2 does not "complete" and the GR17→imp-4 hand-off is not
  taken. **Pinned choice: a WHEN that RESUMEs (NEXT STATEMENT) does NOT run WHEN COMMON.**
