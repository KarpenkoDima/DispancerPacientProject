# Senior ASP.NET Core Roadmap — Система «База пациентів психіатричного диспансеру»

> **Аудитория:** Middle .NET разработчик → Senior .NET архитект
> **Срок:** 6–12 месяцев
> **Контекст:** Медицинская система с чувствительными данными, интеграция с НСЗУ, production-grade требования

---

## Содержание

1. [Архитектурная эволюция проекта (v1 → v5)](#архитектурная-эволюция)
2. [Roadmap по месяцам](#roadmap-по-месяцам)
3. [Архитектура](#архитектура)
4. [Безопасность](#безопасность)
5. [Разграничение прав доступа](#разграничение-прав-доступа)
6. [Domain Modeling и DDD](#domain-modeling-и-ddd)
7. [Производительность и масштабируемость](#производительность-и-масштабируемость)
8. [Интеграция с НСЗУ](#интеграция-с-нсзу)
9. [Тестирование](#тестирование)
10. [Observability](#observability)
11. [Data Governance и защита медицинских данных](#data-governance)
12. [DevOps и CI/CD](#devops-и-cicd)
13. [Метрики зрелости проекта](#метрики-зрелости-проекта)
14. [Как понять, что я стал Senior](#как-понять-что-я-стал-senior)

---

## Архитектурная эволюция

### Версия 1 — Простой CRUD (Месяц 1–2)

**Что есть:** Razor Pages + EF Core + SQL Server. Всё в одном проекте. Контроллеры вызывают DbContext напрямую.

**Проблемы этой версии:**
- Бизнес-логика в контроллерах/пейджах
- Нет разделения ответственности
- Тестирование невозможно без БД
- При добавлении НСЗУ — всё лопается

```csharp
// ❌ Middle-разработчик: логика прямо в PageModel
public class PatientModel : PageModel
{
    private readonly AppDbContext _db;

    public async Task<IActionResult> OnPostAsync()
    {
        var patient = new Patient { Name = Input.Name, DiagnosisCode = Input.DiagnosisCode };
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        // Нет аудита, нет валидации, нет авторизации
        return RedirectToPage("./Index");
    }
}
```

**Задача:** Зафиксировать текущее состояние проекта. Описать его ограничения в Architecture Decision Record (ADR).

---

### Версия 2 — Модульная архитектура (Месяц 2–4)

**Что делаем:** Переход на Clean Architecture + CQRS + MediatR. Разбивка на модули.

```
src/
  DispancerPacient.Web/           ← Presentation (Razor Pages / API)
  DispancerPacient.Application/   ← Use Cases, Commands, Queries, DTOs
  DispancerPacient.Domain/        ← Entities, Value Objects, Domain Events
  DispancerPacient.Infrastructure/← EF Core, External services, Repositories
  DispancerPacient.Shared/        ← Cross-cutting: Result<T>, Errors, Extensions
```

**Senior-подход:** Модульный монолит — каждый модуль (Patients, Appointments, NSZU, Reports) имеет свой namespace, свою БД-схему, минимальные зависимости между модулями.

```csharp
// ✅ Senior: команда через MediatR
public record CreatePatientCommand(
    string FullName,
    DateOnly BirthDate,
    string IpnCode,
    Guid AssignedDoctorId
) : IRequest<Result<PatientId>>;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientId>>
{
    private readonly IPatientRepository _repository;
    private readonly IAuditLogger _audit;

    public async Task<Result<PatientId>> Handle(CreatePatientCommand cmd, CancellationToken ct)
    {
        // Бизнес-правило: IPN должен быть уникальным
        if (await _repository.ExistsByIpnAsync(cmd.IpnCode, ct))
            return Result.Failure<PatientId>(PatientErrors.DuplicateIpn);

        var patient = Patient.Create(cmd.FullName, cmd.BirthDate, cmd.IpnCode, cmd.AssignedDoctorId);
        await _repository.AddAsync(patient, ct);

        await _audit.LogAsync(AuditEvent.PatientCreated, patient.Id, ct);

        return Result.Success(patient.Id);
    }
}
```

---

### Версия 3 — Интеграции (Месяц 4–6)

**Что добавляем:**
- REST API endpoints (Minimal API) рядом с Razor Pages
- Интеграция с НСЗУ через HttpClient + Polly
- Outbox Pattern для надёжной доставки событий
- Redis для кэширования справочников (МКХ-10, лекарства)
- Background jobs (Hangfire) для отправки отчётов в НСЗУ

```csharp
// Outbox Pattern — гарантированная отправка в НСЗУ
public class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = null!;
    public string Payload { get; init; } = null!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
```

---

### Версия 4 — Production-Ready (Месяц 6–9)

**Что добавляем:**
- OpenTelemetry (трейсинг, метрики, логи)
- Health Checks для всех зависимостей
- Rate Limiting по роли пользователя
- Шифрование чувствительных полей в БД
- Полноценный аудит-лог в отдельной таблице
- Docker + GitHub Actions CI/CD
- Feature Flags (для постепенного rollout НСЗУ-интеграции)

---

### Версия 5 — Enterprise (Месяц 9–12)

**Что добавляем:**
- Keycloak / Azure AD B2C для централизованной аутентификации
- Kubernetes deployment + HPA (авто-масштабирование)
- CQRS с разделёнными read/write моделями (Read DB — отдельная схема)
- Event Sourcing для аудита (опционально)
- API Gateway (YARP) для маршрутизации
- Disaster Recovery plan и резервное копирование

---

## Roadmap по месяцам

| Месяц | Фокус | Deliverable |
|-------|-------|-------------|
| 1 | Анализ текущего кода, Clean Architecture структура | Рефакторинг на слои, первый ADR |
| 2 | CQRS + MediatR, FluentValidation | 3 команды, 3 запроса, валидация |
| 3 | ASP.NET Core Identity, Policy-based auth, Audit log | Роли и политики, аудит-таблица |
| 4 | Minimal API, OpenAPI, versioning | API v1 рядом с Razor Pages |
| 5 | НСЗУ интеграция (OAuth2 + Polly + Outbox) | Рабочий модуль НСЗУ |
| 6 | Redis кэш, Background jobs | Кэш справочников, Hangfire отчёты |
| 7 | OpenTelemetry, Health Checks, Rate Limiting | Dashboard метрик, alerting |
| 8 | Шифрование данных, GDPR-подобные требования | EncryptedColumn, Data masking |
| 9 | Docker, CI/CD, GitHub Actions | Pipeline: build → test → deploy |
| 10 | Тестирование (Unit, Integration, Architecture) | Coverage 70%+, ArchUnit тесты |
| 11 | Kubernetes базовый, Helm chart | Dev кластер работает |
| 12 | Performance profiling, Load testing | k6 отчёты, оптимизации |

---

## Архитектура

### Clean Architecture для медицинской системы

**Правило зависимостей:** Domain ← Application ← Infrastructure, Web → Application

```csharp
// Domain/Entities/Patient.cs — чистый домен, без EF, без HTTP
public class Patient : AggregateRoot<PatientId>
{
    private Patient() { } // для EF Core

    public PatientFullName FullName { get; private set; } = null!;
    public DateOnly BirthDate { get; private set; }
    public IpnCode Ipn { get; private set; } = null!;
    public MedicalRecordNumber MedRecordNumber { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    // Диагноз доступен только через специальный метод, не через публичное свойство
    private DiagnosisCode? _primaryDiagnosis;

    public static Patient Create(PatientFullName name, DateOnly birthDate, IpnCode ipn)
    {
        var patient = new Patient
        {
            Id = PatientId.New(),
            FullName = name,
            BirthDate = birthDate,
            Ipn = ipn,
            MedRecordNumber = MedicalRecordNumber.Generate()
        };
        patient.RaiseDomainEvent(new PatientCreatedEvent(patient.Id));
        return patient;
    }

    // Только врач может устанавливать диагноз — проверяется на уровне Use Case
    public void SetDiagnosis(DiagnosisCode code)
    {
        _primaryDiagnosis = code;
        RaiseDomainEvent(new DiagnosisSetEvent(Id, code));
    }

    public DiagnosisCode? GetDiagnosis() => _primaryDiagnosis;
}
```

### Vertical Slice Architecture — когда уместно

Для изолированных фич (например, «Отчёт НСЗУ за месяц») VSA работает лучше:

```
Features/
  NszuMonthlyReport/
    NszuMonthlyReportQuery.cs      ← Query + Handler в одном файле
    NszuMonthlyReportValidator.cs
    NszuMonthlyReportEndpoint.cs   ← Minimal API endpoint
    NszuMonthlyReportResponse.cs
```

### Типичные ошибки Middle-разработчика

```csharp
// ❌ Анемичная доменная модель — бизнес-логика в сервисе
public class PatientService
{
    public void SetDiagnosis(Patient patient, string code)
    {
        patient.DiagnosisCode = code; // просто присваивание
        patient.UpdatedAt = DateTime.Now;
    }
}

// ✅ Senior: логика инкапсулирована в домене
patient.SetDiagnosis(DiagnosisCode.Parse(code)); // валидация внутри Value Object
// UpdatedAt обновляется через Domain Event → Infrastructure handler
```

### Практическое задание #1

Реализовать `CreatePatientCommand` с:
- FluentValidation (IPN — 10 цифр, дата рождения не в будущем)
- Проверка дубликата по IPN
- Domain Event `PatientCreatedDomainEvent`
- Outbox запись для уведомления НСЗУ

---

## Безопасность

### ASP.NET Core Identity + OpenID Connect

```csharp
// Program.cs — конфигурация аутентификации
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.SlidingExpiration = false;
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // рабочий день
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Auth:Authority"];
    options.ClientId = builder.Configuration["Auth:ClientId"];
    options.ClientSecret = builder.Configuration["Auth:ClientSecret"]; // из секретов!
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("medical_records"); // кастомный scope для медданных
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
});
```

### HTTPS и защита транспорта

```csharp
// ❌ Middle: забывает про HSTS в production
app.UseHttpsRedirection();

// ✅ Senior: настраивает HSTS правильно
app.UseHsts(); // только в production
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
```

### Rate Limiting для медицинской системы

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Базовый лимит для всех
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Строгий лимит для доступа к диагнозам — аномалии в активности
    options.AddPolicy("DiagnosisAccess", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});
```

---

## Разграничение прав доступа

### Матрица ролей

| Действие | Врач | Медсестра | Статистик | Администратор | Бухгалтер |
|----------|------|-----------|-----------|---------------|-----------|
| Просмотр списка пациентов | ✅ (свои) | ✅ (отделение) | ✅ (анон.) | ✅ (все) | ❌ |
| Просмотр диагноза | ✅ (свои) | ❌ | ❌ | ✅ | ❌ |
| Создание пациента | ✅ | ✅ | ❌ | ✅ | ❌ |
| Редактирование диагноза | ✅ (свои) | ❌ | ❌ | ❌ | ❌ |
| Экспорт в НСЗУ | ❌ | ❌ | ✅ | ✅ | ❌ |
| Просмотр счетов | ❌ | ❌ | ❌ | ✅ | ✅ |
| Управление пользователями | ❌ | ❌ | ❌ | ✅ | ❌ |

### Policy-based авторизация

```csharp
// Application/Authorization/Policies.cs
public static class Policies
{
    public const string CanViewDiagnosis = "CanViewDiagnosis";
    public const string CanEditDiagnosis = "CanEditDiagnosis";
    public const string CanExportToNszu = "CanExportToNszu";
    public const string CanViewOwnPatientsOnly = "CanViewOwnPatientsOnly";
}

// Infrastructure/Authorization/DiagnosisAuthorizationHandler.cs
public class ViewDiagnosisRequirement : IAuthorizationRequirement { }

public class DiagnosisAuthorizationHandler
    : AuthorizationHandler<ViewDiagnosisRequirement, Patient>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ViewDiagnosisRequirement requirement,
        Patient patient)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = context.User.FindFirstValue(ClaimTypes.Role);

        // Администратор видит всё
        if (role == Roles.Administrator)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Врач — только свои пациенты
        if (role == Roles.Doctor && patient.AssignedDoctorId.ToString() == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Остальные — запрет
        context.Fail();
        return Task.CompletedTask;
    }
}

// Регистрация политик
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanViewDiagnosis, policy =>
        policy.Requirements.Add(new ViewDiagnosisRequirement()));

    options.AddPolicy(Policies.CanExportToNszu, policy =>
        policy.RequireRole(Roles.Statistician, Roles.Administrator));

    options.AddPolicy(Policies.CanEditDiagnosis, policy =>
        policy.RequireRole(Roles.Doctor)
              .RequireClaim("department")); // Только врач со своим отделением
});
```

### Аудит-лог — обязателен для медицинских систем

```csharp
// Domain/Audit/AuditEvent.cs
public class AuditEntry
{
    public long Id { get; init; }
    public string UserId { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string UserRole { get; init; } = null!;
    public string Action { get; init; } = null!;      // "ViewDiagnosis", "EditPatient"
    public string EntityType { get; init; } = null!;  // "Patient"
    public string EntityId { get; init; } = null!;
    public string? OldValues { get; init; }            // JSON снапшот
    public string? NewValues { get; init; }            // JSON снапшот
    public string IpAddress { get; init; } = null!;
    public string UserAgent { get; init; } = null!;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool WasSensitiveData { get; init; }        // был ли доступ к диагнозу
}

// Infrastructure/Audit/EfCoreAuditInterceptor.cs
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContext;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var context = eventData.Context!;
        var auditEntries = BuildAuditEntries(context);

        await base.SavingChangesAsync(eventData, result, ct);

        if (auditEntries.Any())
        {
            await context.Set<AuditEntry>().AddRangeAsync(auditEntries, ct);
            await context.SaveChangesAsync(ct);
        }

        return result;
    }

    private List<AuditEntry> BuildAuditEntries(DbContext context)
    {
        var entries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditEntry) continue; // избегаем рекурсии
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var isSensitive = entry.Entity is Patient or DiagnosisRecord;

            entries.Add(new AuditEntry
            {
                UserId = _currentUser.UserId,
                UserName = _currentUser.UserName,
                UserRole = _currentUser.Role,
                Action = entry.State.ToString(),
                EntityType = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKey(entry),
                OldValues = entry.State == EntityState.Modified
                    ? JsonSerializer.Serialize(entry.OriginalValues.ToObject())
                    : null,
                NewValues = entry.State != EntityState.Deleted
                    ? JsonSerializer.Serialize(entry.CurrentValues.ToObject())
                    : null,
                IpAddress = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = _httpContext.HttpContext?.Request.Headers.UserAgent ?? "unknown",
                WasSensitiveData = isSensitive
            });
        }

        return entries;
    }
}
```

### Практическое задание #2

1. Создать `ICurrentUserService` с mock для тестов
2. Реализовать политику «врач видит только пациентов своего отделения»
3. Написать middleware, который при попытке доступа к диагнозу без прав — логирует Security Event (не просто 403)
4. Написать интеграционный тест: медсестра не может получить диагноз

---

## Domain Modeling и DDD

### Value Objects для медицинской системы

```csharp
// Domain/ValueObjects/IpnCode.cs
public sealed record IpnCode
{
    public string Value { get; }

    private IpnCode(string value) => Value = value;

    public static Result<IpnCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<IpnCode>(IpnErrors.Empty);

        var cleaned = value.Trim();
        if (cleaned.Length != 10 || !cleaned.All(char.IsDigit))
            return Result.Failure<IpnCode>(IpnErrors.InvalidFormat);

        // Проверка контрольной цифры украинского ИНН
        if (!IsValidChecksum(cleaned))
            return Result.Failure<IpnCode>(IpnErrors.InvalidChecksum);

        return Result.Success(new IpnCode(cleaned));
    }

    private static bool IsValidChecksum(string ipn)
    {
        int[] weights = [-1, 5, 7, 9, 4, 6, 10, 5, 7];
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (ipn[i] - '0') * weights[i];

        int checkDigit = ((sum % 11) + 11) % 10;
        return checkDigit == (ipn[9] - '0');
    }

    public override string ToString() => Value;
}

// Domain/ValueObjects/DiagnosisCode.cs — МКХ-10
public sealed record DiagnosisCode
{
    public string Code { get; }
    public string Description { get; }

    private DiagnosisCode(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public static Result<DiagnosisCode> Create(string code, string description)
    {
        // Формат МКХ-10: одна буква + 2 цифры + опц. точка + цифра (F20.0, F32)
        if (!Regex.IsMatch(code, @"^[A-Z]\d{2}(\.\d)?$"))
            return Result.Failure<DiagnosisCode>(DiagnosisErrors.InvalidIcd10Format);

        return Result.Success(new DiagnosisCode(code.ToUpperInvariant(), description));
    }
}
```

### Агрегаты и границы транзакций

```csharp
// ❌ Middle: два агрегата в одной транзакции
public async Task Handle(AssignAppointmentCommand cmd, CancellationToken ct)
{
    var patient = await _patientRepo.GetAsync(cmd.PatientId, ct);
    var doctor = await _doctorRepo.GetAsync(cmd.DoctorId, ct);

    patient.AssignDoctor(doctor.Id);
    doctor.AddAppointment(cmd.Date); // ❌ нарушает границу агрегата

    await _patientRepo.SaveAsync(ct);
    await _doctorRepo.SaveAsync(ct); // два агрегата — eventual consistency!
}

// ✅ Senior: один агрегат за раз, через Domain Events
public async Task Handle(AssignAppointmentCommand cmd, CancellationToken ct)
{
    var patient = await _patientRepo.GetAsync(cmd.PatientId, ct);
    patient.AssignDoctor(cmd.DoctorId); // Поднимает DoctorAssignedEvent

    await _patientRepo.SaveAsync(ct);
    // DoctorAssignedEvent обрабатывается отдельным handler-ом → обновляет расписание врача
}
```

### Типичные ошибки Middle в DDD

- Сделать все поля публичными setters — «анемичная модель»
- Забыть инвариант агрегата (пациент без IPN не должен существовать)
- Класть бизнес-логику в Application layer вместо Domain
- Один большой агрегат `Patient` с 50 полями — нужно разбить на `Patient` + `MedicalRecord` + `Appointments`

---

## Производительность и масштабируемость

### EF Core — правильные запросы

```csharp
// ❌ Middle: N+1 проблема
var patients = await _db.Patients.ToListAsync();
foreach (var p in patients)
{
    var appointments = await _db.Appointments
        .Where(a => a.PatientId == p.Id).ToListAsync(); // N запросов!
}

// ✅ Senior: один запрос с Include или split query
var patients = await _db.Patients
    .AsSplitQuery()
    .Include(p => p.Appointments.Where(a => a.Date >= DateTime.Today))
    .AsNoTracking() // для read-only запросов
    .Where(p => p.IsActive && p.AssignedDoctorId == currentDoctorId)
    .OrderBy(p => p.FullName.LastName)
    .Select(p => new PatientListItem(p.Id, p.FullName.Full, p.BirthDate, p.Appointments.Count))
    .ToListAsync(ct);
```

### Redis кэширование справочников

```csharp
// Infrastructure/Caching/Icd10CacheService.cs
public class Icd10CacheService : IIcd10Service
{
    private readonly IDistributedCache _cache;
    private readonly IIcd10Repository _repository;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<IEnumerable<Icd10Code>> SearchAsync(string term, CancellationToken ct)
    {
        var cacheKey = $"icd10:search:{term.ToLowerInvariant()}";

        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<IEnumerable<Icd10Code>>(cached)!;

        var results = await _repository.SearchAsync(term, ct);

        await _cache.SetStringAsync(cacheKey,
            JsonSerializer.Serialize(results),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
            ct);

        return results;
    }
}
```

### Пагинация — обязательно cursor-based для больших данных

```csharp
// ❌ Middle: offset пагинация — деградирует на больших данных
var skip = (page - 1) * pageSize;
var patients = await _db.Patients.Skip(skip).Take(pageSize).ToListAsync();

// ✅ Senior: cursor-based пагинация
public record PatientCursorQuery(string? Cursor, int PageSize = 20);

public async Task<PagedResult<PatientListItem>> Handle(PatientCursorQuery query, CancellationToken ct)
{
    var q = _db.Patients.AsNoTracking().Where(p => p.IsActive);

    if (query.Cursor is not null)
    {
        var cursor = DecodeCursor(query.Cursor); // Base64 → {LastName, Id}
        q = q.Where(p => p.FullName.LastName > cursor.LastName ||
                         (p.FullName.LastName == cursor.LastName && p.Id > cursor.Id));
    }

    var items = await q
        .OrderBy(p => p.FullName.LastName).ThenBy(p => p.Id)
        .Take(query.PageSize + 1)
        .Select(p => new PatientListItem(p.Id, p.FullName.Full))
        .ToListAsync(ct);

    var hasMore = items.Count > query.PageSize;
    var nextCursor = hasMore ? EncodeCursor(items[^2]) : null;

    return new PagedResult<PatientListItem>(items.Take(query.PageSize), nextCursor);
}
```

### Практическое задание #3

1. Настроить Redis в Docker Compose
2. Кэшировать список МКХ-10 кодов (24 часа, инвалидировать при обновлении)
3. Добавить Benchmark тест (BenchmarkDotNet) для сравнения: без кэша vs с кэшем
4. Настроить connection pool для SQL Server

---

## Интеграция с НСЗУ

### Архитектура интеграции

```
DispancerPacient.Infrastructure/
  Nszu/
    NszuClient.cs              ← HttpClient + авторизация
    NszuAuthService.cs         ← OAuth2 token management
    NszuRequestSigner.cs       ← Подпись запросов (если требуется)
    Models/
      NszuPatientDto.cs
      NszuDeclarationDto.cs
    Retry/
      NszuResilienceHandler.cs ← Polly pipeline
```

### OAuth2 авторизация для НСЗУ

```csharp
// Infrastructure/Nszu/NszuAuthService.cs
public class NszuAuthService : INszuAuthService
{
    private readonly IHttpClientFactory _factory;
    private readonly IOptionsMonitor<NszuOptions> _options;
    private readonly IDistributedCache _cache;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        const string cacheKey = "nszu:access_token";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return cached;

        var client = _factory.CreateClient("NszuAuth");
        var opts = _options.CurrentValue;

        var response = await client.PostAsync("connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = opts.ClientId,
                ["client_secret"] = opts.ClientSecret,
                ["scope"] = "ehealth:declaration:read ehealth:patient:read"
            }), ct);

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(ct: ct);

        // Кэшируем с буфером 60 секунд до истечения
        await _cache.SetStringAsync(cacheKey, token!.AccessToken,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(token.ExpiresIn - 60)
            }, ct);

        return token.AccessToken;
    }
}
```

### Polly Resilience Pipeline

```csharp
// Infrastructure/Nszu/NszuResiliencePipeline.cs
public static class NszuResiliencePipelineExtensions
{
    public static IHttpClientBuilder AddNszuResiliencePipeline(this IHttpClientBuilder builder)
    {
        return builder.AddResilienceHandler("nszu", pipeline =>
        {
            // 1. Retry с exponential backoff
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 4,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode is
                        HttpStatusCode.TooManyRequests or
                        HttpStatusCode.BadGateway or
                        HttpStatusCode.ServiceUnavailable or
                        HttpStatusCode.GatewayTimeout),
                OnRetry = args =>
                {
                    Log.Warning("НСЗУ retry #{Attempt} після {Delay}",
                        args.AttemptNumber, args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            });

            // 2. Circuit Breaker
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(60),
                OnOpened = args =>
                {
                    Log.Error("Circuit breaker НСЗУ відкрито. Пауза {Duration}", args.BreakDuration);
                    return ValueTask.CompletedTask;
                }
            });

            // 3. Timeout
            pipeline.AddTimeout(TimeSpan.FromSeconds(15));
        });
    }
}
```

### Outbox Pattern для надёжной интеграции

```csharp
// Сохраняем запрос в БД атомарно с основными данными
// Отдельный background job обрабатывает и отправляет в НСЗУ

