## PB4918: a review stall is counted from the DONE report, while the implementer is still gating

Wave 20261905 mailed the operator two false STALLs, for w20261905d (PB4838) and w20261905g (PB4744), each "on its branch
review". Neither branch was in review: each implementer had written its DONE report and was still gating its tip. The
rolling wave starts a review only when the implementer returns. `progress.py` starts the 15-minute review clock at the
report file's mtime, so the clock ran out during the implementer's own gate. Filed as kb/Work PB4918 with the fix:
start the clock when the implementer returns. No code changes in this entry.
