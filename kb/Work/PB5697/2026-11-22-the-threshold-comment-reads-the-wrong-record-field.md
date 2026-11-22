## The threshold's re-measurement recipe reads a field that is a different quantity (2026-11-22)

Review lead R1-1 of the PB5697 report (w20262053a-PB5697-report.md), filed by the enqueue unit's lead clerk. Re-read on main
2026-11-22: `scripts/host_resources.py` lines ~198-205 say every gate "now records its slot-held job's peak (`peak_commit_bytes`,
PeakJobMemoryUsed, `scripts/process_job.py bound_peak_bytes`), so a re-measurement reads the records", and `DESIGN-test-build-ci`
section 3.14.6 says the same.

The two quantities differ. `peak_commit_bytes` is the slot-held JOB peak (bound at slot acquire). `GATE_PEAK_COMMIT_BYTES` and the
admission reservation are the WHOLE gate tree (`tree_commit` of the gate.py pid), which includes the self-tests started beside it
before the slot. On the implementer's own run the two differ by about 40 percent (12.1 GiB tree, 7.6 GiB job). Re-measuring from
the records would lower the threshold to the job figure and let one more gate in than the host can hold, the failure PB5697 fixed.
Correct the comment and the design section to say the records give the job peak only, or record the tree peak too.
