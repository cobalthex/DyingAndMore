# list dependencies of a path

Param(
    [string]$file
)

$rfile = (Resolve-Path -Relative $file -ErrorAction Ignore) -replace '\\','/'

Get-ChildItem -Recurse -File | Where-Object {
    (Select-String -Quiet -Path $_ -SimpleMatch $file)
    #-or (Select-String -Quiet -Path $_ -SimpleMatch $rfile)
} | Select -Unique Name