// Infrastructure/Outbox/ProcessOutboxMessagesJob.cs
public class ProcessOutboxMessagesJob : IJob
{
    private readonly AppDbContext _db;
    private readonly INszuClient _nszuClient;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public async Task Execute(IJobExecutionContext context)
    {
        var messages = await _db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(context.CancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessageAsync(message, context.CancellationToken);
                message.ProcessedAt = DateTime.UtcNow;
                _logger.LogInformation("Outbox {Id} оброблено: {Type}", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogError(ex, "Помилка обробки Outbox {Id}", message.Id);
            }
        }

        await _db.SaveChangesAsync(context.CancellationToken);
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken ct)
    {
        switch (message.Type)
        {
            case nameof(PatientCreatedForNszuEvent):
                var payload = JsonSerializer.Deserialize<PatientCreatedForNszuEvent>(message.Payload)!;
                await _nszuClient.RegisterPatientAsync(payload, ct);
                break;
            // другие типы...
        }
    }
}
```

### Логирование взаимодействия с НСЗУ

```csharp
// Infrastructure/Nszu/NszuLoggingHandler.cs
public class NszuLoggingHandler : DelegatingHandler
{
    private readonly ILogger<NszuLoggingHandler> _logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var requestId = Guid.NewGuid();
        using var activity = Activity.Current; // OpenTelemetry

