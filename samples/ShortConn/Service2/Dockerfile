FROM registry.yx.com/dotnet/sdk:6.0 AS build
WORKDIR /app
COPY ./*/*.csproj  ./*/*/*.csproj ./NuGet.Config  ./*.sln ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore -r linux-x64
COPY ./ ./
RUN dotnet publish --no-restore -c Release -r linux-x64 --no-self-contained -o out Service2/Service2.csproj

FROM registry.yx.com/dotnet/aspnet-ttf:6.0
WORKDIR /app
COPY --from=build /app/out  .
EXPOSE 8006
EXPOSE 8005
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
ENTRYPOINT ["dotnet", "Service2.dll"]