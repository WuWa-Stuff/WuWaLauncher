[![App build](https://github.com/WuWa-Stuff/WuWaLauncher/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/WuWa-Stuff/WuWaLauncher/actions/workflows/build.yml)

# Установка

[Скачивать тут](https://github.com/WuWa-Stuff/WuWaLauncher/releases/latest)

- **Людям, которые с компьютером на "вы", рекомендуется качать `WuWaTranslated.exe`**  
  - Да, весит много, но зато содержит всё что нужно для запуска и работы.
  - Для параноиков лежит `WuWaTranslated.zip` рядом.  
    #### ⚠️ ВАЖНО: РАСПАКОВВАТЬ В ПУСТУЮ ПАПКУ И УСТАНОВИТЬ [.NET Desktop Runtime 8.x.x](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) ([тык](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.6-windows-x64-installer))!
- Запустить `WuWaTranslated.exe` и следовать инструкции

## Самостоятельная сборка
### Софт для сборки
- PowerShell
- [.NET8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Сборка
- `git clone --recurse-submodules https://github.com/WuWa-Stuff/WuWaLauncher.git`
- `cd WuWaLauncher`
- `dotnet build "./WuWaTranslated/WuWaTranslated.csproj" -c "Release"`  
  - сборка+запуск:  
    `dotnet run "./WuWaTranslated/WuWaTranslated.csproj" -c "Release"`