        _logger.LogInformation(
            "→ НСЗУ {Method} {Uri} [RequestId={RequestId}]",
            request.Method, request.RequestUri, requestId);

        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;

        try
        {
            response = await base.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "← НСЗУ помилка {Method} {Uri} [{Duration}ms]",
                request.Method, request.RequestUri, sw.ElapsedMilliseconds);
            throw;
        }

        _logger.LogInformation(
            "← НСЗУ {StatusCode} {Method} {Uri} [{Duration}ms]",
            (int)response.StatusCode, request.Method, request.RequestUri, sw.ElapsedMilliseconds);

        return response;
    }
}
```

### Практическое задание #4

1. Реализовать `NszuClient.GetPatientDeclarationsAsync(IpnCode ipn)`
2. Настроить Polly pipeline (retry + circuit breaker + timeout)
3. Написать mock НСЗУ сервер (WireMock.Net) для тестов
4. Реализовать Outbox: пациент создан → запись в Outbox → job отправляет в НСЗУ

---

## Тестирование

### Пирамида тестов для медицинской системы

```
         /\
        /  \   E2E (Playwright) — 10%
       /----\
      /      \ Integration — 30%
     /--------\
    /          \ Unit — 60%
   /____________\
```

### Unit тесты для Domain

```csharp
// Tests/Domain/PatientTests.cs
public class PatientTests
{
    [Fact]
    public void Create_WithValidData_RaisesPatientCreatedEvent()
    {
        var name = PatientFullName.Create("Іваненко", "Іван", "Іванович").Value;
        var ipn = IpnCode.Create("3456789012").Value;
        var birthDate = new DateOnly(1985, 3, 15);

        var patient = Patient.Create(name, birthDate, ipn);

        patient.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PatientCreatedEvent>();
    }

