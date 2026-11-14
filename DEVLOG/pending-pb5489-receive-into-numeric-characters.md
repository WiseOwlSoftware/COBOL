## RECEIVE INTO a numeric display item keeps the message's characters (PB5489)

`RECEIVE cd MESSAGE INTO K-NUM` (--std 85) and `INTO K-NUM (1:2)` (2002 and 85) of `PIC 9(4) VALUE 1234` with the message AB printed 0034. Section 1985:XIV.3.5.4 GR3 transfers the message "to the receiving character positions ... aligned to the left without space fill", which leaves AB34, as `MOVE "AB" TO K-NUM (1:2)` does.

Re-probe on a fresh build: both repros printed `[0034]`. The generated code showed the cause: K-NUM stayed a native `long`, so the receipt's `CobolNum.StoreImage` parsed digits only. `UsageCollectionPass.Visit(BoundCommunicationStatement)` collected the CD area and INTO only as whole-group operands. Both now go through `Channel`, the elementary character-channel set that `StorageFormPass` promotes to image storage, the same arm STRING's identifier-3 uses (PB4089). After the fix both legs print `[AB34]`.

Golden: `XIV.3.5@1985-RECEIVE-STATEMENT/pb5489_receive_into_numeric_characters_85` (whole item and slice, expected `AB34` from GR3). No row closes: the inventory has no 1985 rows. Sibling not probed: RECEIVE of the 2023 messaging format (`BoundReceiveMessage`) stores through the same image store and needs a requester run unit to exercise; registered as a lead.
