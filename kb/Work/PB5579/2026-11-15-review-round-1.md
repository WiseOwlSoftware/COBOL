## Review round 1 (PB5579)

Fixed in `SendingValueTemp.MaterializeLiteral` (`src/WiseOwl.Cobol.Compiler/Binding/Procedure/SendingValueTemp.cs`): a zero-length literal gets a dynamic-length carrier of current length 0; a boolean literal is category boolean, usage display. Goldens `conformance-suite/cases/14.9.51-WRITE/pb5579_write_file_exceptions.cob` (44 low/high, 48 with USE, 22, zero-length, boolean) and `conformance-suite/cases/14.9.35-REWRITE/pb5579_rewrite_file_exceptions.cob` (44, 23 INVALID KEY).