    [Fact]
    public void SetDiagnosis_ByNonDoctor_ShouldNotBeAllowedAtDomainLevel()
    {
        // Domain не знает про пользователя — это задача Application layer
        // Тест проверяет что диагноз корректно устанавливается
        var patient = CreateValidPatient();
        var diagCode = DiagnosisCode.Create("F20", "Шизофренія").Value;

        patient.SetDiagnosis(diagCode);

        patient.GetDiagnosis().Should().Be(diagCode);
        patient.DomainEvents.Should().Contain(e => e is DiagnosisSetEvent);
    }

    [Theory]
    [InlineData("123456789")]   // 9 цифр
    [InlineData("12345678901")] // 11 цифр
    [InlineData("abcdefghij")] // не цифры
    public void IpnCode_WithInvalidFormat_ReturnsFailure(string invalidIpn)
    {
        var result = IpnCode.Create(invalidIpn);
        result.IsFailure.Should().BeTrue();
    }
}
```

### Integration тесты с реальной БД

```csharp
// Tests/Integration/PatientRepositoryTests.cs
public class PatientRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    [Fact]
    public async Task AddAsync_ThenGetAsync_ReturnsCorrectPatient()
    {
        await using var context = _fixture.CreateContext();
        var repository = new PatientRepository(context);

        var patient = PatientFactory.CreateValid();
        await repository.AddAsync(patient, CancellationToken.None);
        await context.SaveChangesAsync();

        var retrieved = await repository.GetAsync(patient.Id, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Ipn.Should().Be(patient.Ipn);
    }
}

