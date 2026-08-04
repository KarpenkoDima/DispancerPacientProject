# База пациентов психиатрического диспансера

Учебный прототип веб-приложения для учёта пациентов,
врачей и приёмов в психиатрическом диспансере.

Проект создавался для изучения ASP.NET Core 8, Razor Pages,
Entity Framework Core, Clean Architecture, CQRS и MediatR.

> [!IMPORTANT]
> **Статус проекта:** разработка приостановлена.
>
> Репозиторий содержит незавершённый учебный архитектурный прототип.
> Сборка и запуск текущего состояния не подтверждены.
> Проект не предназначен для production-использования и работы
> с реальными персональными или медицинскими данными.

---

## Содержание

- [Известные ограничения](#известные-ограничения)
- [Требования](#требования)
- [Локальный запуск](#локальный-запуск)
- [Архитектурное направление](#архитектурное-направление)
- [Структура проекта](#структура-проекта)
- [Технологический стек](#технологический-стек)
- [База данных](#база-данных)
- [Конфигурация](#конфигурация)
- [Работа с миграциями](#работа-с-миграциями)
- [Реализовано в исходном коде](#реализовано-в-исходном-коде)
- [Сборка и публикация](#сборка-и-публикация)
- [Возможные направления развития](#возможные-направления-развития)
- [Лицензия](#лицензия)
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

## Известные ограничения

- сборка текущего снимка состояния не подтверждена;
- запуск приложения не подтверждён;
- initial migration EF Core отсутствует;
- автоматизированные тесты отсутствуют;
- CI/CD pipeline отсутствует;
- аутентификация не реализована;
- разграничение прав доступа не реализовано;
- аудит доступа к медицинским данным отсутствует;
- CQRS и MediatR применяются непоследовательно;
- часть Web-слоя обращается к контексту данных напрямую;
- присутствуют потенциальные ошибки компиляции Razor Pages;
- отдельные EF Core-запросы требуют проверки SQL-трансляции;
- удаление пациента реализовано как физическое удаление;
- optimistic concurrency отсутствует;
- пагинация списков отсутствует;
- проверка ИНН контролирует длину, но не гарантирует наличие только цифр;
- проект нельзя использовать с реальными данными пациентов.

## Локальный запуск

> [!WARNING]
> Текущий снимок состояния не проверен полной последовательностью
> `restore → build → migrate → run`.
>
> В репозитории отсутствует зафиксированная initial migration.
> Приведённые ниже команды сохранены как ориентир для возможного
> продолжения разработки, а не как гарантия успешного запуска.

### 1. Клонировать репозиторий

```bash
git clone <url-репозитория>
cd DispancerPacientProject
```

### 2. Восстановить зависимости

```bash
dotnet restore
```

### 3. Точка продолжения разработки: initial migration

В текущем снимке состояния initial migration отсутствует.

Для продолжения разработки её предполагалось создать командой:

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

При успешной сборке и запуске HTTP-профиль из
`launchSettings.json` предполагает адрес:

**http://localhost:5000**

При наличии и успешном применении миграций SQLite использует
файл `dispanser.db` в рабочем каталоге Web-приложения.

### Альтернативный запуск (watch-режим с горячей перезагрузкой)

```bash
dotnet watch run --project src/DispancerPacient.Web
```

---

## Архитектурное направление

Проект разрабатывался в направлении Clean Architecture
с разделением кода на Domain, Application, Infrastructure,
Shared и Web.

Архитектурные правила реализованы частично. CQRS и MediatR
используются не во всех сценариях, а часть Razor PageModel
обращается к контексту данных напрямую.

Диаграмма ниже отражает целевую архитектуру,
а не полностью завершённую реализацию.

```
                    ┌──────────────────┐
                    │    Web (UI)      │  
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
    └── DispancerPacient.Web/
      ├── Program.cs
      ├── appsettings.json
      ├── Properties/
      │   └── launchSettings.json
      ├── Pages/
      │   ├── Index.cshtml(.cs)
      │   ├── Error.cshtml(.cs)
      │   ├── _ViewImports.cshtml
      │   ├── _ViewStart.cshtml
      │   ├── Shared/
      │   │   ├── _Layout.cshtml
      │   │   └── _ValidationScriptsPartial.cshtml
      │   ├── Patients/
      │   │   ├── Index.cshtml(.cs)
      │   │   ├── Create.cshtml(.cs)
      │   │   ├── Details.cshtml(.cs)
      │   │   ├── Edit.cshtml(.cs)
      │   │   └── Delete.cshtml(.cs)
      │   ├── Doctors/
      │   │   ├── Index.cshtml(.cs)
      │   │   ├── Create.cshtml(.cs)
      │   │   └── Edit.cshtml(.cs)
      │   └── Appointments/
      │       ├── Index.cshtml(.cs)
      │       ├── Create.cshtml(.cs)
      │       └── Edit.cshtml(.cs)
      └── wwwroot/
          └── css/site.css
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

Проект настроен на использование SQLite.

При наличии и успешном применении EF Core migrations база данных
хранится в файле `dispanser.db` в рабочем каталоге Web-приложения.
В текущем снимке состояния initial migration отсутствует.

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

| ФИО                          | Специализация | Лицензия |
| ---------------------------- | ------------- | -------- |
| Иванов Пётр Сергеевич        | Психиатр      | ЛИЦ-001  |
| Петренко Ольга Владимировна  | Психотерапевт | ЛИЦ-002  |
| Сидоров Андрей Николаевич    | Нарколог      | ЛИЦ-003  |

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

## Реализовано в исходном коде

Перечисленная ниже функциональность присутствует в исходном коде,
но работоспособность всех сценариев текущего snapshot не подтверждена
автоматизированной сборкой и тестами.

### Пациенты — частично реализовано

- присутствуют страницы списка, создания, просмотра, редактирования и удаления;
- присутствуют CQRS-команды и запросы;
- реализована базовая валидация создания;
- сценарии не покрыты автоматизированными тестами;
- физическое удаление пациента требует пересмотра.

### Врачи — частично реализовано

- присутствует список врачей;
- присутствуют формы создания и редактирования;
- отображается статус активности;
- часть операций выполняется напрямую через контекст данных;
- сценарии не покрыты автоматизированными тестами.

### Приёмы — частично реализовано

- присутствует список приёмов;
- реализована фильтрация по дате;
- присутствуют формы создания и редактирования;
- используются типы и статусы приёмов;
- работоспособность Razor Pages текущего snapshot не подтверждена;
- сценарии не покрыты автоматизированными тестами.

### Дашборд — частично реализовано

- предусмотрено отображение числа пациентов на учёте;
- предусмотрено отображение числа активных врачей;
- предусмотрено отображение количества приёмов на текущую дату;
- корректность запросов и отображения не подтверждена тестами.

---

## Сборка и публикация

Ниже сохранены предполагаемые команды сборки и публикации.
Их успешное выполнение для текущего снимка состояния
не подтверждено.

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
dotnet publish src/DispancerPacient.Web \
  -c Release \
  -r linux-x64 \
  --self-contained \
  -o ./publish
```

### Запуск опубликованного приложения

```bash
cd publish
./DispancerPacient.Web
```

---

## Возможные направления развития

Разработка проекта приостановлена.

Перечисленные ниже этапы фиксируют ранее рассматривавшиеся идеи
и не являются утверждённым планом реализации.

| Направление | Состояние |
| --- | --- |
| Razor Pages, EF Core и CRUD-сценарии | Частично реализовано |
| Clean Architecture, CQRS и MediatR | Частично реализовано |
| Миграции, тесты и CI | Не реализовано |
| Authentication, authorization и audit | Не реализовано |
| Интеграция с НСЗУ и фоновые задачи | Идея |
| OpenTelemetry, Docker и deployment | Идея |
| OIDC, Kubernetes и разделение read/write | Исследовательская идея |

Историческая подробная дорожная карта сохранена в
[`docs/senior-roadmap.md`](docs/senior-roadmap.md).

---

## Лицензия

Отдельная лицензия для проекта пока не определена.

Репозиторий опубликован для демонстрации и фиксации учебного
прогресса. Отсутствие файла `LICENSE` не предоставляет
автоматического разрешения на копирование, изменение
или распространение исходного кода.
