## PB5097: a queued wave group reads as a stalled one

Filed PB5097. Wave 20261973 has eight groups at concurrency 6, and at 21:45 the loop mailed STALLs for groups G and H,
which the rolling wave had not started yet. `progress.py` dates every group without a report from its dispatch-ledger
render time and counts it as running, so each queued group passes the idle limit untouched. This is the third
false-STALL mechanism, after PB4918 and PB4944. Notes only; the fix is a start signal from the rolling wave.