// Fixture с реальным SQL Server (testcontainers)
public class TestDatabaseFixture : IAsyncLifetime
{
    private MsSqlContainer _container = null!;
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithPassword("TestPass123!")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Apply migrations
        using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public DbContext CreateContext() => new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

### Architecture Tests (ArchUnitNET)

```csharp
// Tests/Architecture/DependencyTests.cs
public class DependencyTests
{
    [Fact]
    public void Domain_ShouldNot_DependOnInfrastructure()
    {
        var domain = typeof(Patient).Assembly;
        var infrastructure = typeof(AppDbContext).Assembly;

        var rule = Types().That().ResideInAssembly(domain)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(infrastructure));

        rule.Check(Architecture.Load(domain, infrastructure));
    }

    [Fact]
    public void CommandHandlers_Should_ResideIn_ApplicationLayer()
    {
        var handlers = Types().That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should().ResideInNamespace("DispancerPacient.Application");

        handlers.Check(Architecture.Load(typeof(CreatePatientCommandHandler).Assembly));
    }
}
```

---

## Observability

### OpenTelemetry — полная настройка

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "DispancerPacient",
        serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString()))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
            // Маскируем чувствительные данные из URL
            opts.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("user.role", request.HttpContext.User.FindFirstValue(ClaimTypes.Role));
                // НЕ логируем IPN или имена пациентов в trace tags!
            };
        })
        .AddEntityFrameworkCoreInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true; // только в dev!
            opts.SetDbStatementForStoredProcedure = true;
        })
        .AddHttpClientInstrumentation()
        .AddSource("DispancerPacient.Nszu") // кастомный ActivitySource
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("DispancerPacient.Business") // бизнес-метрики
        .AddPrometheusExporter())
    .WithLogging(logging => logging
        .AddOtlpExporter());

