# 清理之前的构建
dotnet clean

# 恢复 NuGet 包
dotnet restore

# 构建项目
dotnet build -c Release

# 打包
dotnet pack MonkeyScheduler/MonkeyScheduler.csproj -c Release

# 输出包的位置
Write-Host "NuGet 包已生成在:"
Get-ChildItem -Path "MonkeyScheduler/bin/Release" -Filter "*.nupkg" | Select-Object FullName 