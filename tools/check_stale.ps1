$dll = (Get-Item Library\ScriptAssemblies\Assembly-CSharp.dll).LastWriteTime
$stale = Get-ChildItem Assets\Scripts -Recurse -Filter *.cs | Where-Object { $_.LastWriteTime -gt $dll }
Write-Output ('DLL compiled: ' + $dll)
Write-Output ('Stale .cs (newer than DLL): ' + ($stale | Measure-Object).Count)
$stale | ForEach-Object { Write-Output ('  ' + $_.LastWriteTime.ToString('yyyy-MM-dd HH:mm') + '  ' + $_.FullName) }
