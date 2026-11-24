$ErrorActionPreference = 'Stop'
$launcher = Join-Path $PSScriptRoot 'plugins/jarvis-console'
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Install .NET Framework 4.8 on Windows x64 before building.'
}
$references = @('PresentationCore', 'PresentationFramework', 'WindowsBase', 'System.Speech') |
    ForEach-Object { '/r:' + (Join-Path $framework "WPF/$_.dll") }
Push-Location -LiteralPath $launcher
try {
    & $compiler /nologo /target:winexe /codepage:65001 /optimize+ /r:System.Xaml.dll /r:System.Windows.Forms.dll $references /out:Jarvis.exe Jarvis.cs BootWindow.cs
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }
} finally {
    Pop-Location
}