// Кастомные бизнес-метрики
public class DispancerMetrics
{
    private readonly Counter<long> _patientsCreated;
    private readonly Counter<long> _nszuRequestsSent;
    private readonly Histogram<double> _nszuResponseTime;

    public DispancerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("DispancerPacient.Business");
        _patientsCreated = meter.CreateCounter<long>("patients.created.total");
        _nszuRequestsSent = meter.CreateCounter<long>("nszu.requests.total");
        _nszuResponseTime = meter.CreateHistogram<double>("nszu.response.duration.ms");
    }

    public void RecordPatientCreated() => _patientsCreated.Add(1);
    public void RecordNszuRequest(string endpoint, double durationMs)
    {
        _nszuRequestsSent.Add(1, new TagList { { "endpoint", endpoint } });
        _nszuResponseTime.Record(durationMs, new TagList { { "endpoint", endpoint } });
    }
}
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddRedis(redisConnectionString, "redis")
    .AddUrlGroup(new Uri(nszuBaseUrl + "/health"), "nszu-api",
        tags: ["external"],
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<OutboxHealthCheck>("outbox-processor", tags: ["background"]);

// Кастомный HealthCheck для Outbox
public class OutboxHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        var oldestUnprocessed = await _db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Select(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (oldestUnprocessed == default)
            return HealthCheckResult.Healthy("Немає необроблених повідомлень");

        var age = DateTime.UtcNow - oldestUnprocessed;

        return age switch
        {
            { TotalMinutes: < 5 } => HealthCheckResult.Healthy($"Найстаріше: {age.TotalSeconds:F0}с"),
            { TotalMinutes: < 30 } => HealthCheckResult.Degraded($"Затримка: {age.TotalMinutes:F0}хв"),
            _ => HealthCheckResult.Unhealthy($"Критична затримка: {age.TotalHours:F1}год")
        };
    }
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("external")
});
```

---

## Data Governance

### Шифрование чувствительных полей

```csharp
// Infrastructure/Encryption/EncryptedConverter.cs
public class EncryptedConverter : ValueConverter<string, string>
{
    public EncryptedConverter(IDataProtector protector)
        : base(
            v => protector.Protect(v),         // при сохранении — шифруем
            v => protector.Unprotect(v))        // при чтении — расшифровываем
    { }
}

