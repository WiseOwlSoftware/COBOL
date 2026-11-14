**2026-11-13, lead filed by the notes batch 20261113e.** UNVERIFIED LEAD: re-probe before fixing.

Source: Group C (PB5469), lead 3, `E:/COBOL-coord/scratch/op-20261113/leads-final-batch.md`. Verbatim: "2002/2014 COPY REPLACING identifier object forms are still read as a word: an inline method invocation (`obj :: "m" (args)`) and an object view (`obj AS cls`). Site: CopyProcessor.cs#ReadOperand. "AS is also a legal word-1 of the next pair, so this needs a design rather than another loop." Same root as PB5469."
