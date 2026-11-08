## PB4926: the run unit's message-tag table holds one entry per party and serial block, not one per message

SlotImageTable kept `_tagIds` and `_tags` entries for every distinct (MCS, party, name, message serial) tag for the run unit's life, so a long SEND/RECEIVE batch grew it without bound. Re-probe on this build: `TagId` still inserted one entry per serial.

Fix: the low 12 bits of the message serial now ride in the 8-position token itself (epoch in the high half; a process-wide block number above the serial's low 12 bits in the low half). The table keys on (MCS, party, name, serial >> 12), so it grows by one entry per party per 4096 messages. TagOf rebuilds the tag from the block entry and the token's low bits, so a tag read back still replies to the message it was sent for, and SameParty/Unresolved behaviour is unchanged. The process-wide counter renamed `s_lastTagBlock` (drift allow-list updated); exhausting 2^20 block numbers throws rather than reusing a block. DOC-A.1-212 updated.

Test: MessageTagImageTests.ImagingNManyMessages_LeavesTheRunUnitTableBounded_AndEveryTokenReadsBackItsMessage (20000 messages, table growth at most 6, all tokens distinct, spot-checked round trips). No inventory rows closed. Gate: see branch report.
