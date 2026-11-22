# PB5683 - one more KEPT row owes a code read: 1989-RV-A.2.46.3-3 (2026-11-21)

Found by wave 20262132 group b (PB5926, ruling D-B). The row was closed on DRV-1989-RV-A.2.46.3-3, so the CODE READS
audit owed it no read. The derivation's premise (no listing) is false since the full listing landed (#1906); the
derivation is withdrawn and the row now stands on its own witness,
`conformance:wiseowl/cases/A.2.46@1985-WHEN-COMPILED-FUNCTION/pb5926_when_compiled_listing_heading_85`, so
`code_reads.py plan --edition 1985` lists it among the KEPT rows owed a read (5 where it listed 4; `edition_status.py
--edition 1985` part 2 reads 9 where it read 8).

What the read should find (§1989:A.2.46.3 RV3, one instant for the listing and the object code):
`src/WiseOwl.Cobol.Frontend/Pipeline/CompilationInputs.cs` (`ReadCompilationTime`) reads the clock once per compilation and answers
that instant to the emitter's WHEN-COMPILED (`ProgramEmitter.cs:54`, `IntrinsicRenderer`'s "WhenCompiled" arm, `EmitCore.WhenCompiledStamp`)
and to the listing's heading and DATE-COMPILED replacement (`src/WiseOwl.Cobol.Compiler/Listing/ListingFacts.cs#CompilationTime`). The reader and
refuter confirm or contradict that against the code; the row's read goes in with the other four through
`code_reads.py import`.
