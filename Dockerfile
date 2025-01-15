FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:84fd557bebc64015e731aca1085b92c7619e49bdbe247e57392a43d92276f617 AS build-env
WORKDIR /Noppelbot
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:9.0.1@sha256:7f9384d9d436ab62a5ed9d3188af2b6bf56655ce646503a9f73ff81795391d4c
WORKDIR /Noppelbot
COPY --from=build-env /Noppelbot/out .
ENTRYPOINT ["./chatbot"]