md -Force .\nugetStaging\content
(Get-Content Args.cs).replace('Okedst', '$rootnamespace$').replace('public class Args','internal class Args') | Set-Content .\nugetStaging\content\Args.cs.pp
# Compress-Archive -Path .\nugetStaging\* -DestinationPath ./Okedst.Args.nupkg.zip
# rm -ErrorAction Ignore .\Okedst.Args.0.1.2.nupkg
# Rename-Item .\Okedst.Args.nupkg.zip -NewName .\Okedst.Args.0.1.2.nupkg
nuget.exe  pack .\nugetStaging\Okedst.Args.nuspec -Version 0.1.3