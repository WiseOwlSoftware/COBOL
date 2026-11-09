## Entry 3358 — 2026-11-08 21:36 PST — PB5004 - a decoded message frame refuses a slot its image does not hold

PB4957 made `MessageBody.Carrying` the one construction of a body with slots, but `PeerFrameCodec.Reader.Body` still called the record constructor, so a frame
from a malformed or hostile peer entered the receiver with a slot standing for positions its image lacks. Re-probe on the Debug build: a Grant frame whose image
lacks its slot's token decoded without complaint. The decoder now refuses it as `InvalidDataException` (the caller closes the link, as for a bad party byte) and
builds the body through `Carrying`. A codec test decodes such a frame; the round-trip tests now use token-holding images. Closes no inventory row.

Review round 1 (w20261936d): the short-token probe of `APayloadThatDoesNotParseIsRefused` held a `T` in its image, so its byte patch hit the image count and the
token-length guard went untested; the probe's image is now `abcdefgh`. The slot-in-image test is one member, `MessageSlot.IsHeldBy`, that `Carrying` and the decoder both call.

