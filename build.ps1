﻿param($task = "default")

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

get-module psake | remove-module

Tools\NuGet.exe install .nuget\packages.config -OutputDirectory packages
import-module -Name (Get-ChildItem "$scriptDir\packages\psake.*\tools\psake\psake.psm1" | Select-Object -First 1)

exec { invoke-psake "$scriptDir\default.ps1" $task }