// Infrastructure/Persistence/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder builder)
{
    var protector = _dataProtectionProvider.CreateProtector("PatientData");
    var encryptedConverter = new EncryptedConverter(protector);

    builder.Entity<Patient>().Property(p => p.Ipn)
        .HasConversion(
            ipn => protector.Protect(ipn.Value),
            str => IpnCode.CreateFromEncrypted(protector.Unprotect(str)));

    // Диагноз — строго зашифрован
    builder.Entity<DiagnosisRecord>().Property(d => d.DiagnosisCode)
        .HasConversion(encryptedConverter);

    builder.Entity<DiagnosisRecord>().Property(d => d.Notes)
        .HasConversion(encryptedConverter);
}
```

### Маскирование данных в логах

```csharp
// Shared/Logging/PatientDataDestructuringPolicy.cs
// Serilog: никогда не логировать сырые медданные
public class PatientDataDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue result)
    {
        if (value is not Patient patient)
        {
            result = null!;
            return false;
        }

        result = factory.CreatePropertyValue(new
        {
            PatientId = patient.Id,
            // Только часть ИНН для идентификации в логах
            IpnMasked = $"****{patient.Ipn.Value[^4..]}",
            // НЕ логируем: FullName, DiagnosisCode, BirthDate
        });
        return true;
    }
}
```

### Принцип минимальных данных в API ответах

```csharp
// ❌ Middle: возвращает весь объект
public PatientDto GetPatient(Guid id) => _mapper.Map<PatientDto>(patient);

// ✅ Senior: разные DTO для разных ролей
public object GetPatient(Guid id, ClaimsPrincipal user)
{
    var patient = _repository.Get(id);

    return user.IsInRole(Roles.Doctor) && patient.AssignedDoctorId == user.GetUserId()
        ? new PatientDetailedDto(patient)    // с диагнозом
        : new PatientBasicDto(patient);       // без диагноза, с маскированным IPN
}
```

---

## DevOps и CI/CD

### Dockerfile — правильная многоэтапная сборка

```dockerfile
# Этап 1: сборка
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/DispancerPacient.Web/DispancerPacient.Web.csproj", "Web/"]
COPY ["src/DispancerPacient.Application/DispancerPacient.Application.csproj", "Application/"]
COPY ["src/DispancerPacient.Domain/DispancerPacient.Domain.csproj", "Domain/"]
COPY ["src/DispancerPacient.Infrastructure/DispancerPacient.Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "Web/DispancerPacient.Web.csproj"

COPY . .
RUN dotnet publish "src/DispancerPacient.Web/DispancerPacient.Web.csproj" \
    -c Release -o /app/publish --no-restore

# Этап 2: runtime (минимальный образ)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Безопасность: запускаем не от root
RUN adduser --disabled-password --no-create-home appuser
USER appuser

COPY --from=build /app/publish .
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
  CMD curl -f http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "DispancerPacient.Web.dll"]
```

### GitHub Actions Pipeline

```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: "TestPass123!"
          ACCEPT_EULA: "Y"
        ports: ["1433:1433"]
      redis:
        image: redis:7-alpine
        ports: ["6379:6379"]

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Run Unit Tests
        run: dotnet test tests/Unit --no-build -c Release \
          --logger "trx;LogFileName=unit-results.trx" \
          --collect "XPlat Code Coverage"

      - name: Run Integration Tests
        run: dotnet test tests/Integration --no-build -c Release \
          --logger "trx;LogFileName=integration-results.trx"
        env:
          ConnectionStrings__Default: "Server=localhost;Database=TestDb;User Id=sa;Password=TestPass123!;TrustServerCertificate=True"
          ConnectionStrings__Redis: "localhost:6379"

      - name: Code Coverage Report
        uses: danielpalme/ReportGenerator-GitHub-Action@5
        with:
          reports: '**/coverage.cobertura.xml'
          targetdir: 'coveragereport'
          reporttypes: 'HtmlInline;Cobertura'

      - name: Security Scan (Snyk)
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}

  docker-build:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4

      - name: Build Docker image
        run: docker build -t dispancer-pacient:${{ github.sha }} .

      - name: Scan image for vulnerabilities
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: 'dispancer-pacient:${{ github.sha }}'
          exit-code: '1'
          severity: 'CRITICAL'
```

### Docker Compose для локальной разработки

```yaml
# docker-compose.yml
version: '3.9'

services:
  app:
    build: .
    ports: ["8080:8080"]
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Server=db;Database=DispancerDb;User Id=sa;Password=DevPass123!;TrustServerCertificate=True
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_started

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "DevPass123!"
      ACCEPT_EULA: "Y"
    ports: ["1433:1433"]
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: ["CMD", "/opt/mssql-tools/bin/sqlcmd", "-U", "sa", "-P", "DevPass123!", "-Q", "SELECT 1"]
      interval: 10s
      timeout: 5s
      retries: 10

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]
    command: redis-server --appendonly yes
    volumes:
      - redisdata:/data

  seq:
    image: datalust/seq:latest
    ports: ["5341:5341", "80:80"]
    environment:
      ACCEPT_EULA: "Y"

  otel-collector:
    image: otel/opentelemetry-collector-contrib
    volumes:
      - ./infra/otel-config.yaml:/etc/otel/config.yaml
    command: ["--config=/etc/otel/config.yaml"]
    ports: ["4317:4317", "4318:4318"]

volumes:
  sqldata:
  redisdata:
