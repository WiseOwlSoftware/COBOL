# PB6056 addendum - the stale arm of the sweep was wrong (PB6090)

The sweep above put a record gone `stale` (its rule's text changed) among the changes "made by a deliberate catalog or store edit, in
the diff". The edit is in the diff; its effect on the stored reads is not: PR #1953 corrected six 1985 rules, their `rule-digest`s
changed, and 1985 gained six code-read terms with every check green. The landing check now refuses it too (`code_reads.staled_by`,
kb/Work PB6090); the sweep's other arms stand.
