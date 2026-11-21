---
heading: "### 9.2 The hardware components (§1985:I.1.5.2.5.1)"
---
"In order for an implementation to meet the requirements for this Standard COBOL, the implementor must specify the hardware
components that the implementation supports" (§1985:I.1.5.2.5.1). WiseOwl COBOL supports these components: **mass storage**,
on which every file of every organization is one host file (§7, DOC-A.1-24); the **host's character set**, which holds every
character of the COBOL character set (§9.3); the **binary and packed decimal data formats**; **line-stream display devices**,
CONSOLE, SYSIN, SYSOUT and SYSERR (DOC-A.1-189); and **vertical positioning in a print file**, the lines and pages a WRITE
ADVANCING writes into the file (DOC-A.1-130, DOC-A.1-221, DOC-A.1-222). It supports **no reel or unit device** (§2 rows 28–30
and 33–34, DOC-A.1-24), so the elements that need one "need not be implemented", and this table states their absence as the
same paragraph requires ("The absence of such elements from an implementation of Standard COBOL must be specified").

Each element of the Hardware Dependent Language Element List (§1985:C.3):

| §1985:C.3 item | Element | Component | Determination |
|---|---|---|---|
| 1 | Double character substitution | the host's character set | Never made: the character set holds every COBOL character (§9.3) |
| 2, 3 | USAGE BINARY, USAGE PACKED-DECIMAL | binary and packed decimal formats | Supported |
| 4 | Positioning by the DISPLAY statement | line-stream display devices | Positioning is not applicable to a line stream, so none is made: each DISPLAY writes its operands and ends the line (WITH NO ADVANCING leaves it open) |
| 5 | The PADDING CHARACTER clause | mass storage | A null facility in function: no file has blocks, so no padding character is written or bypassed (§3.6) |
| 6 | The STANDARD-1 phrase of RECORD DELIMITER | a reel device | Not supported: accepted and inert, with the warning COBOL1778 (§2 row 26; §9.1) |
| 7 | The MULTIPLE FILE TAPE clause | a reel device | Not supported in function: the placement of files on a reel is a null facility, and the rules other statements state of it are enforced (§3.4) |
| 8 | The CODE-SET clause | mass storage | Supported for the coded character sets §2 row 27 names, at every edition (DOC-A.1-183, DOC-A.1-184) |
| 9, 10, 11 | CLOSE REEL/UNIT, FOR REMOVAL, WITH NO REWIND | mass storage | Supported for the non-reel/unit medium every file is: each phrase executes and the status is '07', as §1985:VII.1.3.5 states for a file on a non-reel/unit medium (§2 rows 28–30, DOC-A.1-24) |
| 12, 13, 16, 17, 18 | DELETE, OPEN I-O, OPEN EXTEND, REWRITE, USE … I-O | mass storage | Supported |
| 14 | OPEN … REVERSED | mass storage | Supported: a sequential file's records are made available in reversed order |
| 15 | OPEN … WITH NO REWIND | mass storage | Supported for the non-reel/unit medium every file is, as items 9 to 11 (§2 rows 33–34) |
| 19 | WRITE … BEFORE/AFTER ADVANCING | vertical positioning in a print file | Supported (§2 row 37; DOC-A.1-221, DOC-A.1-222) |
| 20 | SEND … BEFORE/AFTER ADVANCING | a communication device | The message control system's devices are oriented to variable-length messages and have no vertical positioning, so the phrase is ignored (§3.3) |

A rule of a reel or unit device that the 1985 catalog holds is a row of §9.1 with this paragraph as its ground.
