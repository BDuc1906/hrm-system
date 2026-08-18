# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
# Đổi sang bản "chiseled-extra": vẫn tối giản, loại bỏ các package OS không cần
# thiết như bản "chiseled" thường, NHƯNG có kèm theo ICU / globalization data /
# timezone data. Bản "chiseled" (không -extra) KHÔNG có ICU, nên khi kết hợp với
# InvariantGlobalization=false trong .csproj (bắt buộc để Microsoft.Data.SqlClient
# hoạt động), app sẽ crash với lỗi:
#   System.NotSupportedException: Globalization Invariant Mode is not supported.
# vì runtime cần ICU nhưng image không có sẵn ICU để dùng.
# Lưu ý: bản chiseled KHÔNG có shell (/bin/sh), nên ENTRYPOINT phải ở dạng exec
# ["dotnet", "....dll"] như bên dưới, không được dùng dạng "/bin/sh -c ...".
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HRCoreService/HRCoreService.csproj", "HRCoreService/"]
RUN dotnet restore "./HRCoreService/HRCoreService.csproj"
COPY . .
WORKDIR "/src/HRCoreService"
RUN dotnet build "./HRCoreService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HRCoreService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HRCoreService.dll"]
