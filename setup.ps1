$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sdkDir = Join-Path $root "thirdparty\rexglue-sdk"
$tag = "v0.10.0"
$xex = Join-Path $root "game\default.xex"

if (-not (Test-Path $xex)) {
    Write-Host "game\default.xex not found. Add your ISO in the launcher first."
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

foreach ($tool in @("git", "cmake")) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        Write-Host "Missing $tool on PATH."
        exit 1
    }
}

if (-not (Test-Path (Join-Path $sdkDir ".git"))) {
    git clone --branch $tag --depth 1 https://github.com/rexglue/rexglue-sdk.git $sdkDir
    if ($LASTEXITCODE -ne 0) { throw "git clone failed" }
}

git -C $sdkDir submodule update --init --recursive --depth 1
if ($LASTEXITCODE -ne 0) { throw "submodule init failed" }

$patchDir = Join-Path $root "patches"
Get-ChildItem -Path $patchDir -Filter "*.patch" | Sort-Object Name | ForEach-Object {
    git -C $sdkDir apply --check --ignore-whitespace $_.FullName 2>$null
    if ($LASTEXITCODE -eq 0) {
        git -C $sdkDir apply --ignore-whitespace $_.FullName
        if ($LASTEXITCODE -ne 0) { throw "failed to apply $($_.Name)" }
    }
}

$buildDir = Join-Path $root "out\build\win-amd64-release"
cmake --preset win-amd64-release "-DREXSDK_DIR=$sdkDir"
if ($LASTEXITCODE -ne 0) { throw "cmake configure failed" }

cmake --build $buildDir --target rexglue
if ($LASTEXITCODE -ne 0) { throw "rexglue CLI build failed" }

$cli = @(
    (Join-Path $sdkDir "out\win-amd64\rexglue.exe"),
    (Join-Path $buildDir "rexglue.exe")
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $cli) { throw "rexglue.exe not found" }

& $cli init --force --project-name wet --project-root $root --xex-path $xex --game-root (Join-Path $root "game")
if ($LASTEXITCODE -ne 0) { throw "rexglue init failed" }

$extraPath = Join-Path $root "patches\extra-functions.toml"
$manifestPath = Join-Path $root "wet_manifest.toml"
if (Test-Path $extraPath) {
    $manifest = Get-Content -Raw $manifestPath
    $extra = Get-Content -Raw $extraPath
    $changed = $false
    foreach ($m in [regex]::Matches($extra, '(?m)^\[entrypoint\.functions\.(0x[0-9A-Fa-f]+)\]\s*\r?\nname\s*=\s*"([^"]+)"')) {
        $addr = $m.Groups[1].Value
        $name = $m.Groups[2].Value
        $header = "[entrypoint.functions.$addr]"
        if ($manifest -notmatch [regex]::Escape($header)) {
            $manifest += "`n$header`nname = `"$name`"`n"
            $changed = $true
            Write-Host "Added guest function $addr to manifest"
        } elseif ($manifest -match "(?s)$([regex]::Escape($header))\s*\r?\nname\s*=\s*`"unresolved_target_[^`"]+`"") {
            $manifest = [regex]::Replace(
                $manifest,
                "(?m)($([regex]::Escape($header))\s*\r?\nname\s*=\s*)`"unresolved_target_[^`"]+`"",
                "`${1}`"$name`"")
            $changed = $true
            Write-Host "Promoted $addr from unresolved stub to $name"
        }
    }
    if ($changed) {
        Set-Content -Path $manifestPath -Value $manifest -NoNewline
    }
}

cmake --build $buildDir --target wet_codegen
if ($LASTEXITCODE -ne 0) { throw "codegen failed" }

Write-Host "Setup done. Run build.ps1"
