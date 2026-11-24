## Entry 4043 — 2026-11-24 10:05 PST — PB6017: a variable-length EXTERNAL record's byte count is not compared at run time

A record holding a dynamic-capacity table, described with another element size by a second program, raised
EC-EXTERNAL-FORMAT-CONFLICT at the CALL at --std 2023 (probe, `extdyn.cob`: `A HANDLED EC-EXTERNAL-FORMAT-CONFLICT`, EXTDB
never ran), where §2023:14.6.13.2 GR6 names EC-DATA-INCOMPATIBLE at the reference. Ruling (kb/Work PB6017): §2023:13.18.22.4
GR6's "same number of bytes" has no count to compare for a record whose size is set at run time, so the table's own rule applies.
`ExternalDescriptor.ByteCount` is now `int?` (null for a variable-length record, as the compile-time comparison of PB5996 already
treated it), and `ExternalTable.Describe` compares two counts only when both exist; VALUE, strong-type and CONSTANT RECORD
identities are still compared. Golden `pb6017_dyn_table_external_2023` (2023, both conditions checked: B and C handled as
EC-DATA-INCOMPATIBLE, D clean, the CALLs complete), refused at 2002/2014 with COBOL0878; unit test in `ExternalTableTests`.
Rows re-verdicted: none. No diagnostic code used.

