<#
.SYNOPSIS
    Xac minh tinh toan ven cua cac hop dong Kafka va gRPC da nhap khau trong mot microservice doc lap.

.DESCRIPTION
    Kich ban nay hoat dong doc lap 100% ma KHONG can ket noi mang hay su ton tai cua repository huong dan.
    Nhiem vu:
    1. Tim va doc tep 'contracts.manifest.json'.
    2. Kiem tra su ton tai cua tat ca cac tep hop dong da ghi nhan trong manifest.
    3. Tinh lai ma bam SHA-256 cho tung tep hop dong tren o dia.
    4. So sanh voi ma bam goc duoc phat hanh tu repository huong dan.
    5. Phat hien tep bi thieu, bi sua doi cuc bo, hoac bi can thiep trai phep.
    6. Tra ve ma thoat (exit code) khac 0 neu co bat ky vi pham nao.

.PARAMETER Path
    Duong dan toi thu muc goc cua dich vu (hoac truc tiep thu muc 'contracts/'). Mac dinh la thu muc hien tai ('.').

.EXAMPLE
    .\scripts\verify-contracts.ps1

.EXAMPLE
    .\scripts\verify-contracts.ps1 -Path C:\PRN232\f-alumni-chat-service
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false, Position = 0, HelpMessage = "Duong dan thu muc dich vu hoac thu muc contracts/")]
    [string]$Path = "."
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host "     FPT Alumni Connect -- Contract Integrity Verification      " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor DarkCyan

# 1. Dinh vi contracts.manifest.json va thu muc goc contracts
$ResolvedInput = Resolve-Path $Path -ErrorAction SilentlyContinue
if ($null -eq $ResolvedInput) {
    Write-Host "[FAIL] Duong dan khong ton tai: $Path" -ForegroundColor Red
    exit 1
}

$BasePath = $ResolvedInput.Path
$ManifestPath = $null
$ContractsDir = $null

if (Test-Path (Join-Path $BasePath "contracts.manifest.json")) {
    $ManifestPath = Join-Path $BasePath "contracts.manifest.json"
    $ContractsDir = $BasePath
}
elseif (Test-Path (Join-Path $BasePath "contracts\contracts.manifest.json")) {
    $ManifestPath = Join-Path $BasePath "contracts\contracts.manifest.json"
    $ContractsDir = Join-Path $BasePath "contracts"
}
else {
    Write-Host "[FAIL] Khong tim thay tep 'contracts.manifest.json'!" -ForegroundColor Red
    Write-Host "  Duong dan da kiem tra: $BasePath hoac $BasePath\contracts" -ForegroundColor Yellow
    Write-Host "  Huong dan: Chay '.\scripts\import-contracts.ps1' de nhap khau hop dong truoc khi xac minh." -ForegroundColor Gray
    exit 1
}

Write-Host "Contracts Root: $ContractsDir" -ForegroundColor DarkGray
Write-Host "Manifest File : $ManifestPath`n" -ForegroundColor DarkGray

