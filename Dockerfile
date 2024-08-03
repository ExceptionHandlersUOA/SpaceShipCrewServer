FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

COPY ./ ./
RUN dotnet publish -c Release -o ./bin

FROM mcr.microsoft.com/dotnet/nightly/aspnet:8.0
WORKDIR /bin
COPY --from=build-env /bin/ .

ENV TZ=Pacific/Auckland
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

ENTRYPOINT ["dotnet", "Init.dll"]
EXPOSE 80