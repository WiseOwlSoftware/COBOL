- **D-85-MCS-SEEDED-DISABLED — a message `mcs.json` seeds for a terminal it defines disabled, at COBOL-85 (kb/Work PB5363).**
  This entry speaks for COBOL-85. A message that `mcs.json` seeds for a terminal it defines `enabled: false` is a message the
  message control system already holds when the run unit starts, and a RECEIVE of a CD FOR I-O whose SYMBOLIC TERMINAL names that
  terminal takes it (status key '00') before any ENABLE. The 1985 text leaves this to the implementor: "The conditions under which
  data is not made available are defined by the implementor" (`§1985:XIV.3.5.4 GR5`). ENABLE and DISABLE act on the link between
  the message control system and the devices: ENABLE "establishes logical connection between the message control system and one
  or more given communication devices" (`§1985:II.7.3`), and "The logical path for the transfer of data between the COBOL programs
  and the MCS is not affected by the DISABLE statement" (`§1985:XIV.3.2.4 GR1`). The enabled-queue condition of `§1985:II.7.5.1`
  ("enqueues the message in the proper input queue (provided that input queue is enabled)") governs device input arriving while
  the run unit executes; seeding is this implementation's means of placing messages, which the standard does not describe, and the
  seeded message is already held. `DESIGN-communication-facility.md` "Logical paths" (kb/Work PB5315) is the design. GnuCOBOL has
  no communication module, so the precedence of CLAUDE.md rule 1 offers no other reading. Decided by the controller (account 2) on
  2026-11-21 from the adjudication of wave 20262131 (decisions6). The test is
  `conformance-suite/cases/XIV.3.5@1985-RECEIVE-STATEMENT/pb5363-mcs-seeded-message-disabled-terminal-85`. Source: `kb/Work/PB5363.md`.
