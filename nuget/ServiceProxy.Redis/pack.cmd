xcopy ..\..\source\ServiceProxy.Redis\bin\Release\ServiceProxy.Redis.dll lib\net45\ /y

NuGet.exe pack ServiceProxy.Redis.nuspec -exclude *.cmd -OutputDirectory ..\