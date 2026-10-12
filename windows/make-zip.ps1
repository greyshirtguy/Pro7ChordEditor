# Makes a zip of the Windows app to give to other people:
#
#     windows\make-zip.ps1          writes windows\build\Pro7ChordEditor-windows-<version>.zip
#
# The Mac's is mac/make-dmg.sh, and this does for Windows what that does for the Mac
# (spec/10-platforms.md, "Getting it to other people": "a self-contained .exe in a
# .zip on the Releases page, as the old editor was"; milestone M10).
#
# What it does, in order, stopping at the first thing that is not as it should be:
#
#   1. Builds the app afresh, and publishes it as it is given out (`dotnet publish
#      App`: one file, with .NET inside it, as App.csproj says), into a folder of its
#      own under windows\build.
#   2. Looks at what it built: that there is one file and no more, its version, and
#      that NOTHING IN IT NAMES A FOLDER OF THE PC IT WAS BUILT ON (below).
#   3. Lays out a folder: the app, and the read-me that is kept beside this script
#      (windows\zip\Read Me First.txt).
#   4. Makes a zip of that folder, takes the zip apart again somewhere else, and holds
#      what comes out against what went in.
#
# The version is the app's own (<Version> in Directory.Build.props): change it there,
# not here.
#
# IT MAKES A FILE AND DOES NOTHING WITH IT. It commits nothing, tags nothing and puts
# nothing on GitHub. (The first editor's old\publish.py did all three, and must not be
# run.) Giving a build out is the maintainer's to do, by hand, when they mean to.
#
# WHAT THE APP SAYS OF WHERE IT WAS BUILT. So that its log can say which line an error
# came from, each of the app's libraries has its debugging symbols inside it, and those
# say where every file it was made from lay: C:\Users\<whoever built it>\... . The
# build of 0.1.0 that was given out says so (found on 2026-10-12, the Mac's having been
# found to: mac/NOTES.md, "The disk image"). The symbols are packed, so a search of the
# file for the folder's name does not show it. Since 0.2:
#
#   - the app is built here with `ContinuousIntegrationBuild`, which is .NET's own way
#     of having every such path written from the top of the source (as "/_/windows/...")
#     and not from the top of the disk. A log then says "/_/windows/AppViews/X.cs:line
#     12", which is as much as anyone needs. It is asked only here, and the app is
#     built afresh for it: a build made for trying things keeps the real paths, which
#     a debugger wants.
#   - and what was built is then LOOKED INTO: the file as it lies, and each set of
#     symbols unpacked, for the home folder of whoever is building and for the folder
#     the source is in; and, in the symbols, for the home folder's own name where it
#     stands by itself (they keep a path in its parts). If any is found, no zip is
#     made.
#
# HOW IT IS SIGNED: it is not. So Windows cannot say who made it, and the first time
# it is started from a zip that came through a browser, Windows stops it ("Windows
# protected your PC") until the person says to run it anyway. The read-me in the zip
# says how. Signing costs money each year, and is the maintainer's to decide
# (spec/10-platforms.md, "The Windows app").
#
# Needs the .NET SDK that builds the app, and nothing else.

$ErrorActionPreference = "Stop"

$here = $PSScriptRoot
if (-not $here) { throw "this is to be run as a script, so that it knows where it is" }
$readMe = Join-Path $here "zip\Read Me First.txt"
if (-not (Test-Path -LiteralPath $readMe)) { throw "there is no $readMe to put in the zip" }

$build = Join-Path $here "build"
$published = Join-Path $build "publish"
$laidOut = Join-Path $build "zip"
$takenApart = Join-Path $build "zip-taken-apart"

