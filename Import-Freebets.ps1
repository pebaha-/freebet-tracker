param(
    [string]$CsvPath = ".\freebets.csv",
    [string]$AccountName = "freebettrackersa",
    [string]$TableName = "Freebets"
)

$ErrorActionPreference = "Stop"
$PartitionKey = "freebets"

# Validate input and Azure CLI login.
if (-not (Test-Path -LiteralPath $CsvPath)) {
    throw "CSV file not found: $CsvPath"
}

$account = az account show --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw "Azure CLI is not logged in. Run 'az login' first."
}

$token = az account get-access-token --resource "https://storage.azure.com/" --query accessToken --output tsv

if ($LASTEXITCODE -ne 0 -or -not $token) {
    throw "Could not obtain an Azure Storage access token."
}

$headers = @{
    Authorization  = "Bearer $token"
    "x-ms-date"    = [DateTime]::UtcNow.ToString("R")
    "x-ms-version" = "2019-02-02"
    Accept         = "application/json;odata=nometadata"
}

$baseUri = "https://$AccountName.table.core.windows.net/$TableName"

# Import CSV. Supports comma- and semicolon-separated files.
$firstLine = Get-Content -LiteralPath $CsvPath -TotalCount 1
$delimiter = if (
    ($firstLine.ToCharArray() | Where-Object { $_ -eq ';' }).Count -gt
    ($firstLine.ToCharArray() | Where-Object { $_ -eq ',' }).Count
) { ';' } else { ',' }

$rows = @(Import-Csv -LiteralPath $CsvPath -Delimiter $delimiter)

if ($rows.Count -eq 0) {
    throw "The CSV contains no data rows."
}

$required = @(
    "ID", "Konto", "Spiller", "Værdi", "Udløbsdato", "Vilkår", "Brugt"
)

$missing = @(
    $required | Where-Object {
        $_ -notin $rows[0].PSObject.Properties.Name
    }
)

if ($missing.Count -gt 0) {
    throw "Missing CSV columns: $($missing -join ', ')"
}

# Validate and convert all rows before writing anything.
$freebets = @(
    foreach ($row in $rows) {
        $id = "$($row.ID)".Trim()
        $amountText = "$($row.Værdi)".Trim() -replace '[^\d,.\-]', ''

        if ([string]::IsNullOrWhiteSpace($id)) {
            throw "A row has no ID."
        }

        if ([string]::IsNullOrWhiteSpace($row.Konto) -or
            [string]::IsNullOrWhiteSpace($row.Spiller)) {
            throw "ID '$id': Konto and Spiller are required."
        }

        $amount = 0.0
        $parsed = [double]::TryParse(
            $amountText,
            [Globalization.NumberStyles]::Number,
            [Globalization.CultureInfo]::GetCultureInfo("da-DK"),
            [ref]$amount
        )

        if (-not $parsed) {
            $parsed = [double]::TryParse(
                $amountText,
                [Globalization.NumberStyles]::Number,
                [Globalization.CultureInfo]::InvariantCulture,
                [ref]$amount
            )
        }

        if (-not $parsed -or $amount -lt 0) {
            throw "ID '$id': Invalid amount '$($row.Værdi)'."
        }

        $culture = [System.Globalization.CultureInfo]::InvariantCulture
        $dateStyles = [System.Globalization.DateTimeStyles]::None

        $expiryDate = [datetime]::MinValue
        $date = $row.Udløbsdato.Trim()

        if ($date -match '^(\d{1,2})/(\d{1,2})/(\d{4})$') {
            $day = [int]$Matches[1]
            $month = [int]$Matches[2]
            $year = [int]$Matches[3]

            try {
                $expiryDate = [datetime]::new($year, $month, $day)
                $expiryDate = $expiryDate.Date.AddDays(1).AddTicks(-1)
            }
            catch {
                throw "ID '$id': Invalid expiry date '$date'."
            }
        }
        elseif ($date -match '^(\d{4})-(\d{2})-(\d{2})$') {
            $year = [int]$Matches[1]
            $month = [int]$Matches[2]
            $day = [int]$Matches[3]

            try {
                $expiryDate = [datetime]::new($year, $month, $day)
            }
            catch {
                throw "ID '$id': Invalid expiry date '$date'."
            }
        }
        else {
            throw "ID '$id': Invalid expiry date '$date'. Expected dd/MM/yyyy or yyyy-MM-dd."
        }

        $used = $false
        switch -Regex ("$($row.Brugt)".Trim()) {
            '^(TRUE|JA|1)$'  { $used = $true; break }
            '^(FALSE|NEJ|0)$' { $used = $false; break }
            default { throw "ID '$id': Invalid Brugt value '$($row.Brugt)'." }
        }

        [pscustomobject]@{
            Id                   = $id
            Account              = "$($row.Konto)".Trim()
            Player               = "$($row.Spiller)".Trim()
            Amount               = $amount
            ExpiryDate           = $expiryDate
            ExpiryTimeSpecified  = $false
            Terms                = "$($row.Vilkår)".Trim()
            IsUsed               = $used
        }
    }
)

$duplicates = @(
    $freebets | Group-Object Id | Where-Object Count -gt 1
)

if ($duplicates.Count -gt 0) {
    throw "Duplicate IDs in CSV: $(($duplicates.Name) -join ', ')"
}

# Check every ID in Azure before importing.
foreach ($freebet in $freebets) {
    $key = [uri]::EscapeDataString($freebet.Id)
    $uri = "$baseUri(PartitionKey='$PartitionKey',RowKey='$key')"

    try {
        Invoke-RestMethod -Method Get -Uri $uri -Headers $headers | Out-Null
        throw "ID '$($freebet.Id)' already exists in Azure. Nothing imported."
    }
    catch {
        $status = [int]$_.Exception.Response.StatusCode

        if ($status -eq 404) {
            continue
        }

        throw
    }
}

# Preview.
Write-Host "`nAccount: $($account.user.name)"
Write-Host "CSV: $((Resolve-Path $CsvPath).Path)"
Write-Host "Records: $($freebets.Count)"
Write-Host ("Total value: {0:N2} DKK" -f (($freebets | Measure-Object Amount -Sum).Sum))

$freebets | Format-Table Id, Account, Player, Amount, ExpiryDate, IsUsed

$confirmation = Read-Host "Type IMPORT to insert these records into Azure"
if ($confirmation -cne "IMPORT") {
    Write-Host "Import cancelled."
    return
}

# Insert entities using the Azure Table REST API.
$imported = 0

foreach ($f in $freebets) {
    $entity = @{
        PartitionKey            = $PartitionKey
        RowKey                  = $f.Id
        Player                  = $f.Player
        Account                 = $f.Account
        Amount = $f.Amount.ToString([System.Globalization.CultureInfo]::InvariantCulture)
        ExpiryDate = $f.ExpiryDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", [System.Globalization.CultureInfo]::InvariantCulture)
        ExpiryTimeSpecified     = $f.ExpiryTimeSpecified
        IsUsed                  = $f.IsUsed
    }

    if (-not [string]::IsNullOrWhiteSpace($f.Terms)) {
        $entity.Terms = $f.Terms
    }

    $body = $entity | ConvertTo-Json -Depth 5 -Compress

    Invoke-RestMethod `
        -Method Post `
        -Uri $baseUri `
        -Headers $headers `
        -ContentType "application/json" `
        -Body $body | Out-Null

    $imported++
    Write-Host "Imported ID $($f.Id)"
}

Write-Host "`nImport completed: $imported records."
