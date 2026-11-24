# CLI приложения: примеры курса

Репозиторий содержит сопровождающие примеры курса. Каждый выпуск находится в собственном каталоге.

- [№31 — интеграционное тестирование процесса](examples/31-process-testing/README.md)

Требуется .NET 10 SDK. Из корня репозитория:

```powershell
dotnet build CLIAppsCourse.slnx
dotnet test CLIAppsCourse.slnx
dotnet run --project examples/31-process-testing/MyCli -- --help
```

Дальнейшие примеры добавляются в `examples/` и в решение. Лицензия — [MIT](LICENSE).
