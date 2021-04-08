#!/bin/bash

version=$1
pwd=$2

dotnet build All.sln --configuration Release

dotnet pack ".\src\NetRpc" -c Release -p:PackageVersion=$version
dotnet pack ".\src\NetRpc.Contract" -c Release -p:PackageVersion=$version
dotnet pack ".\src\NetRpc.Grpc" -c Release -p:PackageVersion=$version
dotnet pack ".\src\NetRpc.Http" -c Release -p:PackageVersion=$version
dotnet pack ".\src\NetRpc.Http.Client" -c Release -p:PackageVersion=$version
dotnet pack ".\src\NetRpc.MiniProfiler" -c Release -p:PackageVersion=$version
dotnet pack ".\src\NetRpc.Jaeger" -c Release -p:PackageVersion=$version
dotnet pack ".\src\NetRpc.OpenTracing" -c Release -p:PackageVersion=$version
dotnet pack ".\src\NetRpc.RabbitMQ" -c Release -p:PackageVersion=$version
dotnet pack ".\src\Proxy.Grpc" -c Release -p:PackageVersion=$version
dotnet pack ".\src\Proxy.RabbitMQ" -c Release -p:PackageVersion=$version

dotnet nuget push ".\src\NetRpc\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\NetRpc.Contract\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\NetRpc.Grpc\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\NetRpc.Http\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\NetRpc.Http.Client\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\NetRpc.MiniProfiler\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\NetRpc.Jaeger\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\NetRpc.OpenTracing\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\NetRpc.RabbitMQ\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\Proxy.Grpc\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd
dotnet nuget push ".\src\Proxy.RabbitMQ\bin\Release\*.nupkg" -s http://nuget2.yx.com/nuget -k $pwd