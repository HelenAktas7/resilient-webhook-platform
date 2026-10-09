namespace WebhookPlatform.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard", () => Results.Content(DashboardHtml, "text/html"))
           .ExcludeFromDescription();

        app.MapGet("/", () => Results.Content(DashboardHtml, "text/html"))
           .ExcludeFromDescription();
    }

    private const string DashboardHtml = """
    <!DOCTYPE html>
    <html lang="tr">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>⚡ Resilient Webhook Platform - Canlı Yönetim Paneli</title>
        <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet">
        <style>
            :root {
                --bg: #090d16;
                --card-bg: rgba(18, 24, 38, 0.7);
                --card-border: rgba(255, 255, 255, 0.08);
                --primary: #6366f1;
                --primary-glow: rgba(99, 102, 241, 0.25);
                --success: #10b981;
                --warning: #f59e0b;
                --danger: #ef4444;
                --text-main: #f8fafc;
                --text-muted: #94a3b8;
            }
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body {
                font-family: 'Plus Jakarta Sans', sans-serif;
                background-color: var(--bg);
                color: var(--text-main);
                min-height: 100vh;
                padding: 24px;
                background-image: radial-gradient(circle at 10% 20%, rgba(99, 102, 241, 0.08) 0%, transparent 40%),
                                  radial-gradient(circle at 90% 80%, rgba(16, 185, 129, 0.05) 0%, transparent 40%);
            }
            .container { max-width: 1380px; margin: 0 auto; }
            header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding-bottom: 24px;
                border-bottom: 1px solid var(--card-border);
                margin-bottom: 28px;
            }
            .logo { display: flex; align-items: center; gap: 12px; }
            .logo-icon {
                width: 44px; height: 44px;
                background: linear-gradient(135deg, #6366f1, #a855f7);
                border-radius: 12px;
                display: flex; align-items: center; justify-content: center;
                font-size: 22px;
                box-shadow: 0 0 20px var(--primary-glow);
            }
            .logo h1 { font-size: 20px; font-weight: 800; letter-spacing: -0.5px; }
            .logo p { font-size: 13px; color: var(--text-muted); }
            .badge-live {
                display: flex; align-items: center; gap: 8px;
                background: rgba(16, 185, 129, 0.1);
                color: var(--success);
                padding: 6px 14px;
                border-radius: 20px;
                font-size: 13px;
                font-weight: 600;
                border: 1px solid rgba(16, 185, 129, 0.2);
            }
            .dot { width: 8px; height: 8px; background: var(--success); border-radius: 50%; animation: pulse 1.5s infinite; }
            @keyframes pulse { 0%, 100% { opacity: 1; transform: scale(1); } 50% { opacity: 0.4; transform: scale(1.3); } }

            /* Metrics Grid */
            .metrics-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
                gap: 16px;
                margin-bottom: 28px;
            }
            .metric-card {
                background: var(--card-bg);
                backdrop-filter: blur(12px);
                border: 1px solid var(--card-border);
                border-radius: 16px;
                padding: 18px;
                transition: transform 0.2s, border-color 0.2s;
            }
            .metric-card:hover { transform: translateY(-2px); border-color: rgba(99, 102, 241, 0.3); }
            .metric-title { font-size: 12px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
            .metric-value { font-size: 26px; font-weight: 800; color: var(--text-main); }
            .val-success { color: var(--success); }
            .val-warning { color: var(--warning); }
            .val-danger { color: var(--danger); }
            .val-primary { color: var(--primary); }

            /* Main Grid */
            .main-grid {
                display: grid;
                grid-template-columns: 380px 1fr;
                gap: 24px;
            }
            @media (max-width: 1024px) { .main-grid { grid-template-columns: 1fr; } }

            .panel {
                background: var(--card-bg);
                backdrop-filter: blur(12px);
                border: 1px solid var(--card-border);
                border-radius: 18px;
                padding: 22px;
                margin-bottom: 24px;
            }
            .panel-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 18px;
                padding-bottom: 12px;
                border-bottom: 1px solid var(--card-border);
            }
            .panel-header h2 { font-size: 16px; font-weight: 700; display: flex; align-items: center; gap: 8px; }
            
            /* Forms & Inputs */
            .form-group { margin-bottom: 14px; }
            label { display: block; font-size: 12px; font-weight: 600; color: var(--text-muted); margin-bottom: 6px; }
            select, input, textarea {
                width: 100%;
                background: rgba(10, 14, 23, 0.8);
                border: 1px solid var(--card-border);
                border-radius: 10px;
                padding: 10px 14px;
                color: #fff;
                font-family: inherit;
                font-size: 13px;
                outline: none;
                transition: border-color 0.2s;
            }
            select:focus, input:focus, textarea:focus { border-color: var(--primary); }
            textarea { height: 110px; font-family: monospace; resize: vertical; }

            .btn {
                width: 100%;
                background: linear-gradient(135deg, #6366f1, #4f46e5);
                color: white;
                border: none;
                border-radius: 10px;
                padding: 12px;
                font-weight: 700;
                font-size: 14px;
                cursor: pointer;
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 8px;
                transition: all 0.2s;
                box-shadow: 0 4px 12px var(--primary-glow);
            }
            .btn:hover { opacity: 0.95; transform: translateY(-1px); }
            .btn-secondary {
                background: rgba(255, 255, 255, 0.06);
                border: 1px solid var(--card-border);
                color: var(--text-main);
                box-shadow: none;
                padding: 8px 12px;
                font-size: 12px;
                width: auto;
            }
            .btn-secondary:hover { background: rgba(255, 255, 255, 0.1); }
            .btn-danger {
                background: rgba(239, 68, 68, 0.15);
                border: 1px solid rgba(239, 68, 68, 0.3);
                color: #fca5a5;
                box-shadow: none;
                padding: 6px 10px;
                font-size: 11px;
                width: auto;
                border-radius: 6px;
            }
            .btn-danger:hover { background: rgba(239, 68, 68, 0.25); }

            /* Tables */
            .table-container { overflow-x: auto; }
            table { width: 100%; border-collapse: collapse; font-size: 13px; }
            th { text-align: left; padding: 12px; color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid var(--card-border); }
            td { padding: 14px 12px; border-bottom: 1px solid rgba(255, 255, 255, 0.04); vertical-align: middle; }
            tr:hover td { background: rgba(255, 255, 255, 0.02); }

            .badge {
                display: inline-flex;
                align-items: center;
                gap: 4px;
                padding: 4px 10px;
                border-radius: 12px;
                font-size: 11px;
                font-weight: 700;
            }
            .badge-success { background: rgba(16, 185, 129, 0.15); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
            .badge-failed { background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); }
            .badge-dlq { background: rgba(239, 68, 68, 0.15); color: #f87171; border: 1px solid rgba(239, 68, 68, 0.3); }

            .toast {
                position: fixed; bottom: 24px; right: 24px;
                background: #1e293b; color: #fff;
                padding: 12px 20px; border-radius: 10px;
                border: 1px solid var(--card-border);
                box-shadow: 0 10px 25px rgba(0,0,0,0.5);
                display: none; z-index: 1000;
                font-size: 13px; font-weight: 600;
            }
        </style>
    </head>
    <body>
        <div class="container">
            <header>
                <div class="logo">
                    <div class="logo-icon">⚡</div>
                    <div>
                        <h1>Resilient Webhook Platform</h1>
                        <p>RabbitMQ • MassTransit • Polly v8 • Redis Rate Limiter • PostgreSQL</p>
                    </div>
                </div>
                <div class="badge-live">
                    <div class="dot"></div> Canlı Dağıtım Motoru Aktif
                </div>
            </header>

            <!-- Metrics Cards -->
            <div class="metrics-grid">
                <div class="metric-card">
                    <div class="metric-title">Toplam Olay</div>
                    <div class="metric-value val-primary" id="metricTotalEvents">0</div>
                </div>
                <div class="metric-card">
                    <div class="metric-title">Toplam Deneme</div>
                    <div class="metric-value" id="metricTotalAttempts">0</div>
                </div>
                <div class="metric-card">
                    <div class="metric-title">Başarılı İletim</div>
                    <div class="metric-value val-success" id="metricSuccess">0</div>
                </div>
                <div class="metric-card">
                    <div class="metric-title">Yeniden Denenen</div>
                    <div class="metric-value val-warning" id="metricRetrying">0</div>
                </div>
                <div class="metric-card">
                    <div class="metric-title">Dead-Letter (DLQ)</div>
                    <div class="metric-value val-danger" id="metricDlq">0</div>
                </div>
                <div class="metric-card">
                    <div class="metric-title">Başarı Oranı</div>
                    <div class="metric-value val-success" id="metricSuccessRate">100%</div>
                </div>
                <div class="metric-card">
                    <div class="metric-title">Ort. Süre</div>
                    <div class="metric-value" id="metricAvgDuration">0 ms</div>
                </div>
            </div>

            <!-- Main Layout -->
            <div class="main-grid">
                <!-- Left Sidebar: Event Simulator & Subscriptions -->
                <div>
                    <!-- Simulator Form -->
                    <div class="panel">
                        <div class="panel-header">
                            <h2>🚀 Webhook Tetikleyici (Simulator)</h2>
                        </div>
                        <div class="form-group">
                            <label>Olay Türü (Event Type):</label>
                            <select id="eventTypeSelect" onchange="updatePayloadTemplate()">
                                <option value="order.created">order.created (Yeni Sipariş)</option>
                                <option value="payment.succeeded">payment.succeeded (Ödeme Başarılı)</option>
                                <option value="shipment.delivered">shipment.delivered (Kargo Teslim Edildi)</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Idempotency Key (Tekillik Anahtarı):</label>
                            <input type="text" id="idempotencyInput" value="evt_test_101">
                        </div>
                        <div class="form-group">
                            <label>JSON Yükü (Payload Data):</label>
                            <textarea id="payloadInput"></textarea>
                        </div>
                        <button class="btn" onclick="publishEvent()">
                            ⚡ Olayı Fırlat (Publish Event)
                        </button>
                    </div>

                    <!-- Quick Subscriptions Seed -->
                    <div class="panel">
                        <div class="panel-header">
                            <h2>⚙️ Hızlı Test Aboneleri</h2>
                            <button class="btn btn-secondary" onclick="seedMockSubscriptions()">+ 3 Mock Hedefi Ekle</button>
                        </div>
                        <p style="font-size: 12px; color: var(--text-muted); line-height: 1.5;">
                            Tek tıkla port 5050'deki <strong>200 OK</strong>, <strong>500 Fail (Polly Test)</strong> ve <strong>Flaky</strong> sahte alıcıları sisteme kaydeder.
                        </p>
                    </div>
                </div>

                <!-- Right Area: Delivery Logs & Dead Letters -->
                <div>
                    <!-- Live Logs Table -->
                    <div class="panel">
                        <div class="panel-header">
                            <h2>📊 Canlı Teslimat Geçmişi & Polly Durumları</h2>
                            <button class="btn btn-secondary" onclick="fetchRecentDeliveries()">🔄 Yenile</button>
                        </div>
                        <div class="table-container">
                            <table>
                                <thead>
                                    <tr>
                                        <th>Olay / Hedef</th>
                                        <th>Deneme</th>
                                        <th>Durum</th>
                                        <th>HTTP Kodu</th>
                                        <th>Süre</th>
                                        <th>Tarih</th>
                                    </tr>
                                </thead>
                                <tbody id="deliveriesTableBody">
                                    <tr><td colspan="6" style="text-align: center; color: var(--text-muted);">Henüz bir teslimat yapılmadı. Sol menüden bir olay tetikleyin!</td></tr>
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <!-- DLQ Table -->
                    <div class="panel">
                        <div class="panel-header">
                            <h2>💀 Dead-Letter Queue (DLQ) & Manuel Replay</h2>
                            <button class="btn btn-secondary" onclick="replayAllDlq()">⚡ Tümünü Yeniden Gönder (Replay All)</button>
                        </div>
                        <div class="table-container">
                            <table>
                                <thead>
                                    <tr>
                                        <th>Hedef URL</th>
                                        <th>Deneme #</th>
                                        <th>Hata Nedeni</th>
                                        <th>Tarih</th>
                                        <th>Aksiyon</th>
                                    </tr>
                                </thead>
                                <tbody id="dlqTableBody">
                                    <tr><td colspan="5" style="text-align: center; color: var(--text-muted);">DLQ temiz! Hiçbir mesaj kaybolmadı.</td></tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="toast" class="toast"></div>

        <script>
            const templates = {
                "order.created": JSON.stringify({ orderId: "ORD-9901", customer: "Ahmet Yılmaz", total: 1450.50, currency: "TRY" }, null, 2),
                "payment.succeeded": JSON.stringify({ paymentId: "PAY-5542", orderId: "ORD-9901", amount: 1450.50, cardLast4: "4242" }, null, 2),
                "shipment.delivered": JSON.stringify({ trackingNumber: "YK-889123", carrier: "Yurtiçi Kargo", status: "DELIVERED" }, null, 2)
            };

            function updatePayloadTemplate() {
                const type = document.getElementById('eventTypeSelect').value;
                document.getElementById('payloadInput').value = templates[type] || "{}";
                document.getElementById('idempotencyInput').value = "evt_" + Math.random().toString(36).substring(2, 9);
            }

            function showToast(msg, isSuccess = true) {
                const t = document.getElementById('toast');
                t.innerText = msg;
                t.style.borderLeft = isSuccess ? '4px solid #10b981' : '4px solid #ef4444';
                t.style.display = 'block';
                setTimeout(() => t.style.display = 'none', 3000);
            }

            async function fetchStats() {
                try {
                    const res = await fetch('/api/v1/dlq/stats');
                    if (!res.ok) return;
                    const d = await res.json();
                    document.getElementById('metricTotalEvents').innerText = d.totalEvents;
                    document.getElementById('metricTotalAttempts').innerText = d.totalAttempts;
                    document.getElementById('metricSuccess').innerText = d.successfulDeliveries;
                    document.getElementById('metricRetrying').innerText = d.retryingDeliveries;
                    document.getElementById('metricDlq').innerText = d.deadLetters;
                    document.getElementById('metricSuccessRate').innerText = d.successRatePercentage;
                    document.getElementById('metricAvgDuration').innerText = d.averageResponseTimeMs + " ms";
                } catch (e) {}
            }

            async function fetchRecentDeliveries() {
                try {
                    const res = await fetch('/api/v1/events');
                    if (!res.ok) return;
                    const events = await res.json();
                    if (!events.length) return;

                    let allAttempts = [];
                    for (const ev of events.slice(0, 5)) {
                        const detailRes = await fetch(`/api/v1/events/${ev.id}`);
                        if (detailRes.ok) {
                            const detail = await detailRes.json();
                            if (detail.deliveryAttempts) {
                                detail.deliveryAttempts.forEach(a => allAttempts.push({ ...a, eventType: ev.eventType }));
                            }
                        }
                    }

                    const tbody = document.getElementById('deliveriesTableBody');
                    if (!allAttempts.length) {
                        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: var(--text-muted);">Henüz teslimat denemesi yok.</td></tr>';
                        return;
                    }

                    tbody.innerHTML = allAttempts.map(a => `
                        <tr>
                            <td>
                                <strong>${a.eventType}</strong><br>
                                <span style="font-size: 11px; color: var(--text-muted);">${a.targetUrl}</span>
                            </td>
                            <td>Deneme #${a.attemptNumber}</td>
                            <td>
                                <span class="badge ${a.status === 'Success' ? 'badge-success' : (a.status === 'DeadLettered' ? 'badge-dlq' : 'badge-failed')}">
                                    ${a.status}
                                </span>
                            </td>
                            <td><code>${a.httpStatusCode || 'Timeout'}</code></td>
                            <td>${a.responseTimeMs} ms</td>
                            <td style="font-size: 11px; color: var(--text-muted);">${new Date(a.attemptedAtUtc).toLocaleTimeString()}</td>
                        </tr>
                    `).join('');
                } catch (e) {}
            }

            async function fetchDlq() {
                try {
                    const res = await fetch('/api/v1/dlq');
                    if (!res.ok) return;
                    const list = await res.json();
                    const tbody = document.getElementById('dlqTableBody');
                    if (!list.length) {
                        tbody.innerHTML = '<tr><td colspan="5" style="text-align: center; color: var(--text-muted);">DLQ temiz! Hiçbir mesaj kaybolmadı.</td></tr>';
                        return;
                    }

                    tbody.innerHTML = list.map(item => `
                        <tr>
                            <td><strong>${item.subscriptionName}</strong><br><span style="font-size: 11px; color: var(--text-muted);">${item.targetUrl}</span></td>
                            <td>${item.attemptNumber}</td>
                            <td style="color: #f87171; font-size: 12px;">${item.errorMessage || 'HTTP ' + item.httpStatusCode}</td>
                            <td style="font-size: 11px; color: var(--text-muted);">${new Date(item.attemptedAtUtc).toLocaleTimeString()}</td>
                            <td>
                                <button class="btn btn-danger" onclick="replaySingle('${item.attemptId}')">🔄 Replay</button>
                            </td>
                        </tr>
                    `).join('');
                } catch (e) {}
            }

            async function publishEvent() {
                const eventType = document.getElementById('eventTypeSelect').value;
                const idempotencyKey = document.getElementById('idempotencyInput').value;
                let data = {};
                try {
                    data = JSON.parse(document.getElementById('payloadInput').value);
                } catch (e) {
                    showToast("Geçersiz JSON formatı!", false);
                    return;
                }

                const res = await fetch('/api/v1/events/publish', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ idempotencyKey, eventType, data })
                });

                const resData = await res.json();
                if (res.ok) {
                    showToast(`✅ Olay kabul edildi (${resData.matchedSubscriptionsCount} aboneye fırlatıldı)`);
                    updatePayloadTemplate();
                    setTimeout(() => { fetchStats(); fetchRecentDeliveries(); fetchDlq(); }, 1000);
                } else {
                    showToast(`❌ Hata: ${resData.error || 'İşlenemedi'}`, false);
                }
            }

            async function seedMockSubscriptions() {
                const subs = [
                    { name: "Trendyol Sağlıklı Müşteri", targetUrl: "http://localhost:5050/api/mock/webhook/success", subscribedEventTypes: ["*"], rateLimitPerMinute: 60 },
                    { name: "Çökmüş Müşteri (Polly Test)", targetUrl: "http://localhost:5050/api/mock/webhook/fail", subscribedEventTypes: ["*"], rateLimitPerMinute: 60 },
                    { name: "Kararsız Müşteri (Flaky Receiver)", targetUrl: "http://localhost:5050/api/mock/webhook/flaky", subscribedEventTypes: ["*"], rateLimitPerMinute: 60 }
                ];

                for (const s of subs) {
                    await fetch('/api/v1/subscriptions', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(s)
                    });
                }
                showToast("✅ 3 Test Webhook Abonesi Başarıyla Eklendi!");
            }

            async function replaySingle(attemptId) {
                const res = await fetch(`/api/v1/dlq/${attemptId}/replay`, { method: 'POST' });
                if (res.ok) {
                    showToast("✅ Mesaj yeniden kuyruğa fırlatıldı!");
                    setTimeout(() => { fetchStats(); fetchRecentDeliveries(); fetchDlq(); }, 1000);
                }
            }

            async function replayAllDlq() {
                const res = await fetch('/api/v1/dlq/replay-all', { method: 'POST' });
                const d = await res.json();
                if (res.ok) {
                    showToast(`✅ ${d.count} mesaj topluca yeniden fırlatıldı!`);
                    setTimeout(() => { fetchStats(); fetchRecentDeliveries(); fetchDlq(); }, 1000);
                }
            }

            // Sayfa yüklendiğinde
            updatePayloadTemplate();
            fetchStats();
            fetchRecentDeliveries();
            fetchDlq();

            // Her 3 saniyede bir canlı verileri güncelle
            setInterval(() => {
                fetchStats();
                fetchRecentDeliveries();
                fetchDlq();
            }, 3000);
        </script>
    </body>
    </html>
    """;
}
