# PayderPay

> 🌐 **Canlı uygulama:** [www.veranogardenhome.com](https://www.veranogardenhome.com)

PayderPay, abonelik bazlı tekrarlayan ödemeleri (elektrik, su, doğalgaz, internet vb.) tek bir platform üzerinden yönetmeyi sağlayan bir **fatura sorgulama ve ödeme** uygulamasıdır. Kullanıcılar aboneliklerini sisteme ekler, 3. parti fatura sağlayıcısı üzerinden borçlarını anlık olarak sorgular, ana hesap bakiyelerinden ödeme yapar; yaklaşan ödeme tarihleri için otomatik e-posta hatırlatma alır.

## Kapsam

- **Hesap yönetimi:** Müşteri kayıt/giriş (JWT + refresh token), ana hesap (cüzdan) bakiye takibi
- **Abonelik yönetimi:** Birden çok sağlayıcı tipi (elektrik, su, doğalgaz, internet) için abonelik oluşturma/silme
- **Borç sorgulama:** Aboneliğin güncel borçlarını 3rd-party Billing API üzerinden sorgulama, Redis ile cache'leme
- **Ödeme işleme:** Borç doğrulama → bakiye kontrolü → gateway entegrasyonu → transaction'lı kayıt → cache invalidation → makbuz e-postası
- **Otomatik hatırlatma:** Hangfire ile gece fatura senkronizasyonu, gündüz vadesi yaklaşan faturalar için e-posta gönderimi
- **Dashboard:** Aylık özet, ödenmemiş abonelik listesi, ödeme geçmişi (Redis cache'li)

## AI Kullanımı
- AI kullanımı proje süresince oluşturulmak istenen yapıların ve fonksiyonların teknik olarak detaylı tasviri ve açıklaması ile prompt'lar oluşturularak yapılmıştır. 

- AI çıktıları doğrudan çalıştırılmayıp mevcut yapılar ile tasvir edildiği şekilde etkileşim kurduğundan emin olunduktan sonra edge-case testleri yapılıp mevcut mimariye entegre edilmiştir. 

- Örnek bir prompt örnekteki gibidir:

** örnek prompt 1 **
Kullanıcı ödeme onayına çok hızlı çift tıklarsa veya iki tab'dan aynı anda onay verirse iki request backend'e aynı anda ulaşıp ikisinin de payment gateway'i çağırmaması için PostgreSQL'de advisory key ile lock al. Lock'u, HasSuccessful check'inden ve gateway çağrısından önce al.

** örnek prompt 2**
Sisteme art arda gelebilecek aynı fatura query'lerinin 3rd party API'a atacağı istek yükünü azaltmak için Redis entegrasyonu yap. Kullanıcı bir fatura isteği attığında önce bu aboneliğin sorgusunun Redis'te olup olmadığını kontrol et, Redis'te yoksa 3rd party API isteği oluştur. Redis'teki bu fatıraların TTL'ini de 60sn olarak ayarla.

## Mimari

Clean Architecture (Domain → Application → Infrastructure → Api) prensiplerine göre yapılandırılmıştır.

```
┌─────────────────────────────────────────────────────────────┐
│  Frontend (React + Vite + TanStack Query)                   │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTPS / JWT Bearer
┌──────────────────────────┴──────────────────────────────────┐
│  PayderPay.Api            (Controllers, Middleware, CORS)   │
├─────────────────────────────────────────────────────────────┤
│  PayderPay.Application    (Services, DTOs, Interfaces)      │
├─────────────────────────────────────────────────────────────┤
│  PayderPay.Infrastructure (EF Core, Redis, Hangfire, SMTP)  │
├─────────────────────────────────────────────────────────────┤
│  PayderPay.Domain         (Entities, Enums)                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
       ┌───────────────────┼─────────────────────────┐
       ▼                   ▼                         ▼
  PostgreSQL          Redis Cache         3rd-Party Billing API
                                          (bills + payments)
```

Detaylı diyagramlar için: [`docs/diagrams/`](docs/diagrams/)
- ER diagram, system diagram, deployment diagram
- Sequence diagrams: borç sorgusu, ödeme oluşturma, hatırlatma akışı

## Teknoloji Yığını

**Backend**
- .NET 9 / ASP.NET Core
- Entity Framework Core (PostgreSQL)
- Hangfire (background jobs)
- Redis (caching)
- JWT Bearer + Refresh Token
- xUnit (unit + integration tests)

**Frontend**
- React 19 + TypeScript
- Vite
- TanStack Query (server state)
- React Hook Form + Zod (form validation)
- React Router

**Altyapı**
- PostgreSQL
- Redis
- SMTP (e-posta)

## Proje Yapısı

```
.
├── src/
│   ├── PayderPay.Api/              # HTTP endpoints, middleware
│   ├── PayderPay.Application/      # Business logic, DTOs, services
│   ├── PayderPay.Domain/           # Entities, enums
│   └── PayderPay.Infrastructure/   # EF Core, Redis, Hangfire, SMTP
├── tests/
│   ├── PayderPay.Api.IntegrationTests/
│   └── PayderPay.Application.UnitTests/
├── frontend/                       # React uygulaması
└── docs/diagrams/                  # ER + sequence + architecture diyagramları
```

## Yerel Geliştirme

### Ön gereksinimler
- .NET 9 SDK
- Node.js 20+
- PostgreSQL 15+
- Redis 7+ (opsiyonel, `Redis:Enabled=false` ile devre dışı bırakılabilir)

### Backend

```bash
# Veritabanını ayağa kaldır
cd src/PayderPay.Api
dotnet ef database update --project ../PayderPay.Infrastructure

# API'yi başlat
dotnet run
# → http://localhost:5158
```

### Frontend

```bash
cd frontend
npm install
npm run dev
# → http://localhost:5159
```

### Konfigürasyon

`src/PayderPay.Api/appsettings.json` üzerinden:
- `ConnectionStrings:DefaultConnection` — PostgreSQL bağlantısı
- `Redis:ConnectionString` + `Redis:Enabled`
- `Jwt:SigningKey` — **production'da mutlaka değiştirin**
- `ExternalServices:MockApiBaseUrl` — 3rd-party billing API
- `Smtp:*` — e-posta gönderimi
- `ReminderJob:InvoiceSyncCron` / `NotificationDeliveryCron` — Hangfire cron ifadeleri

Hassas değerler `.env` dosyası ile override edilebilir (`DotEnvLoader` otomatik yükler).

## Test

```bash
dotnet test                                            # tüm testler
dotnet test tests/PayderPay.Application.UnitTests      # unit testler
dotnet test tests/PayderPay.Api.IntegrationTests       # integration testler
```

## Temel Akışlar

| Akış | Açıklama | Diyagram |
|---|---|---|
| **Borç Sorgulama** | Cache → MISS ise 3rd-party'den çek → DB'ye snapshot | [debt-query-sequence](docs/diagrams/debt-query-sequence-diagram.pdf) |
| **Ödeme** | Re-validate (live) → gateway → Tx (deduct + soft-delete + insert) → cache invalidate → makbuz mail | [payment-create-sequence](docs/diagrams/payment-create-sequence-diagram.pdf) |
| **Hatırlatma** | Hangfire gece: fatura senkronizasyonu + kuyruğa al · Gündüz: pending mail'leri gönder | [debt-reminder-sequence](docs/diagrams/debt-reminder-sequence-diagram.pdf) |

## Canlı Erişim

🌐 **[www.veranogardenhome.com](https://www.veranogardenhome.com)**

Uygulama canlı ortamda çalışmaktadır. Test hesabı oluşturabilirsiniz. Ancak 3rd party fatura sağlayıcısındaki faturalar sınırlı olup case sırasında kullanılacağından şu an Abone No'ların güvenliği açısından bu dosyaya eklenememiştir.
