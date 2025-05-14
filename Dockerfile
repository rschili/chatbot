FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:90872f8e7f1fd2b93989b81fb7f152c3bef4fe817470a3227abaa18c873dba60 AS build-env
WORKDIR /Noppelbot
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:9.0.5@sha256:14529413f5bcd9f11d3fb230abe5bcc6118ca27cc3a3d3ee042dc36081a9569f
WORKDIR /Noppelbot
COPY --from=build-env /Noppelbot/out .
ENTRYPOINT ["./chatbot"]