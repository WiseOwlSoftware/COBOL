- **D-MCS-TRANSPORT — the transport between run units is a peer AF_UNIX stream socket, in a per-user socket directory**
  (kb/Work PB4742; DESIGN-message-control.md §5, §6). §2023:3.118 defines the message control system as an "implementor-defined
  system that sends and receives messages exchanged between run units", so the transport is the implementor's. The runtime
  has no broker and keeps no message on disk: each run unit holds its own messages in memory and exchanges them with the
  run units of the same user on the same machine over `AF_UNIX` stream sockets (`SocketTransport`, one `PeerLink` per
  connection, a read pump on the thread pool). The socket directory is `<base>/wiseowl-mcs-<uid>/<h>/` on Unix (`<base>` is
  `$XDG_RUNTIME_DIR` when it exists and leaves room for the longest entry, else `/tmp`) and
  `Path.GetTempPath()` + `wiseowl-mcs\<h>\` on Windows, where `<h>` is the identity of the MCS directory
  (`WISEOWL_COBOL_MCS`); a server's listener is `s/<name>/<run id>` and a requestor's pending bell `p/<name>/<run id>`. A
  listener is bound, listens and is only then renamed to its final name, so a file under `s/` or `p/` always belonged to a
  listening socket and a connect that is refused means its owner has ended: whoever is refused deletes the file. A run unit
  that is killed reaches its partners as an ended stream, never as silence. A run unit serves a name when
  `WISEOWL_COBOL_MCS_SERVER` is set (or `MessagingSettings.ServedName`, a host setting that travels with the run unit
  as `RunUnit.Clock` does); the run unit that starts a server passes `WISEOWL_COBOL_MCS_NOTIFY`, which the server connects
  to once its listener is published. A socket directory this user cannot create with mode 0700 (Unix), or one too long for
  an `AF_UNIX` path, is EC-MCS-IMP at the run unit's first messaging statement, with the reason. Two users never share an
  MCS's sockets. Witnesses `unit:SocketTransportTests`, `unit:MessagingPeerTests`, and the platform facts in
  `unit:PlatformAssumptionTests`.
