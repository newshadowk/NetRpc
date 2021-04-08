#!/bin/bash

pwd=$1
version=$2

dotnet build all.sln --configuration Release
dotnet nuget push  ".\src\NetRpc\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\NetRpc.Contract\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\NetRpc.Grpc\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\NetRpc.Http\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\NetRpc.Http.Client\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\NetRpc.MiniProfiler\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\NetRpc.Jaeger\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\NetRpc.OpenTracing\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\NetRpc.RabbitMQ\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\Proxy.Grpc\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push  ".\src\Proxy.RabbitMQ\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd