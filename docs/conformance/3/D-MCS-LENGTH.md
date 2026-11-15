- **D-MCS-LENGTH — message lengths** (kb/Work PB4744; DESIGN-message-control.md §0 Q7). §2023:14.9.38.4 GR1 makes a message of zero positions, or one "larger than the message
  server allows", EC-MCS-MESSAGE-LENGTH and the SEND unsuccessful; this implementation's server allows what `mcs.json` defines as the name's `maxLength`, else any length, and a reply to a requestor
  is measured against the requestor's own identifier-1. §2023:14.9.31.4 GR3 makes a message longer than identifier-1 unsuccessful with EC-MCS-MESSAGE-LENGTH and says nothing of the
  message: the message is taken (it does not stay queued, where the next RECEIVE FROM NULL would loop on it), identifier-1 receives its first positions, data-name-2 is unchanged,
  data-name-1 identifies the sender, and the server can answer it. A message's length is the number of positions of the sender's identifier-1, two per national character (A.1 item 213).
  An identifier-1 that is an elementary message tag is a message of 8 positions, its token; the tag travels as the message's one slot. A receiving message tag, elementary
  or in a group, is what the message holds at its own positions (§2023:14.9.31.4 GR1b), decoded as a file record's tag is (A.1 item 212) with the slots the message
  carries standing for the sending run unit's tokens (`AsyncMessaging.Resolve`, kb/Work PB4957): so a message cut to identifier-1 (GR3) delivers no tag that lay past the
  cut, eight X"00" positions are NULL, and positions that hold no carried token (spaces past a shorter message included) are an unresolved tag, as in a short file
  record. Witness `conformance-test:MessagingProcessTests`.
