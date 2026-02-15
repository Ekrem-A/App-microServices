# OpenTelemetry + ELK Stack Observability Setup

Bu dokümantasyon, Identity API projesinin OpenTelemetry ve ELK Stack (Elasticsearch, Kibana) ile nasýl izleneceðini açýklar.

## ??? Mimari

```
????????????????????     ???????????????????????     ??????????????????
?   Identity API   ??????? OpenTelemetry       ??????? Elasticsearch  ?
?   (Traces,       ?     ? Collector           ?     ?                ?
?    Metrics,      ?     ? (OTLP Receiver)     ?     ??????????????????
?    Logs)         ?     ???????????????????????              ?
?                  ?                                           ?
?   Serilog ??????????????????????????????????????????? ??????????????????
?   (Direct Logs)  ?                                    ?    Kibana      ?
????????????????????                                    ?  (Dashboard)   ?
                                                        ??????????????????
```

## ?? Baþlatma

### Docker Compose ile Baþlatma

```bash
cd Idendity.Api
docker-compose up -d
```

### Servisler ve Portlar

| Servis | Port | Açýklama |
|--------|------|----------|
| Identity API | 5002 | Ana uygulama |
| Elasticsearch | 9200 | Veri depolama |
| Kibana | 5601 | Görselleþtirme |
| OTEL Collector | 4317 | gRPC OTLP receiver |
| OTEL Collector | 4318 | HTTP OTLP receiver |
| APM Server | 8200 | Elastic APM |
| Prometheus Metrics | 8889 | Metric exporter |

## ?? Kibana Yapýlandýrmasý

### 1. Kibana'ya Eriþim

Tarayýcýnýzda `http://localhost:5601` adresine gidin.

### 2. Index Pattern Oluþturma

1. **Management** ? **Stack Management** ? **Index Patterns**
2. **Create index pattern** butonuna týklayýn
3. Aþaðýdaki pattern'leri oluþturun:

#### Logs Index Pattern
- **Name**: `identity-api-logs-*`
- **Timestamp field**: `@timestamp`

#### Traces Index Pattern
- **Name**: `otel-traces-*`
- **Timestamp field**: `@timestamp`

### 3. Discover ile Log Ýnceleme

1. **Analytics** ? **Discover**
2. `identity-api-logs-*` index pattern'ini seçin
3. Zaman aralýðýný ayarlayýn
4. Loglarý filtreleyebilirsiniz:
   - `level: "Error"` - Sadece hatalarý göster
   - `Application: "IdentityService"` - Servis bazlý filtreleme
   - `RequestPath: "/api/auth/*"` - Endpoint bazlý filtreleme

### 4. Dashboard Oluþturma

#### Log Dashboard
1. **Analytics** ? **Dashboard** ? **Create dashboard**
2. **Add panel** ? **Aggregation based** ? **Vertical Bar**
3. Yapýlandýrma:
   - **Metrics**: Count
   - **Buckets**: Date Histogram on `@timestamp`
   - **Split Series**: Terms on `level.keyword`

#### Örnek Visualizations

**HTTP Request Sayýsý (Zaman Serisi)**
```json
{
  "aggs": {
    "requests_over_time": {
      "date_histogram": {
        "field": "@timestamp",
        "fixed_interval": "1m"
      }
    }
  }
}
```

**Hata Oraný**
```json
{
  "query": {
    "bool": {
      "must": [
        { "term": { "level.keyword": "Error" } }
      ]
    }
  }
}
```

**En Çok Çaðrýlan Endpoint'ler**
```json
{
  "aggs": {
    "endpoints": {
      "terms": {
        "field": "RequestPath.keyword",
        "size": 10
      }
    }
  }
}
```

## ?? Metrics

### Prometheus Metrics

OpenTelemetry Collector, metrics'leri Prometheus formatýnda `http://localhost:8889/metrics` adresinde expose eder.

### Önemli Metrikler

- `http_server_request_duration_seconds` - HTTP request süreleri
- `http_server_active_requests` - Aktif request sayýsý
- `process_runtime_dotnet_gc_collections_count` - GC koleksiyon sayýsý
- `process_runtime_dotnet_gc_heap_size_bytes` - Heap boyutu

