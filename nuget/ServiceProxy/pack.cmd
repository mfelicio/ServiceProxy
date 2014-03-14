xcopy ..\..\source\ServiceProxy\bin\Release\ServiceProxy.dll lib\net45\ /y

NuGet.exe pack ServiceProxy.nuspec -exclude *.cmd -OutputDirectory ..\