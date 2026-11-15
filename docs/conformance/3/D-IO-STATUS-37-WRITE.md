- **D-IO-STATUS-37-WRITE — I-O status '37' write capability on OPEN (§2023:14.9.27.4 GR16; §2023:9.1.13.6 item 6 a)**: emitted at all editions,
  for every organization. Presence and *capability* are two questions and a file answers them differently: a
  read-only file is `FilePresence.Present` and fully observable, yet GR16 requires '37' when *"the file does not
  support"* the statements the I-O mode permits, and §2023:9.1.13.6 item 6 a) 1. requires the same for EXTEND and
  OUTPUT. **The question is asked once, in the OPEN contract, above the organizations**: `FileConnector.Open`
  calls `HostFile.PermitsWrite` (`Runtime/IO/FileSupport.cs`) for I-O, EXTEND and OUTPUT on a file that is
  Present, before `OpenCore`. The probe is a real write open (`FileMode.Open`, `FileAccess.Write`,
  `FileShare.ReadWrite`, nothing written), because a read-only attribute, a mode bit and a deny-write ACE are
  three mechanisms with one consequence, and GR25 (*"the file is not affected"*) leaves content and timestamp
  untouched either way. INPUT is deliberately excluded — a read-only file supports every statement Table 20
  permits in the input mode, and item 6 a) 3.'s read-capability question is answered by the organization's own
  eager read. Witnesses: `unit:FileAuthorityPresenceTests` (the nine cells — three organizations × I-O/EXTEND/
  OUTPUT — plus the INPUT and writable-file over-fire guards) and `unit:HostFileProbeDriftTests`, which keeps
  the call singular (kb/Work PB328: the sequential arm answered '37' by accident because its I-O and EXTEND
  streams are write opens, while the keyed arms only read their store and answered '00', then '00' for READ and
  REWRITE, and surfaced the loss as a '30' at CLOSE on a byte-identical file).
