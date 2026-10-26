## The GUI guard's two medium review leads filed as PB3800: it still passes the Obsidian incident's own command form

PB3798's review capped with two medium leads: a PowerShell assignment (`$p = Start-Process obsidian -PassThru`) and a
spaceless call operator (`&('obsidian')`) both pass the new guard, and the first is the form the incident used.
PB3800 refuses both and asks whether an allow list of console tools should replace the deny list.
