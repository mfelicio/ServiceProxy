xcopy ..\..\source\ServiceProxy.Zmq\bin\Release\ServiceProxy.Zmq.dll lib\net45\ /y

NuGet.exe pack ServiceProxy.Zmq.nuspec -exclude *.cmd -OutputDirectory ..\