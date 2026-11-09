## PB5004 - a decoded message frame refuses a slot its image does not hold

PB4957 made `MessageBody.Carrying` the one construction of a body with slots, but `PeerFrameCodec.Reader.Body` still called the record constructor, so a frame
from a malformed or hostile peer entered the receiver with a slot standing for positions its image lacks. Re-probe on the Debug build: a Grant frame whose image
lacks its slot's token decoded without complaint. The decoder now refuses it as `InvalidDataException` (the caller closes the link, as for a bad party byte) and
builds the body through `Carrying`. A codec test decodes such a frame; the round-trip tests now use token-holding images. Closes no inventory row.
