# WAD project integrity checker
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$script:Guids = @{}
$script:OrphanMetas = New-Object System.Collections.Generic.List[string]
$script:NoMeta = New-Object System.Collections.Generic.List[string]
$script:NoGuid = New-Object System.Collections.Generic.List[string]
$script:Dupes = New-Object System.Collections.Generic.List[string]
$script:MetaCount = 0

function Test-Meta {
    # 1. Every non-meta asset file must have a .meta, and every .meta must have a file
    $metaFiles = Get-ChildItem -Path "$root\Assets" -Recurse -Filter *.meta -File
    $script:MetaCount = $metaFiles.Count

    foreach ($m in $metaFiles) {
        $base = $m.FullName.Substring(0, $m.FullName.Length - 5)
        if (-not (Test-Path $base)) {
            $script:OrphanMetas += (Resolve-Path -Relative $m.FullName)
            continue
        }
        $c = Get-Content $m.FullName -Raw
        if ($c -match 'guid:\s*([0-9a-fA-F]{32})') {
            $g = $Matches[1]
            if ($script:Guids.ContainsKey($g)) { $script:Dupes += "$g -> $($script:Guids[$g])  AND  $base" }
            else { $script:Guids[$g] = $base }
        } else {
            $script:NoGuid += (Resolve-Path -Relative $m.FullName)
        }
    }
    # files without meta
    Get-ChildItem -Path "$root\Assets" -Recurse -File |
        Where-Object { $_.Extension -ne '.meta' -and -not (Test-Path ($_.FullName + '.meta')) } |
        ForEach-Object { $script:NoMeta += (Resolve-Path -Relative $_.FullName) }

    Write-Output "=== META INTEGRITY ==="
    Write-Output ("Metas total: " + $script:MetaCount)
    Write-Output ("Orphan metas (no asset): " + $script:OrphanMetas.Count)
    $script:OrphanMetas | ForEach-Object { Write-Output "  ORPHAN: $_" }
    Write-Output ("Files without .meta: " + $script:NoMeta.Count)
    $script:NoMeta | ForEach-Object { Write-Output "  NOMETA: $_" }
    Write-Output ("Metas without guid: " + $script:NoGuid.Count)
    $script:NoGuid | ForEach-Object { Write-Output "  NOGUID: $_" }
    Write-Output ("Duplicate GUIDs: " + $script:Dupes.Count)
    $script:Dupes | ForEach-Object { Write-Output "  DUPE: $_" }
}

Test-Meta
$guids = $script:Guids

# 2. Broken fileID/guid references in scenes & prefabs
$referenced = @{}
$broken = New-Object System.Collections.Generic.List[string]
$sceneFiles = Get-ChildItem -Path "$root\Assets" -Recurse -File -Include *.unity, *.prefab, *.playable, *.asset
foreach ($f in $sceneFiles) {
    $c = Get-Content $f.FullName -Raw
    $ms = [regex]::Matches($c, 'guid:\s*([0-9a-fA-F]{32})')
    foreach ($m in $ms) {
        $g = $m.Groups[1].Value
        if (-not $guids.ContainsKey($g) -and -not $referenced.ContainsKey($g)) {
            # known-good Unity built-in guids can still be missing from Assets; only flag as suspect
            $referenced[$g] = (Resolve-Path -Relative $f.FullName)
        }
    }
}
Write-Output ""
Write-Output "=== REFERENCE INTEGRITY (guids referenced but not in Assets) ==="
Write-Output ("Distinct missing guids: " + $referenced.Count)
# filter: many belong to built-in/hidden folders (e.g. builtin extra, fonts). List top offenders with counts.
$missing = $referenced.GetEnumerator() | Sort-Object -Property Value | Select-Object -First 40
foreach ($kv in $missing) { Write-Output ("  MISSING: $($kv.Key)  (referenced by $($kv.Value))") }

# 2b. Which missing guids are actually used in .unity scenes (highest risk of broken scene refs)
$sceneMissing = @{}
foreach ($f in (Get-ChildItem -Path "$root\Assets\Scenes" -Recurse -File -Include *.unity)) {
    $c = Get-Content $f.FullName -Raw
    foreach ($m in [regex]::Matches($c, 'guid:\s*([0-9a-fA-F]{32})')) {
        $g = $m.Groups[1].Value
        if (-not $guids.ContainsKey($g) -and -not $sceneMissing.ContainsKey($g)) { $sceneMissing[$g] = (Resolve-Path -Relative $f.FullName) }
    }
}
Write-Output "=== SCENE REFERENCE INTEGRITY (guids used in scenes but missing) ==="
Write-Output ("Distinct missing guids in scenes: " + $sceneMissing.Count)
$sceneMissing.GetEnumerator() | ForEach-Object { Write-Output ("  SCENE-MISSING: $($_.Key)  (in $($_.Value))") }

# 3. Editor scene list vs actual scene files
$build = Get-Content "$root\ProjectSettings\EditorBuildSettings.asset" -Raw
$listed = [regex]::Matches($build, 'path:\s*Assets\\(.*?\.unity)')
Write-Output ""
Write-Output "=== BUILD SETTINGS ==="
Write-Output ("Scenes in build settings: " + $listed.Count)
foreach ($l in $listed) {
    $p = $l.Groups[1].Value -replace '\\', '\'
    Write-Output ("  " + $p + " exists=" + (Test-Path "$root\Assets\" + $p))
}
$allScenes = Get-ChildItem -Path "$root\Assets" -Recurse -Filter *.unity -File | ForEach-Object { Resolve-Path -Relative $_.FullName }
Write-Output ("Scene files on disk: " + $allScenes.Count)
$allScenes | ForEach-Object { Write-Output "  SCENE: $_" }

# 4. Shadergraph / materials referencing missing files (light check)
$sgCount = (Get-ChildItem -Path "$root\Assets" -Recurse -Filter *.shadergraph -File).Count
$matCount = (Get-ChildItem -Path "$root\Assets" -Recurse -Filter *.mat -File).Count
Write-Output ""
Write-Output ("Shadergraphs: $sgCount, Materials: $matCount")

Write-Output ""
Write-Output "=== DONE ==="
