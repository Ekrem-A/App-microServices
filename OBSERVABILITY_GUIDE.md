# 🔍 Servislerinizi İzleme Rehberi

## ✅ Çalışan Konteynerler
```
✓ Elasticsearch:9200  - Log ve trace depolama
✓ Kibana:5601         - Trace/log görselleştirme
✓ Prometheus:9090     - Metrics toplama
✓ Grafana:3000        - Metrics görselleştirme
✓ OTEL Collector:4317 - Telemetry hub
```

## 📊 1. GRAFANA - Metrics Görüntüleme

### Adım 1: Grafana'ya Giriş
1. Tarayıcıda aç: **http://localhost:3000**
2. Kullanıcı adı: `admin`
3. Şifre: `admin`
4. (İsterseniz şifreyi değiştirin veya Skip)

### Adım 2: Prometheus Datasource Ekle
1. Sol menüden **⚙️ Configuration** → **Data sources**
2. **Add data source** tıkla
3. **Prometheus** seç
4. URL: `http://prometheus:9090`
5. **Save & test** tıkla (✓ yeşil işaret görmelisiniz)

### Adım 3: Dashboard Oluştur
1. Sol menüden **+** → **Create** → **Dashboard**
2. **Add visualization** tıkla
3. Prometheus datasource'u seç
4. **Metric** alanına şunları yazın:

#### Örnek Queries:

**HTTP İstekleri (Gateway/Catalog/Order/Cart)**
```promql
rate(otel_http_server_request_duration_seconds_count[5m])
```

**.NET GC Heap Kullanımı**
```promql
otel_process_runtime_dotnet_gc_heap_size_bytes
```

**Aktif HTTP Bağlantıları**
```promql
otel_http_server_active_requests
```

**Thread Pool Kuyruğu**
```promql
otel_process_runtime_dotnet_thread_pool_queue_length
```

**HTTP Request Duration (p95)**
```promql
histogram_quantile(0.95, rate(otel_http_server_request_duration_seconds_bucket[5m]))
```

5. **Apply** tıkla
6. Dashboard'u kaydet

---

## 🔍 2. KIBANA - Traces & Logs Görüntüleme

### Adım 1: Kibana'ya Giriş
1. Tarayıcıda aç: **http://localhost:5601**

### Adım 2: Data View Oluştur
1. Sol menüden **☰** → **Stack Management** → **Data Views**
2. **Create data view** tıkla
3. **Name**: `Traces`
4. **Index pattern**: `traces-otel*`
5. **Timestamp field**: `@timestamp`
6. **Save data view to Kibana**

### Adım 3: Traces'leri Görüntüle
1. Sol menüden **☰** → **Analytics** → **Discover**
2. Üstteki dropdown'dan **Traces** data view'ı seç
3. Soldaki field listesinden şunları ekleyin:
   - `Name` (Span adı: örn. "GET /api/products")
   - `Duration` (İşlem süresi microseconds)
   - `Resource.service.name` (Hangi servis: Order.Api, Cart.Api, vb.)
   - `Attributes.http.request.method` (HTTP metodu)
   - `Attributes.http.response.status_code` (Response kodu)

4. **Filtreleme örnekleri**:
   - Sadece hatalı istekler: `Attributes.http.response.status_code >= 400`
   - Yavaş istekler: `Duration > 1000000` (1 saniye = 1M microsecond)
   - Belirli servis: `Resource.service.name : "Order.Api"`

---

## 📈 3. PROMETHEUS UI - Ham Metrics

### Prometheus'a Giriş
1. Tarayıcıda aç: **http://localhost:9090**
2. **Graph** tab'ine tıkla
3. Query örnekleri yukarıdaki Grafana bölümünde

---

## 🚀 Hangi Servisler Veri Gönderiyor?

Kontrol etmek için Prometheus'ta:
```promql
otel_target_info
```

Bu query size şunları gösterecek:
- **Order.Api** - Sipariş servisi
- **Cart.Api** - Sepet servisi
- **Catalog.Api** - Ürün kataloğu
- **Gateway.Api** - API Gateway

---

## 🎯 Hızlı Başlangıç İçin:

### Grafana'da En Kullanışlı Panel:
**Servis başına HTTP isteği/saniye:**
```promql
sum by(job) (rate(otel_http_server_request_duration_seconds_count[5m]))
```

**Legend formatı:** `{{job}}`

Bu size Gateway, Catalog, Order, Cart servislerine gelen istekleri gösterecek.

---

## 🔧 Sorun Giderme

### "No data" görüyorsanız:
1. Konteynerler çalışıyor mu: `docker ps`
2. OTEL Collector logları: `docker logs otel-collector`
3. Prometheus targets sağlıklı mı: http://localhost:9090/targets

### Kibana'da veri yoksa:
1. Elasticsearch indices: `curl http://localhost:9200/_cat/indices?v`
2. Trace sayısı: `curl http://localhost:9200/traces-otel/_count`

---

## 📝 Notlar

- **Metrics**: 15 saniyede bir toplanır (Prometheus scrape interval)
- **Traces**: Gerçek zamanlıdır
- **Retention**: Prometheus 15 gün, Elasticsearch sınırsız (disk dolana kadar)
- **Grafana varsayılan şifresini değiştirin**: http://localhost:3000/profile/password
