## kb/Work PB4884: a live session's branch triggers a false resume unit

The resume unit of 2026-11-06 ran because `next_unit.fresh_unlanded` counted a branch an attended session still owns. PB4884 records the mechanism and the fix to weigh; nothing else changes here.
