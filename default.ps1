Include ".\build_utils.ps1"

properties {
 $base_dir  = resolve-path .
 $build_dir = "$base_dir\build"
 $packages_dir = "$base_dir\packages"
 $buildartifacts_dir = "$build_dir\"
 $sln_file = "$base_dir\SharpTAL.sln"
 $version = "3.0.1"
 $tools_dir = "$base_dir\Tools"
 $global:configuration = "Release"
 
 $sharptal_files = ( @( "SharpTAL.???")) |
     ForEach-Object { 
         if ([System.IO.Path]::IsPathRooted($_)) { return $_ }
         return "$build_dir\$_"
     }
    
    $test_prjs = @("SharpTAL.Tests.dll")
}

task default -depends Test, DoRelease

task Verify40 {
 if( (ls "$env:windir\Microsoft.NET\Framework\v4.0*") -eq $null ) {
     throw "Building SharpTAL requires .NET 4.0, which doesn't appear to be installed on this machine"
 }
}

task Clean {
 Remove-Item -force -recurse $buildartifacts_dir -ErrorAction SilentlyContinue
}

task Init -depends Verify40, Clean {
 New-Item $build_dir -itemType directory -ErrorAction SilentlyContinue | Out-Null
}

task Compile -depends Init {
 
 $v4_net_version = (ls "$env:windir\Microsoft.NET\Framework\v4.0*").Name
 
 Write-Host "Compiling with '$global:configuration' configuration" -ForegroundColor Yellow
 exec { &"C:\Windows\Microsoft.NET\Framework\$v4_net_version\MSBuild.exe" "$sln_file" /p:Configuration=$global:configuration /p:nowarn="1591 1573" }
}

task Test -depends Compile {
 Write-Host $test_prjs
 
 $nUnit = Get-PackagePath NUnit.ConsoleRunner
 $nUnit = "$nUnit\tools\nunit3-console.exe"
 
 $test_prjs | ForEach-Object { 
        Write-Host "Testing $build_dir\$_ (default)"
	Set-Location -Path $build_dir
        exec { &"$nUnit" "$build_dir\$_" }
 }
}

task CreateOutpuDirectories -depends CleanOutputDirectory {
 New-Item $build_dir\Output -Type directory -ErrorAction SilentlyContinue | Out-Null
}

task CleanOutputDirectory { 
 Remove-Item $build_dir\Output -Recurse -Force -ErrorAction SilentlyContinue
}

task CopySharpTALFiles -depends CreateOutpuDirectories {
 $sharptal_files | ForEach-Object { Copy-Item "$_" $build_dir\Output }
}

task ZipOutput {
 
 if($env:buildlabel -eq 13)
 {
     return 
 }

 $old = pwd
 cd $build_dir\Output
 
 $file = "SharpTAL-$version-bin.zip"
    
 exec { 
     & $tools_dir\zip.exe -9 -A -r `
         $file `
         *.*
 }
 
 Copy-Item $file $base_dir\$file
 
    cd $old
}

task Merge {
 $old = pwd
 cd $build_dir
 
 Remove-Item SharpTAL.Partial.dll -ErrorAction SilentlyContinue 
 Rename-Item $build_dir\SharpTAL.dll SharpTAL.Partial.dll
 
 & $tools_dir\ILMerge.exe SharpTAL.Partial.dll `
     ICSharpCode.NRefactory.dll `
     ICSharpCode.NRefactory.CSharp.dll `
     ICSharpCode.NRefactory.Xml.dll `
     /out:SharpTAL.dll `
     /t:library `
     "/internalize"
 if ($lastExitCode -ne 0) {
        throw "Error: Failed to merge assemblies!"
    }
 cd $old
}

task DoRelease -depends Compile, Merge, `
 CleanOutputDirectory, `
 CreateOutpuDirectories, `
 CopySharpTALFiles, `
 ZipOutput, `
    CreateNugetPackages {
 
 Write-Host "Done building SharpTAL"
}

task UploadNuget -depends InitNuget, PushNugetPackages

task InitNuget {
 $global:nugetVersion = "$version"
}

task PushNugetPackages {
 # Upload packages
 $accessPath = "$base_dir\..\Nuget-Access-Key.txt"
 $sourceFeed = "https://nuget.org/"
 
 if ( (Test-Path $accessPath) ) {
     $accessKey = Get-Content $accessPath
     $accessKey = $accessKey.Trim()
     
     $nuget_dir = "$build_dir\NuGet"

     # Push to nuget repository
     $packages = Get-ChildItem $nuget_dir *.nuspec -recurse

     $packages | ForEach-Object {
         $tries = 0
         while ($tries -lt 10) {
             try {
                 &"$base_dir\Tools\NuGet.exe" push "$($_.BaseName).$global:nugetVersion.nupkg" $accessKey -Source $sourceFeed -Timeout 4800
                 $tries = 100
             } catch {
                 $tries++
             }
         }
     }
     
 }
 else {
     Write-Host "$accessPath does not exit. Cannot publish the nuget package." -ForegroundColor Yellow
 }
}

task CreateNugetPackages -depends Compile, InitNuget {

 Remove-Item $base_dir\SharpTAL*.nupkg
 
 $nuget_dir = "$build_dir\NuGet"
 Remove-Item $nuget_dir -Force -Recurse -ErrorAction SilentlyContinue
 New-Item $nuget_dir -Type directory | Out-Null
 
 New-Item $nuget_dir\SharpTAL\lib\net40 -Type directory | Out-Null
 Copy-Item $base_dir\NuGet\SharpTAL.nuspec $nuget_dir\SharpTAL\SharpTAL.nuspec
 @("SharpTAL.???") |% { Copy-Item "$build_dir\$_" $nuget_dir\SharpTAL\lib\net40 }
 
    # Sets the package version in all the nuspec as well as any SharpTAL package dependency versions
 $packages = Get-ChildItem $nuget_dir *.nuspec -recurse
 $packages |% { 
     $nuspec = [xml](Get-Content $_.FullName)
     $nuspec.package.metadata.version = $global:nugetVersion
     $nuspec | Select-Xml '//dependency' |% {
         if($_.Node.Id.StartsWith('SharpTAL')){
             $_.Node.Version = "[$global:nugetVersion]"
         }
     }
     $nuspec.Save($_.FullName);
     Exec { &"$base_dir\Tools\nuget.exe" pack $_.FullName }
 }
}

TaskTearDown {
 
 if ($LastExitCode -ne 0) {
     write-host "TaskTearDown detected an error. Build failed." -BackgroundColor Red -ForegroundColor Yellow
     write-host "Yes, something was failed!!!!!!!!!!!!!!!!!!!!!" -BackgroundColor Red -ForegroundColor Yellow
     # throw "TaskTearDown detected an error. Build failed."
     exit 1
 }
}