## ?? Traces (APM)

### Elastic APM Dashboard

1. Kibana'da **Observability** ? **APM** bölümüne gidin
2. **IdentityApi** servisini seçin
3. Görüntüleyebilecekleriniz:
   - Transaction'lar ve süreleri
   - Span breakdown
   - Error rate
   - Throughput

### Distributed Tracing

Her HTTP isteði için trace ID oluþturulur ve þunlarý içerir:
- HTTP request/response detaylarý
- Entity Framework Core sorgularý
- External HTTP çaðrýlarý
- Custom spans

## ?? Yapýlandýrma

### Environment Variables

```bash
# OpenTelemetry
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317

# Elasticsearch
ElasticSearch__Uri=http://elasticsearch:9200

# OpenTelemetry Options
OpenTelemetryOption__ServiceName=IdentityApi
OpenTelemetryOption__ServiceVersion=1.0.0
OpenTelemetryOption__ActivitySourceName=IdentityApi.ActivitySource
```

### appsettings.json

```json
{
  "OpenTelemetryOption": {
    "ServiceName": "IdentityApi",
    "ServiceVersion": "1.0.0",
    "ActivitySourceName": "IdentityApi.ActivitySource"
  },
  "ElasticSearch": {
    "Uri": "http://localhost:9200"
  }
}
```

## ?? Custom Logging Örnekleri

### Controller'da Structured Logging

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    using var activity = ActivitySourceProvider.Source.StartActivity("UserLogin");
    activity?.SetTag("user.email", request.Email);
    
    Log.Information("Login attempt for user {Email}", request.Email);
    
    var result = await _authService.LoginAsync(request);
    
    if (result.IsSuccess)
    {
        Log.Information("User {Email} logged in successfully", request.Email);
        activity?.SetTag("login.success", true);
    }
    else
    {
        Log.Warning("Login failed for user {Email}: {Error}", request.Email, result.Error);
        activity?.SetTag("login.success", false);
        activity?.SetTag("login.error", result.Error);
    }
    
    return Ok(result);
}
```

### Custom Metrics

```csharp
using System.Diagnostics.Metrics;

public class AuthMetrics
{
    private readonly Counter<int> _loginAttempts;
    private readonly Counter<int> _loginSuccesses;
    private readonly Counter<int> _loginFailures;

    public AuthMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("IdentityApi.Auth");
        _loginAttempts = meter.CreateCounter<int>("auth.login.attempts");
        _loginSuccesses = meter.CreateCounter<int>("auth.login.successes");
        _loginFailures = meter.CreateCounter<int>("auth.login.failures");
    }

    public void RecordLoginAttempt() => _loginAttempts.Add(1);
    public void RecordLoginSuccess() => _loginSuccesses.Add(1);
    public void RecordLoginFailure() => _loginFailures.Add(1);
}
```

## ?? Troubleshooting

### Elasticsearch'e Baðlanamýyor

```bash
# Elasticsearch durumunu kontrol et
curl http://localhost:9200/_cluster/health

# Container loglarýný kontrol et
docker logs elasticsearch
```

### OpenTelemetry Collector Çalýþmýyor

```bash
# Collector health check
curl http://localhost:13133/

# Collector loglarý
docker logs otel-collector
```

### Loglar Kibana'da Görünmüyor

1. Index pattern doðru oluþturulmuþ mu kontrol edin
2. Elasticsearch'te index'i kontrol edin:
   ```bash
   curl http://localhost:9200/_cat/indices
   ```
3. Zaman aralýðýný geniþletin (son 7 gün vb.)

## ?? Kaynaklar

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog Elasticsearch Sink](https://github.com/serilog-contrib/serilog-sinks-elasticsearch)
- [Elastic APM .NET Agent](https://www.elastic.co/guide/en/apm/agent/dotnet/current/index.html)
- [Kibana User Guide](https://www.elastic.co/guide/en/kibana/current/index.html)
