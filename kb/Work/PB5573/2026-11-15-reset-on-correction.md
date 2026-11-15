**2026-11-15: correction to the sibling sweep in `2026-11-14-landed.md` (line 3).** That record says the SUM clause's
RESET ON operand "refuses a reference-modified operand at every edition (COBOL0899 ..., probed at 85 and 2023), so it
needs no edition gate". The stated reason is false. §2023:13.18.54.3 SR8 reads "Data-name-3 may be qualified and
reference-modified" (`cite.py --check "§2023:13.18.54.3 SR8"` OK), and the compiler accepts a reference-modified RESET ON
operand when it matches a reference-modified CONTROL operand (`w20262040d-PB5572-report.md`, review R1-1). The no-gate
conclusion still holds, for a different reason: a matching RESET ON needs a reference-modified CONTROL operand, and the
gate this note landed refuses that operand at `--std 85` (COBOL0900), so a program that reaches RESET ON with a
reference modifier at 85 has already been refused at its CONTROL clause. The same sentence is in
`DEVLOG/3661-pb5572-pb5573-pb5564-three-std-85-under-rejections-in-the.md` (a stamped entry, which is history and is not
edited; this file is the correction). No code change follows.
