# База пациентов психиатрического диспансера

Веб-приложение для учёта пациентов, врачей и приёмов в психиатрическом диспансере.  
Построено на ASP.NET Core 8 с Clean Architecture, CQRS (MediatR) и EF Core (SQLite).

---

## Содержание

- [Требования](#требования)
- [Быстрый старт](#быстрый-старт)
- [Архитектура](#архитектура)
- [Структура проекта](#структура-проекта)
- [Технологический стек](#технологический-стек)
- [База данных](#база-данных)
- [Конфигурация](#конфигурация)
- [Работа с миграциями](#работа-с-миграциями)
- [Функциональность](#функциональность)
- [Дорожная карта](#дорожная-карта)

---

## Требования

| Компонент      | Версия    |
| -------------- | --------- |
| .NET SDK       | 8.0+      |
| ОС             | Windows / Linux / macOS |
| БД             | SQLite (встроена, не требует установки) |

Установка .NET SDK: https://dotnet.microsoft.com/download/dotnet/8.0

Проверка установки:

```bash
dotnet --version
# Ожидается: 8.0.x
```

---

## Быстрый старт

### 1. Клонировать репозиторий

```bash
git clone <url-репозитория>
cd DispancerPacientProject
```

### 2. Восстановить зависимости

```bash
dotnet restore
```

### 3. Создать миграцию (если отсутствует папка Migrations)

```bash
dotnet ef migrations add InitialCreate \
  --project src/DispancerPacient.Infrastructure \
  --startup-project src/DispancerPacient.Web \
  --output-dir Persistence/Migrations
```

### 4. Запустить приложение

```bash
dotnet run --project src/DispancerPacient.Web
```

Приложение будет доступно по адресу: **http://localhost:5000**

> В режиме Development миграции применяются автоматически при старте.  
> При первом запуске создаётся файл `dispanser.db` (SQLite) в корне Web-проекта.

### Альтернативный запуск (watch-режим с горячей перезагрузкой)

```bash
dotnet watch run --project src/DispancerPacient.Web
```

---

## Архитектура

Проект построен по принципам **Clean Architecture** с разделением на независимые слои:

```
                    ┌──────────────────┐
                    │    Web (UI)      │  Razor Pages, контроллеры
                    └────────┬─────────┘
                             │ зависит от
              ┌──────────────┴──────────────┐
              │                             │
    ┌─────────▼─────────┐     ┌─────────────▼─────────┐
    │    Application     │     │    Infrastructure     │
    │  (бизнес-логика)   │     │  (реализация доступа  │
    │  CQRS, MediatR     │     │   к данным, EF Core)  │
    └─────────┬──────────┘     └─────────────┬─────────┘
              │ зависит от                   │ зависит от
              └──────────────┬───────────────┘
                    ┌────────▼─────────┐
                    │     Domain       │  Сущности, перечисления,
                    │  (ядро системы)  │  доменные события
                    └────────┬─────────┘
                             │
                    ┌────────▼─────────┐
                    │     Shared       │  Result-паттерн
                    └──────────────────┘
```

**Направление зависимостей**: Domain не зависит ни от чего. Application зависит от Domain.
Infrastructure реализует интерфейсы из Application. Web связывает всё вместе через DI.

---

## Структура проекта

```
DispancerPacientProject/
├── DispancerPacient.sln                      # Solution-файл
├── README.md
├── docs/
│   └── senior-roadmap.md                     # Дорожная карта обучения
│
└── src/
    ├── DispancerPacient.Domain/              # Доменный слой (без зависимостей)
    │   ├── Entities/
    │   │   ├── BaseEntity.cs                 #   Базовая сущность (Id, timestamps, events)
    │   │   ├── Patient.cs                    #   Пациент
    │   │   ├── Doctor.cs                     #   Врач
    │   │   └── Appointment.cs                #   Приём
    │   ├── Enums/
    │   │   ├── Gender.cs                     #   Пол (Male, Female)
    │   │   ├── AppointmentType.cs            #   Тип приёма (первичный, повторный...)
    │   │   └── AppointmentStatus.cs          #   Статус (запланирован, проведён...)
    │   └── Events/
    │       ├── IDomainEvent.cs               #   Интерфейс доменного события
    │       ├── PatientCreatedEvent.cs        #   Событие: пациент создан
    │       └── DiagnosisChangedEvent.cs      #   Событие: диагноз изменён
    │
    ├── DispancerPacient.Shared/              # Общие утилиты
    │   └── Result.cs                         #   Паттерн Result<T> для ошибок без исключений
    │
    ├── DispancerPacient.Application/         # Слой бизнес-логики (CQRS)
    │   ├── Interfaces/
    │   │   └── IAppDbContext.cs              #   Абстракция DbContext
    │   ├── Common/Behaviors/
    │   │   └── ValidationBehavior.cs         #   MediatR pipeline (FluentValidation)
    │   ├── DependencyInjection.cs            #   Регистрация сервисов слоя
    │   ├── Patients/
    │   │   ├── Commands/
    │   │   │   ├── CreatePatientCommand.cs   #   Создание пациента
    │   │   │   ├── CreatePatientValidator.cs #   Валидация создания
    │   │   │   ├── UpdatePatientCommand.cs   #   Обновление пациента
    │   │   │   └── DeletePatientCommand.cs   #   Удаление пациента
    │   │   └── Queries/
    │   │       ├── GetPatientsQuery.cs       #   Список пациентов (поиск, фильтр)
    │   │       └── GetPatientByIdQuery.cs    #   Детали пациента
    │   ├── Doctors/Queries/
    │   │   ├── GetDoctorsQuery.cs            #   Список врачей
    │   │   └── GetDoctorSelectListQuery.cs   #   Выпадающий список врачей
    │   └── Appointments/
    │       ├── Commands/
    │       │   └── CreateAppointmentCommand.cs  # Создание приёма
    │       └── Queries/
    │           └── GetAppointmentsQuery.cs      # Список приёмов
    │
    ├── DispancerPacient.Infrastructure/      # Реализация доступа к данным
    │   ├── DependencyInjection.cs            #   Регистрация DbContext и SQLite
    │   └── Persistence/
    │       ├── AppDbContext.cs                #   EF Core DbContext
    │       └── Configurations/
    │           ├── PatientConfiguration.cs   #   Конфигурация таблицы Patients
    │           ├── DoctorConfiguration.cs    #   Конфигурация + seed-данные (3 врача)
    │           └── AppointmentConfiguration.cs # Конфигурация таблицы Appointments
    │
    └── DispancerPacient.Web/                 # Веб-слой (Razor Pages)
        ├── Program.cs                        #   Точка входа, конфигурация DI
        ├── Pages/
        │   ├── Index.cshtml(.cs)             #   Главная: дашборд со статистикой
        │   ├── Patients/                     #   CRUD пациентов
        │   │   ├── Index.cshtml(.cs)         #     Список (поиск, фильтр «на учёте»)
        │   │   ├── Create.cshtml(.cs)        #     Создание
        │   │   ├── Details.cshtml(.cs)       #     Карточка пациента + история приёмов
        │   │   ├── Edit.cshtml(.cs)          #     Редактирование
        │   │   └── Delete.cshtml(.cs)        #     Удаление с подтверждением
        │   └── Doctors/                      #   Управление врачами
        │       ├── Index.cshtml(.cs)         #     Список врачей
        │       └── Create.cshtml(.cs)        #     Добавление врача
        └── wwwroot/                          #   Статические файлы (CSS)
```

---

## Технологический стек

| Технология                 | Назначение                                      |
| -------------------------- | ----------------------------------------------- |
| ASP.NET Core 8             | Веб-фреймворк                                   |
| Razor Pages                | Серверный UI                                     |
| Entity Framework Core 8    | ORM, работа с БД                                 |
| SQLite                     | СУБД (файловая, без установки)                   |
| MediatR 12                 | Реализация CQRS (команды и запросы)              |
| FluentValidation 11        | Валидация команд в pipeline MediatR              |
| Bootstrap 5.3              | CSS-фреймворк (CDN)                              |
| jQuery Validation          | Клиентская валидация форм (CDN)                  |

---

## База данных

Приложение использует **SQLite** — файл `dispanser.db` создаётся автоматически.

### Схема данных

```
┌──────────────┐       ┌──────────────┐       ┌──────────────────┐
│   Doctors    │       │   Patients   │       │  Appointments    │
├──────────────┤       ├──────────────┤       ├──────────────────┤
│ Id (PK)      │◄──┐   │ Id (PK)      │◄──┐   │ Id (PK)          │
│ LastName     │   │   │ LastName     │   │   │ PatientId (FK)───┘
│ FirstName    │   │   │ FirstName    │   │   │ DoctorId (FK)────┐
│ MiddleName   │   │   │ MiddleName   │   │   │ AppointmentDate  │
│ Specialization│  │   │ BirthDate    │   │   │ AppointmentType  │
│ LicenseNumber│   │   │ Gender       │   │   │ Status           │
│ Phone        │   │   │ IpnCode (UQ) │   │   │ DiagnosisCode    │
│ Email        │   │   │ Address      │   │   │ Notes            │
│ IsActive     │   └───│ DoctorId(FK) │   │   │ CreatedAt        │
│ CreatedAt    │       │ DiagnosisCode│   │   │ UpdatedAt        │
│ UpdatedAt    │       │ MedRecNum(UQ)│   │   └──────────────────┘
└──────────────┘       │ IsOnRecord   │   │           │
                       │ Reg.Date     │   │           │
                       │ Dereg.Date   │   │           │
                       │ CreatedAt    │   └───────────┘
                       │ UpdatedAt    │
                       └──────────────┘
```

### Seed-данные

При первой миграции автоматически создаются 3 врача:

| ФИО                           | Специализация  | Лицензия |
| ----------------------------- | -------------- | -------- |
| Иванов Пётр Сергеевич        | Психиатр       | ПС-001   |
| Петрова Анна Викторовна       | Психотерапевт  | ПТ-002   |
| Сидоров Михаил Александрович  | Нарколог       | НК-003   |

---

## Конфигурация

Основные настройки в `src/DispancerPacient.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=dispanser.db"
  }
}
```

| Параметр                              | Описание                                  | По умолчанию        |
| ------------------------------------- | ----------------------------------------- | -------------------- |
| `ConnectionStrings:DefaultConnection` | Строка подключения SQLite                 | `Data Source=dispanser.db` |
| `Logging:LogLevel:Default`            | Уровень логирования                       | `Information`        |

Для переключения на другую СУБД (PostgreSQL, SQL Server) нужно:
1. Заменить NuGet-пакет в `Infrastructure.csproj`
2. Изменить `UseSqlite(...)` на `UseNpgsql(...)` / `UseSqlServer(...)` в `DependencyInjection.cs`
3. Обновить строку подключения

---

## Работа с миграциями

Все команды выполняются из **корня решения** (где `DispancerPacient.sln`):

```bash
# Создать новую миграцию
dotnet ef migrations add <ИмяМиграции> \
  --project src/DispancerPacient.Infrastructure \
  --startup-project src/DispancerPacient.Web \
  --output-dir Persistence/Migrations

# Применить миграции вручную
dotnet ef database update \
  --project src/DispancerPacient.Infrastructure \
  --startup-project src/DispancerPacient.Web

# Откатить последнюю миграцию
dotnet ef migrations remove \
  --project src/DispancerPacient.Infrastructure \
  --startup-project src/DispancerPacient.Web

# Сгенерировать SQL-скрипт миграции
dotnet ef migrations script \
  --project src/DispancerPacient.Infrastructure \
  --startup-project src/DispancerPacient.Web \
  --output migration.sql
```

> В Development-режиме миграции применяются автоматически при запуске (`Program.cs`).

---

## Функциональность

### Пациенты
- Список с поиском по ФИО, ИНН, номеру карты
- Фильтр «только на учёте»
- Создание с валидацией (ФИО обязательно, ИНН — 10 цифр, дата рождения в прошлом)
- Карточка пациента с историей приёмов (последние 10)
- Редактирование, удаление с подтверждением
- Автогенерация номера медкарты (формат `МК-{timestamp}`)

### Врачи
- Список с количеством пациентов на учёте
- Добавление нового врача
- Статус (активен / неактивен)

### Приёмы
- Список с фильтрацией по дате и врачу
- Создание приёма (выбор пациента, врача, типа)
- Типы: первичный, повторный, экстренный, консультация, комиссия
- Статусы: запланирован, проведён, отменён, неявка

### Дашборд
- Количество пациентов на учёте
- Количество активных врачей
- Приёмы на сегодня

---

## Сборка и публикация

### Debug-сборка

```bash
dotnet build
```

### Release-сборка

```bash
dotnet build -c Release
```

### Публикация (self-contained для Linux)

```bash
dotnet publish src/DispancerPacient.Web -c Release -r linux-x64 --self-contained -o ./publish
```

### Запуск опубликованного приложения

```bash
cd publish
./DispancerPacient.Web
```

---

## Дорожная карта

Подробная дорожная карта развития приложения — в файле [`docs/senior-roadmap.md`](docs/senior-roadmap.md).

| Версия | Описание                                                      | Статус       |
| ------ | ------------------------------------------------------------- | ------------ |
| v2     | Clean Architecture + CQRS + MediatR + FluentValidation + DDD | Текущая      |
| v3     | Minimal API, интеграция НСЗУ, Polly, Redis, Hangfire         | Планируется  |
| v4     | OpenTelemetry, Health Checks, Docker, CI/CD                   | Планируется  |
| v5     | Keycloak/OIDC, Kubernetes, CQRS read/write split              | Планируется  |

---

## Лицензия

Проект создан в учебных целях.
