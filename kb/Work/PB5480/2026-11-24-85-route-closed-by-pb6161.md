## Finding (implementer, 2026-11-24, kb/Work PB6161): at COBOL-85 the zero-byte route to '04' is closed

The route this note found (a relative or indexed reader opened INPUT on a zero-byte file while a second connector of the run unit opens
the same file and writes records outside the reader's size) needs a second OPEN of a file in the open mode. Since PB6161 that OPEN
answers 41 at `--std 85` (§1985:VII.1.3.5, "An OPEN statement is attempted for a file in the open mode"; §1985:II.2), and every other
OPEN validates the attributes (39), so at 85 such a record never reaches a READ. The three 85 goldens built on the route
(`pb5480_relative_read_record_length_conflict`, `pb5480_indexed_read_record_length_conflict`,
`pb5480_depending_on_after_truncated_read_85`) were deleted and retired from 1985-GR-VIII.4.5.4-16 and 1985-GR-IX.4.5.4-18 (each keeps
its other witness), and `g85_bf18_04` lost its 04 leg. The READ arm stays: from 2002 a SHARING mode admits the route. Probed on the
branch: the same relative program with the writer first answers `R OPEN 39`.
