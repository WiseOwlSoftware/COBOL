- **D-85-OPEN-OF-AN-OPEN-FILE — a second file connector's OPEN of a physical file another connector of the run unit holds
  open, each edition by its own text (kb/Work PB6161; the owner's decision of 2026-11-24, which replaced PB670's option (i) at
  COBOL-85).** The case: two or more SELECTs whose ASSIGN values reach one host path, which WiseOwl COBOL maps to one physical file
  (the content of the ASSIGN literal is the implementor's, `§1985:VII.2.3.3 SR3`). **COBOL-85: '41'.** 1985 has no file sharing
  facility, and it keys its status 41 to the physical file: "The terms 'file referenced by file-name' and 'file' mean the physical
  file regardless of the file-name used in the COBOL program" (`§1985:II.2`); "each subsequent OPEN statement execution for that
  same file must be preceded by the execution of a CLOSE statement" (`§1985:VII.4.3.4 GR5`); and I-O status 41 is "An OPEN
  statement is attempted for a file in the open mode" (`§1985:VII.1.3.5`). So while one connector holds the file open, every other
  connector's OPEN answers 41 in every pair of modes (INPUT, OUTPUT, I-O, EXTEND), and the file is not affected: a refused
  OPEN OUTPUT does not create it anew. The refused connector never associated the file with its file-name ("The successful
  execution of an OPEN statement associates the file with the file-name through the file connector", `§1985:VII.4.3.4 GR1`), so its
  CLOSE answers 42 and leaves the holder's file open; once the holder has closed, the same OPEN succeeds. **COBOL 2002, 2014 and
  2023: the sharing facility.** Their status 41 is keyed to the file connector ("An OPEN statement is attempted for a file
  connector in an open mode", `§2002:9.1.12.6`, `§2014:9.1.12.6`; `§2023:9.1.13.7 GR1` adds DELETE FILE), so it answers only a
  connector's own second OPEN. The second connector's OPEN is arbitrated instead: "the sharing mode and the open mode shall be
  allowed by all other file connectors that are currently associated with the physical file" (`§2002:9.1.14`, `§2014:9.1.14`,
  `§2023:9.1.15`), under the implementor default when no SHARING phrase or clause is written (`§2002:14.8.26.3 GR22`,
  `§2014:14.9.26.3 GR22`, `§2023:14.9.27.4 GR23`; WiseOwl's default, A.1 item 77: INPUT is SHARING WITH READ ONLY, every other mode
  SHARING WITH NO OTHER), and a refusal is the file sharing conflict condition, I-O status 61 (`§2002:9.1.12.8`, `§2014:9.1.12.8`,
  `§2023:9.1.13.9 GR1`). Two clause-less readers therefore share the file (00), and every other pair answers 61. Toward another run
  unit COBOL-85 promises nothing: no file lock is taken, and a host refusal by a process outside the run unit answers 30. One
  read of the edition decides it, `DialectBehavior.FileSharingFacility` through `FileConnector.SharingFacilityInEffect`, in
  `FileRegistry.SharedOpenAttempt`. Witnesses: `conformance:wiseowl/cases/14.9.27-OPEN-STATEMENT/pb6161_second_select_opens_an_open_file_85`
  (85), `conformance:wiseowl/cases/14.9.27-OPEN-STATEMENT/pb6161_second_select_opens_an_open_file` (2002, 2014, 2023), and
  `unit:SharingFacilityEditionTests` (every organization and every pair of modes at each edition, a third connector).
