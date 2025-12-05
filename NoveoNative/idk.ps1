# combine-xaml.ps1
$outputFile = "combined_xaml.txt"

# Clear output file if it exists
if (Test-Path $outputFile) { Clear-Content $outputFile }

# Get all .xaml files and process them
Get-ChildItem -Filter *.xaml | ForEach-Object {
    # Write filename
    $_.Name | Out-File -Append $outputFile
    
    # Write blank line
    "" | Out-File -Append $outputFile
    
    # Write file content
    Get-Content $_.FullName | Out-File -Append $outputFile
    
    # Write blank line separator
    "" | Out-File -Append $outputFile
}

Write-Host "Combined XAML files into $outputFile"
