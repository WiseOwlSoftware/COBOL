---
heading: "### 3.13 The I-O status values a COBOL-85 program is told (kb/Work PB5882)"
---
§1985:VII.1.3.5, §1985:VIII.1.3.4 and §1985:IX.1.3.4 each give "a list of the values placed in the I-O status for the previously named conditions" for their module, and a condition the implementor adds is "I-O Status = 9x. An implementor-defined condition exists. This condition must not duplicate any condition specified by the I-O status values 00 through 49"; the value of x is the implementor's (§1985:C.2 item 48). At `--std 85` the FILE STATUS item, the AT END and INVALID KEY tests and the USE procedures therefore see only a value of the file's module list or one of the 9x below. The runtime's conditions that 1985 does not list are reported this way (docs/FILES_DESIGN.md D33; `IoStatus1985`, VCR row 7.86):

| Condition (the value COBOL-2002 and later report) | Reported at `--std 85` | Critical |
|---|---|---|
| '90' the LINAGE value rules (DOC-A.1-110) | 90 | yes |
| '91' a character with no byte image in the file's coded character set (DOC-A.1-110) | 91 | yes |
| '45' record identification failure (FORMAT and SELECT WHEN) | 92 | yes |
| '51' the record is locked by another file connector | 93 | no |
| '52' the implementor-detected deadlock | 94 | no |
| '53' the run unit holds its maximum number of record locks | 95 | no |
| '54' the file connector holds its maximum number of record locks | 96 | no |
| '61' an OPEN's file sharing conflict | 97 | no |
| '62' a DELETE FILE's file sharing conflict | 98 | no |
| '71' a WRITE or REWRITE of a file framed by the implementor-name LINE-SEQUENTIAL whose record holds a character outside the line character set (DOC-A.1-115), such as the line delimiter | 99 | yes |
| '06' a line longer than the maximum record size, truncated | 04, the record length conflict of §1985:VII.4.4.4 GR13 | no |
| '09' a successful READ of a line holding a character outside the line character set | 00 | no |
| '31' the dynamic assignment's permanent error | 30 | yes |
| '34' and '07' on a relative or indexed file, whose module does not list them | 30 and 00 | as the value |

A 9x is critical (§1985:VII.1.3.5: "any that begin with the digit 9 that the implementor defines as critical") exactly when the condition it reports is fatal at the later editions, so the record operation and file sharing conflicts stay continuable. The statements and modules that produce '45', '62' and '31' (FORMAT, SELECT WHEN, DELETE FILE, the dynamic assignment) are later syntax, and the file sharing facility behind '61' is a COBOL-2002 module that is not in effect at COBOL-85 (DialectBehavior FileSharingFacility); every condition still has its value, so none can reach a 1985 program as a value the 1985 text does not list. The tests are `conformance-suite/cases/VII.1.3@1985-LANGUAGE-CONCEPTS/pb5882_io_status_1985_values` and `IoStatus1985Tests`, which re-reads the three module lists from the 1985 text and requires every runtime status, on every organization, to report a listed value or one of these 9x of the same class and criticality.
