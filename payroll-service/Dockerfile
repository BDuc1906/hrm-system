# Đổi sang bản "chiseled": loại bỏ toàn bộ package OS không cần thiết (perl, tar,
# libc-bin, shell...) mà một web API .NET không dùng tới -> xóa luôn hàng trăm
# CVE của các package đó thay vì phải tự vá từng cái một.
# Lưu ý: bản chiseled KHÔNG có shell (/bin/sh), nên ENTRYPOINT phải ở dạng exec
# ["dotnet", "....dll"] như bên dưới, không được dùng dạng "/bin/sh -c ...".
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 7300

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["payroll-service.csproj", "."]
RUN dotnet restore "payroll-service.csproj"
COPY . .
RUN dotnet build "payroll-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "payroll-service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "payroll_service.dll"]
