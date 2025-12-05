# combine-cs.ps1
$outputFile = "combined_cs.txt"

# Clear output file if it exists
if (Test-Path $outputFile) { Clear-Content $outputFile }

# Get all .cs files and process them
Get-ChildItem -Filter *.cs | ForEach-Object {
    # Write filename
    $_.Name | Out-File -Append $outputFile
    
    # Write blank line
    "" | Out-File -Append $outputFile
    
    # Write file content
    Get-Content $_.FullName | Out-File -Append $outputFile
    
    # Write blank line separator
    "" | Out-File -Append $outputFile
}

Write-Host "Combined CS files into $outputFile"
