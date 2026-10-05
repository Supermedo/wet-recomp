$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$xex = Join-Path $root "game\default.xex"
if (-not (Test-Path $xex)) {
    Write-Host "game\default.xex is missing. Add your ISO in the launcher first."
    exit 2
}

$sdkDir = Join-Path $root "thirdparty\rexglue-sdk"
if (-not (Test-Path (Join-Path $sdkDir "CMakeLists.txt"))) {
    Write-Host "ReXGlue SDK missing. Run setup.ps1 first."
    exit 2
}

foreach ($llvmBin in @(
    (Join-Path $env:USERPROFILE "llvm-23\LLVM\bin"),
    "C:\Program Files\LLVM\bin"
)) {
    $clang = Join-Path $llvmBin "clang.exe"
    if (-not (Test-Path $clang)) { continue }
    $verLine = & $clang --version 2>$null | Select-Object -First 1
    if ($verLine -match "clang version (\d+)" -and [int]$Matches[1] -ge 20) {
        $env:PATH = "$llvmBin;$env:PATH"
        break
    }
}

if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) { throw "cmake not found" }
if (-not (Get-Command clang -ErrorAction SilentlyContinue) -and
    -not (Get-Command clang++ -ErrorAction SilentlyContinue)) {
    throw "clang not found"
}

cmake --preset win-amd64-release "-DREXSDK_DIR=$sdkDir" "-DCMAKE_BUILD_TYPE=Release"
if ($LASTEXITCODE -ne 0) { throw "cmake configure failed" }

$buildDir = Join-Path $root "out\build\win-amd64-release"
cmake --build $buildDir --target wet
if ($LASTEXITCODE -ne 0) { throw "wet build failed" }

$exe = Join-Path $buildDir "wet.exe"
Copy-Item $exe (Join-Path $root "wet.exe") -Force
foreach ($dll in @("rexruntime.dll", "rexgpu-xenos.dll")) {
    $src = Join-Path $buildDir $dll
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $root $dll) -Force
    }
}

Write-Host "Built $exe"
