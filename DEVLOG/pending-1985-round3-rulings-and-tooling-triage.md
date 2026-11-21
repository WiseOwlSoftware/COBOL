## Two more 1985 wrong answers in line-sequential files, two questions closed, and the 1985 tooling notes triaged

Round 3 of the open 1985 questions (one adjudicator, main at 8244f1aa4b) found two wrong answers on a RECORD DELIMITER IS
LINE-SEQUENTIAL file at `--std 85`. PB5984, reproduced: three WRITE ... AFTER ADVANCING records read back as five, the two extras
of length 0 with status 04, because the ADVANCING write treats the file as a print stream and writes the delimiter as
positioning. PB5915: a REWRITE with a shorter record returns 00 and pads the line, where §1985:VII.4.5.4 GR10 requires 44 and the
file left unchanged. Both are in the line-sequential connector and are planned as one group. PB4244 (statement verbs missing
from a condition-boundary list) misparses no legal 1985 program and is retired; PB5321's code is right and its two documents
are owed their corrections.

A triage of the 37 process-only notes that touch 1985 found 25 still true, which now sit in three fix groups (the citation
checkers, comment and document drift, two missing goldens) and one held group (catalog tooling, which shares files with
PB5449). PB657 and PB765 are obsolete and PB4872 is done: retired. The rest belong to PB479, to another edition, or to an
upstream report.