```

---

## Метрики зрелості проекту

### Чек-ліст для кожної версії

**Версія 1 (CRUD):**
- [ ] Є EF Core міграції (не ручний SQL)
- [ ] Є базова валідація вводу
- [ ] Немає паролів у коді

**Версія 2 (Модульна):**
- [ ] Clean Architecture: Domain не залежить від Infrastructure
- [ ] CQRS: команди і запити розділені
- [ ] FluentValidation для всіх команд
- [ ] Unit тести для Domain (coverage > 80%)

**Версія 3 (Інтеграції):**
- [ ] Outbox Pattern для НСЗУ
- [ ] Polly pipeline (retry + circuit breaker)
- [ ] Redis кеш для довідників
- [ ] Інтеграційні тести з WireMock

**Версія 4 (Production-Ready):**
- [ ] OpenTelemetry (трейсинг + метрики)
- [ ] Health Checks для всіх залежностей
- [ ] Шифрування чутливих полів у БД
- [ ] Аудит-лог для всіх дій з пацієнтами
- [ ] Rate Limiting
- [ ] Docker + CI/CD pipeline
- [ ] Security scan у pipeline (Snyk/Trivy)

**Версія 5 (Enterprise):**
- [ ] Kubernetes deployment з HPA
- [ ] Centralized identity (Keycloak)
- [ ] SLO визначені та вимірюються (uptime 99.9%)
- [ ] Disaster Recovery протокол
- [ ] DORA метрики вимірюються
- [ ] Architecture Decision Records (ADR) для всіх ключових рішень

---

## Як зрозуміти, що я став Senior

### Технічні індикатори

**Middle думає:**
> «Як реалізувати цю фічу?»

**Senior думає:**
> «Чи потрібна ця фіча взагалі? Які trade-offs? Як це вплине на безпеку пацієнтів? Що відбудеться під навантаженням?»

---

### 10 питань, на які Senior відповідає без вагань

1. **«Де зберігається секрет підключення до НСЗУ?»**
   Senior: У Azure Key Vault / AWS Secrets Manager. Ніколи в `appsettings.json`, ніколи в git.

2. **«Чому у нас Circuit Breaker відкрився о 3 ночі?»**
   Senior: Дивиться на OpenTelemetry dashboard, бачить аномалію у НСЗУ latency за 15 хвилин до відкриття, коригує threshold.

3. **«Медсестра каже, що бачить діагнози пацієнтів. Це баг?»**
   Senior: Перевіряє policy, audit log, знаходить що медсестра має тимчасову роль Doctor через помилку адміна. Знаходить root cause, не симптом.

4. **«Додати поле у таблицю пацієнтів»**
   Senior: Питає — навіщо? Як воно буде використовуватись? Чи є альтернатива без зміни схеми? Якщо додає — пише міграцію з rollback скриптом.

5. **«Запити до БД стали повільними»**
   Senior: Вмикає slow query log, аналізує execution plan, додає правильний індекс або переписує LINQ запит.

6. **«Нам потрібно зберігати фото пацієнтів»**
   Senior: Зберігаємо у blob storage (не в БД), URL зашифрований, доступ через pre-signed URL з TTL 15 хвилин, аудит кожного доступу.

7. **«Як тестувати без реального НСЗУ?»**
   Senior: WireMock.Net для integration тестів, stub для unit тестів, окреме dev-середовище НСЗУ sandbox.

8. **«Деплой зламав production»**
   Senior: Feature flag + canary deployment — 5% трафіку на нову версію, автоматичний rollback при зростанні error rate.

9. **«Треба зробити звіт для МОЗ»**
   Senior: Read-model окремо від write-model, звітний запит не навантажує операційну БД.

10. **«Як ти знаєш, що система здорова?»**
    Senior: SLO dashboard, алерти на error rate > 1%, p99 latency > 2s, outbox queue > 100 повідомлень.

---

### Поведінкові індикатори Senior

| Ознака | Middle | Senior |
|--------|--------|--------|
| Код-рев'ю | Дивиться на синтаксис | Дивиться на безпеку, trade-offs, тестованість |
| Помилка в prod | Виправляє симптом | Знаходить root cause, додає тест, додає алерт |
| Нова вимога | Одразу кодить | Задає 5 питань, малює діаграму, потім кодить |
| Технічний борг | Ігнорує | Визначає, пріоритезує, планує |
| Безпека | «Додам пізніше» | Вбудована з першого дня |
| Документація | README.md | ADR + C4 діаграми + runbook |
| Монітоинг | «Якщо щось зламається — побачимо» | SLO, алерти, дашборди, post-mortem |

---

### Фінальний Self-Assessment (по кожному блоку)

Оцінюй себе від 1 до 5:

- **Архітектура:** Я можу обґрунтувати кожне архітектурне рішення у проекті
- **Безпека:** Я знаходжу вразливості на code review, не лише пишу захищений код
- **НСЗУ інтеграція:** Я можу налагодити проблему інтеграції по логах без запуску дебагера
- **Тести:** Мої тести ловлять регресії до деплою в production
- **Observability:** Я можу відповісти на будь-яке питання про поведінку системи за останні 24 години
- **Менторство:** Я можу пояснити будь-яке рішення junior/middle розробнику з прикладами

**Якщо по всіх пунктах оцінка 4+ — ти Senior.**

---

> **Ключова думка:**
> Senior — це не той, хто знає всі технології.
> Senior — це той, хто знає, **яку** технологію **не** застосовувати,
> **чому** саме так, і може **переконати** команду у правильності рішення.
> У медичній системі це особливо важливо — ціна помилки вимірюється не грошима, а довірою та безпекою людей.
