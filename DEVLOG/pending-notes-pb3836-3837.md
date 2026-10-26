## PB3837: the code takes the product's name (WiseOwl.Cobol); PB3836: the register's missing harm class

The owner retired Cobol.Net, CobolNet and COBOLNET as names (2026-10-26): "As the product is no longer named COBOL.NET,
we also should not have Cobol.Net.* assemblies, should not have CobolNet namespace, in fact, likely should not be using
COBOLNET as a name anywhere ongoing", and chose WiseOwl.Cobol as the root name. PB3837 records the decision, what it
overturns (a 2026-09-26 line that kept the code names, which the owner never set) and the measured reach, and it runs as
one re-runnable rename after the diagnostic-prefix rename PB3575 lands.

PB3836 records why two approved fuzz fixes were filed `process_only: true`: the register has no harm class for a
diagnostic that names the wrong cause, so `work.py check` offered a false flag as the only way through. The fix adds the
class and re-reads the 23 open notes with compiler sites that carry `process_only`.
