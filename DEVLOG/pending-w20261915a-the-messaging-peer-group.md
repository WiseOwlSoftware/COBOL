## The messaging peer group: review round 3 of PB4744 after PB4838, a long Windows temp path, a far pointer token and two citations

Wave 20261915 group A finishes PB4744's branch (review round 3) and the PB4919 continuation, merged onto current main, and fixes PB4921, PB4922, PB4925 and PB4928.
PB4855 was re-probed and stays open.

**The merge.** Main's PB4838 had landed while PB4744's branch was in review: a message-tag leaf now carries its 8-position TOKEN in a group image, and a group
holding only tags has a `FromImage` that decodes it (the file record's inverse). Five files conflicted. The resolution keeps both halves: `FromImage` for the file
record, and `FromMessageImage` for the message, which skips the token positions because a token names a tag of the SENDING run unit. The tags still travel as
the message's slots.

**R3-1 (blocking).** The reviewer's program has a tag in an ELEMENT of an OCCURS DYNAMIC table. It no longer failed to compile after the merge, because the element
group now had `FromImage`, but that decoded the sender's token into the receiver. `TableElementFromImage` now stores a tag-holding element by its
`FromMessageImage`. The two-process test `ADynamicTableWhoseElementsHoldMessageTags_CarriesEachTagBesideItsElement` checks LEN 0021, element 1's tag NULL and
element 2's tag set.

**R3-2.** §2023:14.6.11 7) sizes the run unit's implicit reply as "the size of the receiving data item in the associated RECEIVE statement". An area with no
allocated maximum was recorded as 0 positions. Its size is known only after the store, which recreates each dynamic table and raises it to its FROM minimum
(§2023:14.6.9.2). So the program now states it (`AsyncMessaging.Received`, the length of the current-extent image). In the test, a 4-position message into a FROM 3
table is answered with 6 spaces.

**A merge consequence.** PB4838 also made an elementary message tag 8 token positions (DOC-A.1-212). Before the merge it was a zero-position message that SEND GR1
refused; now its SEND sent the token and no tag. An elementary tag is therefore an 8-position message whose tag travels as its one slot, and a RECEIVE into one
takes the tag back from the slot (`AsyncMessaging.TagOf`). Test `AnElementaryMessageTag_IsAnEightPositionMessage_WhoseTagTravelsAsItsSlot`.

**PB4921.** The re-probe showed that the scenario does not occur. The RECEIVE wait is already bounded by `CobolTiming.Interval`, the one rule of the maximum
meaningful value (§2023:14.9.9.4 GR1, one day), so the 49.7-day timer cap in `MessagingPeer.Arm` was unreachable. `Arm` now clamps to `CobolTiming.MaxSeconds`.

**PB4922.** A Windows temp path too long for the socket budget, which is any user name over 16 characters in the default layout, made every messaging statement
EC-MCS-IMP. The socket directory now falls back to `%ProgramData%\wiseowl-mcs-<u>\<h>\`, the twin of Unix's `/tmp` fallback, where `<u>` hashes the user's SID. The
user level is created with a protected access list that grants the user alone, and an existing one that inherits, belongs to another user or grants anyone else is
refused. Re-probe: the peer, transport and platform tests under a 55-byte TEMP went from 18 red (the note's measurement) to 62/62 green.

**PB4925.** A data-pointer displaced 2^32 or more from its cell decoded to another area's window. Such a pointer is not a valid address of its storage
(§2023:13.18.5.4 GR4), so it now takes a token base of its own (`PointerImage.FarBase`) and round-trips exactly.

**PB4928.** The national and boolean data gates cited §2023:8.5.2 for "category national" and "category boolean". They now cite §2023:8.5.2.10 and §2023:8.5.2.5,
both checked, and the registry is regenerated.

**PB4855** (re-probe: `SORT T` Format 2 still runs no debugging section) needs the `WholeTablePlace` redesign its note names. That is a separate mechanism, and it
stays open.

**Self-review** covered architecture (one predicate `HoldsOnlyMessageTagSlots` for every tag-skipping arm; the socket fallback in the one resolver), full code
(every citation checked with `cite.py`), performance (the far-pointer table grows only with far pointers that are imaged) and duplication (none added).
