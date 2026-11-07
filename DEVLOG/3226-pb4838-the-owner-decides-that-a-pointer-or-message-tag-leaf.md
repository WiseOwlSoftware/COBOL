## Entry 3226 — 2026-11-06 17:27 PST — PB4838: the owner decides that a pointer or message-tag leaf round-trips through a file

The wave unit held PB4838 for the owner because the implementer found no precedent: GnuCOBOL and IBM persist raw address bytes, and a managed reference has none. The operator ran the case under GnuCOBOL 3.2.0 with `-std=cobol2002` and `-std=cobol2014`, and the pointer read back equalled the pointer written. The address token of DOC-A.1-216 has the same shape, so the round trip is reachable once the token can be decoded.

The owner chose the round trip. A file carries the token, and READ, RETURN and SORT GIVING decode it to the same reference within the run unit. A message tag uses the same mechanism. A token from another run unit keeps its image and raises an exception on dereference. The note returns to `open` so the fix lane plans it; the fix changes A.1 items 216 and 212. An object leaf was outside the question, and the note records what the fix must settle from the spec first.

