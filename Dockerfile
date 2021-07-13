#FROM mcr.microsoft.com/dotnet/sdk:5.0 as build <-- use this image for dotnet 5.0
FROM  mcr.microsoft.com/dotnet/sdk:3.1 as build
WORKDIR /app
# copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# copy and build everything else
COPY *.sln ./
COPY src/*.cs ./


RUN dotnet publish -c Release -o out

# use runtime image
#FROM mcr.microsoft.com/dotnet/runtime:5.0 # <-- use this image for dotnet 5.0
FROM mcr.microsoft.com/dotnet/runtime:3.1
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "k8s-complete.dll"]