# What this script made the last time is cleared away: three folders of its own, each
# looked at before it is taken (it is under windows\build, and named as this names it).
foreach ($folder in @($published, $laidOut, $takenApart)) {
    if ((Test-Path -LiteralPath $folder) -and $folder.StartsWith($build + "\") -and ((Split-Path -Leaf $folder) -in @("publish", "zip", "zip-taken-apart"))) {
        Remove-Item -LiteralPath $folder -Recurse -Force -Confirm:$false
    }
}
New-Item -ItemType Directory -Force -Path $published, $laidOut, $takenApart | Out-Null

Write-Output "Building the app afresh, and publishing it..."
$log = Join-Path $build "publish.log"
# (Afresh, with every path written from the top of the source: see the top of this
# file. Whatever was built before, for trying things, has the real paths in it.)
& dotnet build (Join-Path $here "App") --configuration Release --no-incremental --nologo -p:ContinuousIntegrationBuild=true *> $log
if ($LASTEXITCODE -ne 0) {
    Get-Content -LiteralPath $log -Tail 40
    throw "the building failed: all of what it said is in $log"
}
& dotnet publish (Join-Path $here "App") --configuration Release --no-build --output $published --nologo -p:ContinuousIntegrationBuild=true *>> $log
if ($LASTEXITCODE -ne 0) {
    Get-Content -LiteralPath $log -Tail 40
    throw "the publishing failed: all of what it said is in $log"
}

# What was built: one file, and no more.
$files = @(Get-ChildItem -LiteralPath $published -File)
if ($files.Count -ne 1 -or $files[0].Name -ne "Pro7ChordEditor.exe") {
    throw ("what was published is not one file named Pro7ChordEditor.exe: " + (($files | ForEach-Object { $_.Name }) -join ", "))
}
$app = $files[0]
$version = $app.VersionInfo.ProductVersion
if (-not $version) { throw "the app does not say what version it is" }
# (.NET adds what it was built from after a plus sign. The number alone names the zip.)
$number = ($version -split "\+")[0]
Write-Output ("Built: " + $app.Name + ", version " + $version + ", " + [math]::Round($app.Length / 1MB) + " MB")

# Does it name a folder of this PC? (The top of this file says why it might, and where.)
Write-Output "Looking into it for the names of this PC's folders..."
$homeFolder = $env:USERPROFILE
if (-not $homeFolder) { throw "this PC does not say where the home folder of whoever is building is" }
$source = (Resolve-Path -LiteralPath (Join-Path $here "..")).Path
$own = Split-Path -Leaf $homeFolder
# Each folder as it may be written: as Windows writes it, with the strokes the other
# way, and as JSON writes it; and each of those in the two ways text is kept.
$latin = [Text.Encoding]::GetEncoding(28591)
$whole = @()
foreach ($folder in @($homeFolder, $source)) {
    foreach ($written in @($folder, $folder.Replace("\", "/"), $folder.Replace("\", "\\"))) {
        $whole += $latin.GetString([Text.Encoding]::UTF8.GetBytes($written))
        $whole += $latin.GetString([Text.Encoding]::Unicode.GetBytes($written))
    }
}
$whole = @($whole | Select-Object -Unique)
$bytes = [IO.File]::ReadAllBytes($app.FullName)
$asItLies = $latin.GetString($bytes)
$named = @()
foreach ($needle in $whole) {
    if ($asItLies.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) { $named += "a folder of this PC, in the file as it lies" }
}
# The symbols .NET puts inside a library: "MPDB", how long they are unpacked, and then
# the symbols themselves, packed, which begin "BSJB".
$sets = 0
$at = $asItLies.IndexOf("MPDB", [StringComparison]::Ordinal)
while ($at -ge 0) {
    $size = [BitConverter]::ToInt32($bytes, $at + 4)
    if ($size -gt 4 -and $size -lt 64MB) {
        $unpacked = New-Object byte[] $size
        $read = 0
        try {
            $packed = [IO.MemoryStream]::new($bytes, $at + 8, $bytes.Length - $at - 8, $false)
            $unpacking = [IO.Compression.DeflateStream]::new($packed, [IO.Compression.CompressionMode]::Decompress)
            while ($read -lt $size) {
                $more = $unpacking.Read($unpacked, $read, $size - $read)
                if ($more -le 0) { break }
                $read += $more
            }
        } catch {
            # (Four letters that were not the start of any symbols.)
            $read = 0
        }
        if ($read -eq $size -and $latin.GetString($unpacked, 0, 4) -eq "BSJB") {
            $sets++
            $symbols = $latin.GetString($unpacked)
            foreach ($needle in $whole) {
                if ($symbols.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) { $named += "a folder of this PC, in a library's symbols" }
            }
            # The home folder's own name, where it stands by itself and is not part of a
            # longer word (the symbols keep a path in its parts).
            $spot = $symbols.IndexOf($own, [StringComparison]::OrdinalIgnoreCase)
            while ($spot -ge 0) {
                $before = if ($spot -gt 0) { $symbols[$spot - 1] } else { [char]0 }
                $after = if ($spot + $own.Length -lt $symbols.Length) { $symbols[$spot + $own.Length] } else { [char]0 }
                if (-not ([char]::IsLetterOrDigit($before) -or [char]::IsLetterOrDigit($after))) {
                    $named += "the name of the home folder of whoever built it, in a library's symbols"
                    break
                }
                $spot = $symbols.IndexOf($own, $spot + 1, [StringComparison]::OrdinalIgnoreCase)
            }
        }
    }
    $at = $asItLies.IndexOf("MPDB", $at + 4, [StringComparison]::Ordinal)
}
# (The app's own five libraries each have a set. Fewer found, and this has looked at
# less than it says it has: .NET may have changed how it packs them.)
if ($sets -lt 5) { throw "only $sets sets of symbols were found in the app, where its own five libraries each have one: what it names could not be looked into" }
if ($named.Count -gt 0) {
    throw ("the app names what it should not, and no zip was made: " + (($named | Select-Object -Unique) -join "; "))
}
Write-Output "It names no folder of this PC: looked for as it lies, and inside its $sets sets of symbols."
$bytes = $null
$asItLies = $null

Copy-Item -LiteralPath $app.FullName -Destination $laidOut
Copy-Item -LiteralPath $readMe -Destination $laidOut

$zip = Join-Path $build ("Pro7ChordEditor-windows-" + $number + ".zip")
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force -Confirm:$false }
Compress-Archive -Path (Join-Path $laidOut "*") -DestinationPath $zip -CompressionLevel Optimal

# The zip taken apart again, and held against what went into it.
Expand-Archive -LiteralPath $zip -DestinationPath $takenApart
foreach ($name in @("Pro7ChordEditor.exe", "Read Me First.txt")) {
    $went = (Get-FileHash -LiteralPath (Join-Path $laidOut $name) -Algorithm SHA256).Hash
    $came = (Get-FileHash -LiteralPath (Join-Path $takenApart $name) -Algorithm SHA256).Hash
    if ($went -ne $came) { throw "$name did not come out of the zip as it went in" }
}
$inIt = @(Get-ChildItem -LiteralPath $takenApart)
if ($inIt.Count -ne 2) { throw ("the zip has more in it than the app and its read-me: " + (($inIt | ForEach-Object { $_.Name }) -join ", ")) }

Write-Output ("Made: " + $zip + ", " + [math]::Round((Get-Item -LiteralPath $zip).Length / 1MB) + " MB")
Write-Output ("SHA-256: " + (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash)
Write-Output "Nothing was committed, tagged or sent anywhere."
