FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:9b0a4330cb3dac23ebd6df76ab4211ec5903907ad2c1ccde16a010bf25f8dfde AS build-env
WORKDIR /Noppelbot
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:9.0.4@sha256:17de18d43822c0379f9779ecaf5e8886b73b413b5009251a277c08ff7be47027
WORKDIR /Noppelbot
COPY --from=build-env /Noppelbot/out .
ENTRYPOINT ["./chatbot"]