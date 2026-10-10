param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$ConservarArchivos
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'No se encontró MSBuild de Visual Studio.' }
& $msbuild (Join-Path $repoRoot 'TaskDB1.sln') /t:Rebuild /p:Configuration=$Configuration /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'La compilación no terminó correctamente.' }

$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = Join-Path $tempRoot ('TaskDB1-pruebas-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $output = Join-Path $repoRoot "TaskDB1\bin\$Configuration"
    Copy-Item -LiteralPath (Join-Path $output 'TaskDB1.exe') -Destination $testRoot
    Copy-Item -LiteralPath (Join-Path $output 'TaskDB1.exe.config') -Destination (Join-Path $testRoot 'VerificarTaskDB.exe.config')
    Copy-Item -LiteralPath (Join-Path $output 'Database') -Destination $testRoot -Recurse
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    & $compiler /nologo /out:"$testRoot\VerificarTaskDB.exe" /r:"$testRoot\TaskDB1.exe" /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $PSScriptRoot 'VerificarTaskDB.cs')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la verificación.' }
    & (Join-Path $testRoot 'VerificarTaskDB.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Una o más comprobaciones fallaron.' }
    if ($ConservarArchivos) { Write-Output "Archivos de la prueba: $testRoot" }
}
finally {
    if (-not $ConservarArchivos) {
        $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
        if (-not $resolvedTestRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
            (Split-Path -Leaf $resolvedTestRoot) -notlike 'TaskDB1-pruebas-*') {
            throw 'La carpeta temporal no es válida para la limpieza.'
        }
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
