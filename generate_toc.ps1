# PowerShell script to generate TOC from namespace YAML files
$namespaceFiles = Get-ChildItem docfx_project/api/*.yml | Where-Object { $_.BaseName -notmatch '\.' -and $_.Name -ne 'toc.yml' } | Sort-Object Name

$tocContent = "items:`n"

foreach ($file in $namespaceFiles) {
    $content = Get-Content $file.FullName
    $uidLine = $content | Where-Object { $_ -match '^- uid: ' }
    if ($uidLine) {
        $uid = ($uidLine -split ': ', 2)[1]
        $tocContent += "  - uid: $uid`n"
    }
}

$tocContent | Out-File docfx_project/api/toc.yml -Encoding UTF8

"# API Reference`n`nBrowse namespaces and types." | Out-File docfx_project/api/index.md -Encoding UTF8

Write-Host "TOC generated:"
Get-Content docfx_project/api/toc.yml