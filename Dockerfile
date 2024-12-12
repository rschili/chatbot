FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:fe8ceeca5ee197deba95419e3b85c32744970b730ae11645e13f1cb74a848d98 AS build-env
WORKDIR /Noppelbot
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:9.0.0@sha256:b7afeeb945d9cfaf215e8262c5e17ffcba4e4397b7a6fb37610d4c3a30b85906
WORKDIR /Noppelbot
COPY --from=build-env /Noppelbot/out .
ENTRYPOINT ["./chatbot"]