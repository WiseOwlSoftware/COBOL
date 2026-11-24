## Finding (implementer, 2026-11-24, kb/Work PB6161): option (i) is superseded at COBOL-85

PB670's question rested on "1985's silence". PB6130's adjudication and its refuter found 1985 is not silent: "file" means "the
physical file regardless of the file-name used in the COBOL program" (§1985:II.2), a second OPEN of the file must follow a CLOSE
(§1985:VII.4.3.4 GR5), and I-O status 41 is "An OPEN statement is attempted for a file in the open mode" (§1985:VII.1.3.5).

The owner answered on 2026-11-24, verbatim, "Yes, return 41". At `--std 85` a second connector's OPEN of a physical file another
connector of the run unit holds open now answers 41 in every pair of modes, replacing option (i)'s 00. Option (i)'s other halves
stand: no file lock against other run units at 85, and a host refusal from outside the run unit answers 30. The machinery option
(i) needed to admit an 85 sibling is deleted. Golden `pb670_cobol85_no_sharing_facility` is replaced by
`wiseowl/cases/14.9.27-OPEN-STATEMENT/pb6161_second_select_opens_an_open_file_85`. The work and the per-edition derivation are in
kb/Work PB6161.
