# Stage 1: build the Angular client
FROM node:24-alpine AS client-build
WORKDIR /client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

# Stage 2: build and publish the .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server-build
WORKDIR /src
COPY DrivingLessons.sln ./
COPY src/ ./src/
RUN dotnet publish src/DrivingLessons.Presentation.Web/DrivingLessons.Presentation.Web.csproj -c Release -o /app/publish

# Stage 3: runtime image serving API + SPA
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=server-build /app/publish ./
COPY --from=client-build /client/dist/client/browser ./wwwroot/
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DrivingLessons.Presentation.Web.dll"]
