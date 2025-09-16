$warnings = Get-Content "cs1591_current_warnings.txt"

foreach ($line in $warnings) {
    if ($line -match 'c:\\Users\\Peter\\Documents\\GitHub\\kalshi-trading-bot\\(.*)\((\d+),\d+\):.*''(.*)''') {
        $file = $matches[1]
        $lineNum = [int]$matches[2]
        $member = $matches[3]

        try {
            # Read file
            $content = Get-Content $file
            if ($content -eq $null) { continue }

            # Insert before lineNum (1-based), so at index $lineNum -1
            $insertIndex = $lineNum - 1

            # Check if the line above already has ///
            if ($insertIndex -gt 0 -and $content[$insertIndex - 1] -match '^\s*///') {
                continue
            }

            # Determine comment text
            $summaryText = ""
            if ($member -match '\.(\w+)\(\)') {
                # Method
                $methodName = $matches[1]
                $summaryText = "/// <summary>$methodName</summary>"
            } elseif ($member -match '\.(\w+)\(') {
                # Method with params
                $methodName = $matches[1]
                $summaryText = "/// <summary>$methodName</summary>"
            } elseif ($member -match '\.(\w+)$') {
                # Property
                $propName = $matches[1]
                $summaryText = "/// <summary>Gets or sets the $propName.</summary>"
            } else {
                # Class or other
                $summaryText = "/// <summary>$member</summary>"
            }

            # Comment lines
            $commentLines = @(
                $summaryText
            )

            # Insert
            $content = $content[0..($insertIndex - 1)] + $commentLines + $content[$insertIndex..($content.Length - 1)]

            # Write back
            $content | Set-Content $file -Encoding UTF8
        } catch {
            Write-Host "Failed to process $file : $_"
        }
    }
}