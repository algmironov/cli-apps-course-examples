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

Тесты используют проектную ссылку и запускают уже собранную `mycli.dll` из каталога результатов тестов через `dotnet`. Сборка каждого теста не выполняется. `CliProcessRunner` задаёт аргументы через `ArgumentList` и параллельно читает оба потока. Основной тайм-аут охватывает запись stdin, ожидание выхода и завершение чтения stdout/stderr; сам синхронный вызов запуска процесса не ограничен таймером. При тайм-ауте или внешней отмене runner пытается завершить дерево процесса и ждёт завершения процесса и чтения до пяти дополнительных секунд. Тайм-аут возвращает `TimedOut = true`, внешняя отмена вызывает `OperationCanceledException`; внешняя отмена имеет приоритет, если произошла одновременно с тайм-аутом. Другие исключения также вызывают попытку очистки. При ошибке завершения или истечении времени очистки выбрасывается `AggregateException` с исходной ошибкой и ошибкой очистки.

`Kill(entireProcessTree: true)` — попытка, а не гарантия завершения произвольного дерева на любой ОС. Если основной процесс уже вышел, его потомки могут продолжать удерживать унаследованные дескрипторы; runner ограничивает ожидание потоков, но не может по вышедшему процессу гарантированно найти и завершить потомков. В таком случае истечение времени очистки сообщается как ошибка, а не успешный тайм-аут. Этот сценарий отдельно не воспроизводится тестами. Принудительное завершение в тестах не проверяет обработку Ctrl+C.

Публикация для Windows x64 (нужен установленный .NET 10 runtime):

```powershell
dotnet publish examples/31-process-testing/MyCli/MyCli.csproj -c Release -r win-x64 --self-contained false
& ./examples/31-process-testing/MyCli/bin/Release/net10.0/win-x64/publish/mycli.exe echo --text "hello world"
```

Native AOT не требуется и в обычные тесты не входит.
