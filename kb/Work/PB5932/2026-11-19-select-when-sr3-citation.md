**2026-11-19, a lead found by the closing slice's first session (branch `listing-step-6b-close`): a SELECT WHEN diagnostic may cite the wrong syntax rule.**

`ResolveRecordFormatPass` refuses a SELECT WHEN clause whose condition-name names no level-88 entry with
`RecordFormatCitations.SelectWhen(level, "SR3")`, which renders `ISO §2023:13.18.51.3 SR3` (and the 2014 and 2002 equivalents). The
transcription's SR3 reads "A given condition-name-1 shall be specified in the SELECT WHEN clause of only one record description entry
associated with a given file-name" (`python scripts/spec/cite.py --find "Condition-name-1 shall be"` lists it as `§2023:13.18.51.3 3)`).
That is the one-record rule, not the rule that condition-name-1 names a level-88 entry, so the refusal may be cited to the wrong rule.
What the clause says the operand IS, §2023:13.18.51.1 ("specifies a condition-name condition under which a record description entry is
associated" with a record) and the general format's `condition-name-1`, would be the rule to cite. Not adjudicated here: the citation
is the text of a diagnostic, so changing it changes compiler output, and it belongs to the record-format clause's owner. No compiled
program behaves differently.
