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
#   1. Publishes the app as it is given out (`dotnet publish App`: one file, with .NET
#      inside it, as App.csproj says), into a folder of its own under windows\build, so
#      that it never disturbs a build Visual Studio has open.
#   2. Looks at what it built: that there is one file and no more, and its version.
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

Write-Output "Publishing the app..."
$log = Join-Path $build "publish.log"
& dotnet publish (Join-Path $here "App") --output $published --nologo *> $log
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
