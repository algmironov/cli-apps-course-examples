# №31: тестирование настоящего CLI

Ссылка на статью будет добавлена после публикации. Требуется .NET 10 SDK.

```powershell
dotnet build CLIAppsCourse.slnx
dotnet test CLIAppsCourse.slnx
dotnet run --project examples/31-process-testing/MyCli -- scan --path .
dotnet run --project examples/31-process-testing/MyCli -- echo --text "hello world"
dotnet run --project examples/31-process-testing/MyCli -- fail
dotnet run --project examples/31-process-testing/MyCli -- sleep --milliseconds 100
"hello input" | dotnet run --project examples/31-process-testing/MyCli -- read
dotnet run --project examples/31-process-testing/MyCli -- streams --lines 10
```

`scan` рекурсивно считает строки, содержащие хотя бы один непробельный символ, в файлах `*.cs`; относительный путь разрешается от рабочего каталога процесса. Команда ничего не изменяет. `read` читает одну строку stdin. `streams` выводит заданное число строк в stdout и диагностических строк в stderr (0–100000); это позволяет проверить одновременное чтение обоих потоков.

Коды: 0 — успех; 1 — ошибка разбора аргументов System.CommandLine; 2 — предсказуемая ошибка команды или файлового доступа; 3 — отмена sleep. Операционная система может возвращать другой код при принудительном завершении процесса. Вывод результата идёт в stdout, ошибки — в stderr; при ошибке разбора справка также может попасть в stdout.

Тесты используют проектную ссылку и запускают уже собранную `mycli.dll` из каталога результатов тестов через `dotnet`. Сборка каждого теста не выполняется. `CliProcessRunner` задаёт аргументы через `ArgumentList`, параллельно читает оба потока, ограничивает время и на тайм-ауте завершает дерево процессов и ждёт завершения чтения.

Публикация для Windows x64 (нужен установленный .NET 10 runtime):

```powershell
dotnet publish examples/31-process-testing/MyCli/MyCli.csproj -c Release -r win-x64 --self-contained false
& ./examples/31-process-testing/MyCli/bin/Release/net10.0/win-x64/publish/mycli.exe echo --text "hello world"
```

Native AOT не требуется и в обычные тесты не входит.
