# SecureConsoleFileManager

Консольная утилита на .NET 9 для безопасной работы с файлами и каталогами в «песочнице» пользователя: навигация и просмотр содержимого, создание/чтение/дозапись/удаление файлов, работа с директориями, перемещение, а также создание и распаковка ZIP‑архивов. Есть пользователи (регистрация/вход), журналирование через Serilog и конфигурация через `appsettings.json`.

## Ключевые возможности
- Навигация и просмотр: `ls`, `cd`, `pwd`, `clear`
- Пользователи: `register`, `login`
- Информация о дисках: `disk`
- Файлы: `touch`, `rm`, `wr` (дозапись), `cat`
- Директории: `mkdir`, `rmdir` (с `-r`/`--recursive`)
- Перемещение: `mv` (файлы и каталоги)
- Архивы: `zip`, `unzip`

Реальные команды и их синтаксис см. ниже (из `Common/CommandBuilder.cs`). Все операции выполняются в пределах стартовой директории песочницы.

## Требования
- .NET SDK 9.0+
- ОС: Linux/Windows/macOS
- (Опционально) Docker для запуска PostgreSQL/Elasticsearch из compose

## Сборка и запуск (локально)
```bash
cd SecureConsoleFileManager
# Сборка
dotnet build
# Запуск интерактивной оболочки (REPL)
dotnet run --project SecureConsoleFileManager
```
После запуска откроется интерактивная оболочка. Команды вводятся прямо в консоли (без префикса `dotnet`). Примеры ниже.

## Конфигурация
Файл: `SecureConsoleFileManager/appsettings.json`.
- FileSystem (песочница):
  - Раздел `FileSystemConfig` → ключ `StartPath`. Итоговый корень песочницы вычисляется как `FullStartPath = Path.Combine(RootPath, StartPath)`, где `RootPath` — домашняя директория текущего пользователя ОС.
  - Пример: если пользователь — `alice`, `RootPath = /home/alice` (Linux), при `StartPath = "file_manager_directory"` корень песочницы будет `/home/alice/file_manager_directory`.
- Логирование (Serilog):
  - Пишет в файл `logs/file_manager-.log` (ротация по дням) и в консоль. Опционально — в Elasticsearch (`http://localhost:9200`).
- БД (EF Core / PostgreSQL):
  - `ConnectionStrings:DefaultConnection` — строка подключения для фич `register`/`login`. Требуется доступный PostgreSQL и применённые миграции (кратко; см. папку `Migrations`).

Для переопределения настроек создайте `appsettings.Development.json` и запускайте с `DOTNET_ENVIRONMENT=Development`.

## Использование: команды REPL
Все пути интерпретируются относительно текущей директории песочницы. Переход вверх — `cd ..`.

- Навигация и просмотр
  - `pwd` — показать текущий относительный путь (корень песочницы — `/`).
  - `ls` — список директорий и файлов.
  - `cd <path>` — перейти в поддиректорию или `..` для подъёма.
  - `clear` — очистить экран.

- Пользователи
  - `register` — интерактивная регистрация (логин/пароль запрашиваются в консоли).
  - `login` — интерактивный вход (логин/пароль запрашиваются в консоли).

- Диски
  - `disk` — вывести информацию о доступных дисках.

- Файлы
  - `touch <name>` — создать файл.
  - `wr <name> <info>` — дозаписать строку `<info>` в файл `<name>`.
  - `cat <name>` — вывести содержимое файла.
  - `rm <name>` — удалить файл.

- Директории
  - `mkdir <directoryName>` — создать директорию.
  - `rmdir <directoryName> [-r|--recursive]` — удалить директорию; для непустых требуется флаг `-r`.

- Перемещение
  - `mv <source> <destination>` — переместить файл или директорию. Если родительская директория у `<destination>` отсутствует — будет создана. Перемещение отменяется, если целевой путь уже занят.

- Архивы
  - `zip <archive-name> <sources...>` — создать ZIP‑архив из одного или нескольких источников (файлов/директорий). Пример: `zip backup.zip docs notes.txt`.
  - `unzip <archive-path> <destination-directory>` — распаковать архив. Пример: `unzip backup.zip restore_dir`.

### Примеры с путями (при StartPath = "file_manager_directory")
Предположим, ваша домашняя директория — `/home/alice`, значит корень песочницы: `/home/alice/file_manager_directory`.
```text
pwd
/         # вы в корне песочницы
mkdir docs
cd docs
pwd
/docs
wr notes.txt "Hello"
ls
	notes.txt                        5 bytes
cd ..
zip backup.zip docs notes.txt
unzip backup.zip restored
mv docs docs_old
rmdir restored -r
rm notes.txt
```
Все указанные относительные пути будут преобразованы в абсолютные внутри песочницы. Попытки выхода наружу (например, `../../etc`) будут отклонены.

## Безопасность и ограничения (по коду)
- Все операции с путями проходят валидацию (`FileManagerService.ValidateAndGetFullPath`): защита от недопустимых символов и выхода за пределы корня песочницы (Path Traversal).
- Файловые операции выполняются под блокировками (`LockerService`) для предотвращения гонок.
- Архивы (`ArchiveService`):
  - Защита от Zip Slip (проверка канонического пути распаковки).
  - Лимиты: размер одного файла, общий объём, количество файлов, глубина вложенных архивов, коэффициент сжатия.
  - Игнор символических ссылок.

## Журналы (логи)
- По умолчанию — файл `logs/file_manager-.log` (в рабочем каталоге исполняемого файла) и консоль.
- Формат и уровень настраиваются в `Serilog` в `appsettings.json`. Поддерживается вывод в Elasticsearch (см. `Serilog:WriteTo:Elasticsearch`).

## Docker Compose (зависимости)
Файл `compose.yaml` поднимает зависимости:
- `postgres` (БД для пользователей)
- `elasticsearch` и `kibana` (для логов, опционально)

Пример:
```bash
docker compose up -d 
```
Сервис приложения в `compose.yaml` не активен (закомментирован); запускайте приложение локально через `dotnet run`.

## Архитектура
- `Common/CommandBuilder.cs` — построение дерева команд (`System.CommandLine`) и маршрутизация на обработчики.
- `Feature/*` — команды/обработчики конкретных операций (через MediatR).
- `Services/*` — работу с ФС/архивами/блокировками реализуют сервисы.
- `Infrastructure/*` — репозитории и интерфейсы (пользователи, EF Core).
- `Models/*` — модели домена/DTO.

## Примечания по БД
- Для `register/login` нужен PostgreSQL (см. `ConnectionStrings:DefaultConnection`).
- Требуются применённые миграции (кратко; миграции в `Migrations/`).
