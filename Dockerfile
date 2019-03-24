FROM microsoft/dotnet:2.2-sdk as build
WORKDIR /build
COPY . /build

RUN dotnet restore /build/VisioStencilCreator.App/VisioStencilCreator.App.csproj
RUN dotnet publish /build/VisioStencilCreator.App/VisioStencilCreator.App.csproj -o /app

FROM microsoft/dotnet:2.2-runtime as final
COPY --from=build /app /app

# From https://github.com/JanKallman/EPPlus/issues/83
RUN ln -s /lib/x86_64-linux-gnu/libdl.so.2 /lib/x86_64-linux-gnu/libdl.so \
    && apt-get update \
    && apt-get install -y libgdiplus \
    && ln -s /usr/lib/libgdiplus.so /lib/x86_64-linux-gnu/libgdiplus.so 

ENTRYPOINT [ "dotnet", "/app/VisioStencilCreator.App.dll" ]
