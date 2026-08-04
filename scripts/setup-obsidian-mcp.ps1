<#
.SYNOPSIS
    Bootstrap the Obsidian Local REST API MCP server for this repo (see .mcp.json).

.DESCRIPTION
    The vault root IS the repo root, so the plugin exposes this project's notes, docs, spec transcription and
    JSON SSOTs over MCP. Two things are needed and BOTH are machine-local, so neither is committed:

      * the plugin's 64-char API key, which lives in its own gitignored data.json
      * the plugin's SELF-SIGNED TLS certificate, so Node will trust https://127.0.0.1:27124

    This script verifies the API is reachable, exports the certificate to .claude/local/ (gitignored), and
    prints the exact environment-variable commands for PowerShell and bash.

    ⛔ IT PRINTS THE KEY. That is the point — the key is a local secret you paste into your own environment —
    but it means the output should not be pasted into an issue, a commit message, or a chat log.

    ⚠ THE MCP SERVER IS OPTIONAL BY DESIGN. It requires Obsidian to be RUNNING. Nothing in the build, the test
    gates or CI may depend on it: every generator in scripts/ must work headless. Treat it as a convenience for
    reading and searching the vault during a session, never as a source of truth.

.EXAMPLE
    pwsh scripts/setup-obsidian-mcp.ps1
#>
[CmdletBinding()]
param(
    [int] $Port = 27124,
    [switch] $Quiet
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$dataJson = Join-Path $repo '.obsidian/plugins/obsidian-local-rest-api/data.json'
$certDir = Join-Path $repo '.claude/local'
$certPath = Join-Path $certDir 'obsidian-rest-api.crt'
$base = "https://127.0.0.1:$Port"

function Fail([string] $msg) { Write-Host "FAILED: $msg" -ForegroundColor Red; exit 1 }

if (-not (Test-Path $dataJson)) {
    Fail @"
the plugin's data.json was not found at
    $dataJson
Install 'Local REST API' in Obsidian (Settings -> Community plugins) and enable it. The vault is this repo,
so it installs into .obsidian/plugins/ — which git ignores (only .obsidian/community-plugins.json is tracked,
deliberately, so the plugin LIST survives a fresh clone).
"@
}

# ── The certificate. Served UNAUTHENTICATED, which is why it is fetched before the key is used. ───────────────
New-Item -ItemType Directory -Force -Path $certDir | Out-Null
try {
    # -SkipCertificateCheck is correct HERE and only here: we are fetching the very certificate we do not yet
    # trust, from loopback. Every other call below validates against it.
    $crt = Invoke-WebRequest -Uri "$base/obsidian-local-rest-api.crt" -SkipCertificateCheck -TimeoutSec 8
} catch {
    Fail @"
could not reach $base — is Obsidian running with the Local REST API plugin enabled?
(The MCP server is optional; the compiler's generators and gates never require it.)
"@
}
[IO.File]::WriteAllBytes($certPath, $crt.Content)
if (-not $Quiet) { Write-Host "certificate -> $certPath ($($crt.Content.Length) bytes)" -ForegroundColor Green }

$apiKey = (Get-Content -Raw $dataJson | ConvertFrom-Json).apiKey
if ([string]::IsNullOrWhiteSpace($apiKey)) { Fail "data.json carries no apiKey — open the plugin's settings and generate one." }

# ── Prove the whole chain works, THROUGH THE MECHANISM CLAUDE CODE ACTUALLY USES. ─────────────────────────────
# ⛔ NOT Invoke-RestMethod. NODE_EXTRA_CA_CERTS is a *Node* variable; PowerShell's HTTP stack is .NET and ignores
# it entirely, so a .NET probe passes or fails for reasons that say nothing about whether the MCP client will
# connect. The first version of this script did exactly that and reported FAILED on a working setup. Claude Code
# runs the MCP client on Node, so the probe runs on Node — verified to fail with DEPTH_ZERO_SELF_SIGNED_CERT
# without the variable and to return status OK with it.
$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) {
    Write-Host "WARNING: node not found, so the TLS chain could not be verified the way Claude Code will use it." -ForegroundColor Yellow
} else {
    $js = @'
fetch(process.argv[1], { headers: { Authorization: 'Bearer ' + process.argv[2] } })
  .then(r => r.json())
  .then(j => { console.log('OK ' + j.manifest.version); })
  .catch(e => { console.error('ERR ' + (e.cause?.code || e.message)); process.exit(1); });
'@
    $prev = $env:NODE_EXTRA_CA_CERTS
    $env:NODE_EXTRA_CA_CERTS = $certPath
    try {
        $out = & node -e $js "$base/" $apiKey 2>&1
        if ($LASTEXITCODE -ne 0) { Fail "the cert was exported but Node still rejected the endpoint: $out" }
        if (-not $Quiet) { Write-Host "endpoint    -> $out (verified on Node, the MCP client's runtime)" -ForegroundColor Green }
    } finally { $env:NODE_EXTRA_CA_CERTS = $prev }
}

Write-Host ''
Write-Host 'Set these, then RESTART Claude Code so it re-reads .mcp.json:' -ForegroundColor Cyan
Write-Host ''
Write-Host '  # PowerShell (this session)'
Write-Host "  `$env:OBSIDIAN_API_KEY = '$apiKey'"
Write-Host "  `$env:NODE_EXTRA_CA_CERTS = '$certPath'"
Write-Host ''
Write-Host '  # PowerShell (persist for your user)'
Write-Host "  [Environment]::SetEnvironmentVariable('OBSIDIAN_API_KEY', '$apiKey', 'User')"
Write-Host "  [Environment]::SetEnvironmentVariable('NODE_EXTRA_CA_CERTS', '$certPath', 'User')"
Write-Host ''
Write-Host '  # bash'
Write-Host "  export OBSIDIAN_API_KEY='$apiKey'"
Write-Host "  export NODE_EXTRA_CA_CERTS='$($certPath -replace '\\','/')'"
Write-Host ''
Write-Host 'Then verify inside Claude Code with:  /mcp' -ForegroundColor Cyan
