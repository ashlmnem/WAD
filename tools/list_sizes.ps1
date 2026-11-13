Get-ChildItem Assets\Scripts -Recurse -Filter *.cs |
    Sort-Object Length -Descending |
    Select-Object -First 40 |
    Format-Table @{N='KB';E={[math]::Round($_.Length/1KB,1)}}, @{N='Name';E={$_.Name}}, @{N='Dir';E={$_.Directory.Name}} -AutoSize |
    Out-String -Width 200
