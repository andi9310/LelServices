FROM microsoft/dotnet:1.0.1-core
ENTRYPOINT ["dotnet", "LelConsumer.dll"]
ARG source=.
WORKDIR /app
COPY $source .
