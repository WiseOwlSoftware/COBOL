## PB5220: the toolchain manifest follows the dev box to dotnet-sdk 10.0.404 and pwsh 7.6.10

The dev box updated to .NET SDK 10.0.404 and PowerShell 7.6.10 at 16:49, and every local gate after that ended NOT RUN
[toolchain] against `toolchain.env`'s 10.0.403 and 7.6.9; the PB5158 fix sat ungated. The manifest and its cloud copy now name the
host's versions. pwsh 7.7.0 and semgrep 1.185.0 are newer still and wait for a host install with no gate running.
