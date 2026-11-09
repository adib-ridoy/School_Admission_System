FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/SchoolAdmission.Api.csproj backend/
RUN dotnet restore backend/SchoolAdmission.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/SchoolAdmission.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