# 2. Doc va phan tich manifest
try {
    $ManifestContent = Get-Content -Path $ManifestPath -Raw -Encoding UTF8
    $Manifest = $ManifestContent | ConvertFrom-Json
}
catch {
    Write-Host "[FAIL] Tep 'contracts.manifest.json' bi loi cu phap JSON hoac khong the doc duoc: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Guideline Origin Commit : $($Manifest.guidelineCommit)" -ForegroundColor Cyan
Write-Host "Imported Timestamp      : $($Manifest.importedAtUtc)" -ForegroundColor Cyan
Write-Host "Origin Repository       : $($Manifest.guidelineRepo)" -ForegroundColor DarkGray
Write-Host "-----------------------------------------------------------------" -ForegroundColor DarkCyan

if ($null -eq $Manifest.contracts -or ($Manifest.contracts.PSObject.Properties | Measure-Object).Count -eq 0) {
    Write-Host "[WARN] Manifest khong chua bat ky hop dong nao duoc dang ky." -ForegroundColor Yellow
    exit 0
}

# 3. Kiem tra tinh toan ven tung hop dong va tung tep
$TotalFiles = 0
$PassedFiles = 0
$FailedFiles = 0
$MissingFiles = 0

foreach ($prop in $Manifest.contracts.PSObject.Properties) {
    $contractName = $prop.Name
    $cObj = $prop.Value
    
    Write-Host "`n[CONTRACT] $contractName (Loai: $($cObj.type) | Phien ban: $($cObj.version))" -ForegroundColor Cyan
    if (-not [string]::IsNullOrWhiteSpace($cObj.description)) {
        Write-Host "  Mo ta: $($cObj.description)" -ForegroundColor DarkGray
    }

    foreach ($fileEntry in $cObj.files) {
        $TotalFiles++
        $relPath = $fileEntry.relativePath
        $expectedHash = $fileEntry.sha256
        
        # Chuyen duong dan tuong doi phu hop voi OS
        $osRelPath = $relPath.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
        $fullFilePath = Join-Path $ContractsDir $osRelPath

        if (-not (Test-Path $fullFilePath)) {
            $MissingFiles++
            $FailedFiles++
            Write-Host "  [MISSING] $relPath" -ForegroundColor Red
            Write-Host "            Tep khong ton tai tren o dia tai: $fullFilePath" -ForegroundColor Red
            continue
        }

        # Tinh toan SHA-256
        $actualHash = (Get-FileHash -Path $fullFilePath -Algorithm SHA256).Hash

        if ($actualHash -eq $expectedHash) {
            $PassedFiles++
            Write-Host "  [PASS] $relPath" -ForegroundColor Green
            Write-Host "         SHA-256: $actualHash (Khop 100%)" -ForegroundColor DarkGray
        }
        else {
            $FailedFiles++
            Write-Host "  [MODIFIED] $relPath" -ForegroundColor Red
            Write-Host "             Noi dung tep da bi THAY DOI cuc bo so voi ban phat hanh goc!" -ForegroundColor Red
            Write-Host "             Expected SHA-256: $expectedHash" -ForegroundColor Yellow
            Write-Host "             Actual SHA-256  : $actualHash" -ForegroundColor Yellow
        }
    }
}

# 4. Tong ket ket qua
Write-Host "`n=================================================================" -ForegroundColor DarkCyan
Write-Host "                     TONG KET XAC MINH HOP DONG                 " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host ("  Tong so tep kiem tra: {0}" -f $TotalFiles)
Write-Host ("  Chinh xac  (PASS)   : {0}" -f $PassedFiles) -ForegroundColor Green
Write-Host ("  Bi sua doi (MODIFIED: {0}" -f ($FailedFiles - $MissingFiles)) -ForegroundColor $(if ($FailedFiles -gt $MissingFiles) { "Red" } else { "DarkGray" })
Write-Host ("  Bi thieu   (MISSING): {0}" -f $MissingFiles) -ForegroundColor $(if ($MissingFiles -gt 0) { "Red" } else { "DarkGray" })
Write-Host "-----------------------------------------------------------------" -ForegroundColor DarkCyan

if ($FailedFiles -gt 0) {
    Write-Host ">>> KET QUA: THAT BAI (HOP DONG KHONG DUNG BAN GOC TAI GUIDELINE) <<<" -ForegroundColor Red
    Write-Host "Chu y: Hop dong microservices la ranh gioi giao tiep bat bien giua cac nhom." -ForegroundColor Yellow
    Write-Host "Neu can thay doi hop dong, hay tao phien ban moi (v2) tai guideline repo," -ForegroundColor Yellow
    Write-Host "hoac chay '.\scripts\import-contracts.ps1 -Force' neu muon khoi phuc ve ban goc." -ForegroundColor Yellow
    exit 1
}

Write-Host ">>> KET QUA: TAT CA HOP DONG HOAN TOAN NGUYEN BAN & DAT CHUAN (PASS) <<<" -ForegroundColor Green
exit 0
