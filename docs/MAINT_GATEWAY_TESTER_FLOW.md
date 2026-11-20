# MaintGatewayTester × MaintGateway 流程圖

> 三種模式的 Tester UI 入口與 MaintGateway 內部 endpoint 對照。
> 圖中所有 port 與 endpoint 皆來自 `appsettings.json` + `Program.cs` source code。

---

## 1. HTTP 模式（明文，無 token）

- **Tester UI listen**：`http://localhost:5000`（Kestrel 設定，與目標 gateway 同 port）
- **Tester → Gateway**：`POST /api/interchange` (http://localhost:5000)
- **目標 Gateway**：`MaintGateway`
- **NCCC mock**：14801

```
 Browser / curl               Tester UI               MaintGateway          NCCC mock
     │                            │                       │                     │
     │  GET  http://localhost:5000/                       │                     │
     ├───────────────────────────▶│  (網頁: 填 body, 按 Send) │                     │
     │                            │                       │                     │
     │  POST http://localhost:5000/                       │                     │
     │    /api/test/send-xml      │                       │                     │
     ├───────────────────────────▶│                       │                     │
     │                            │  POST api/interchange │                     │
     │                            │  http://localhost:5000│                     │
     │                            ├──────────────────────▶│                     │
     │                            │                       │  ISO8583 0420       │
     │                            │                       ├────────────────────▶│
     │                            │                       │                     │
     │                            │                       │  ISO8583 0430       │
     │                            │                       │◀────────────────────┤
     │                            │  200 OK (ISO8583)     │                     │
     │                            │◀──────────────────────┤                     │
     │  200 OK + ISO8583 response │                       │                     │
     │◀───────────────────────────┤                       │                     │
```

---

## 2. HTTPS 模式（TLS，無 token）

- **Tester UI listen**：`http://localhost:5102`（Program.cs line 53 `app.Urls.Add`）
- **Tester → Gateway**：`POST /api/interchange` (https://localhost:5101)
- **目標 Gateway**：`MaintGatewayHttps`（listen `https://0.0.0.0:5101`，另有 `http://0.0.0.0:5100` 備援）
- **NCCC mock**：14801

```
 Browser / curl             Tester UI                MaintGatewayHttps      NCCC mock
     │                          │                            │                 │
     │  GET  http://localhost:5102/                          │                 │
     ├─────────────────────────▶│  (網頁: 填 body, 按 Send)   │                 │
     │                          │                            │                 │
     │  GET  http://localhost:5102/                          │                 │
     │    /api/test/send-xml    │                            │                 │
     ├─────────────────────────▶│                            │                 │
     │                          │  POST api/interchange      │                 │
     │                          │  https://localhost:5101    │                 │
     │                          ├───────────────────────────▶│                 │
     │                          │  (TLS: 信任 self-signed)   │                 │
     │                          │                            │  ISO8583 0420   │
     │                          │                            ├────────────────▶│
     │                          │                            │                 │
     │                          │                            │  ISO8583 0430   │
     │                          │                            │◀────────────────┤
     │                          │  HTTPS 200 (ISO8583)       │                 │
     │                          │◀───────────────────────────┤                 │
     │  200 OK + ISO8583        │                            │                 │
     │◀─────────────────────────┤                            │                 │
```

---

## 3. JWT 模式（TLS + Bearer token）

- **Tester UI listen**：`http://localhost:5104`（Program.cs `app.Urls.Add`）
- **Token 取得**：`POST /api/token` (https://localhost:5103)
- **Tester → Gateway**：`POST /api/interchange` (https://localhost:5103)
- **目標 Gateway**：`MaintGatewayJwt`（listen `https://0.0.0.0:5103`，另有 `http://0.0.0.0:5102` 備援）
- **NCCC mock**：14801

```
 Browser / curl             Tester UI                MaintGatewayJwt       NCCC mock
     │                          │                            │                 │
     │  GET  http://localhost:5104/                          │                 │
     ├─────────────────────────▶│  (網頁: 填 body, 按 Send)   │                 │
     │                          │                            │                 │
     │                          │  1) POST /api/token        │                 │
     │                          │     {Username, Password}   │                 │
     │                          ├───────────────────────────▶│                 │
     │                          │  2) { token: "eyJ..." }    │                 │
     │                          │◀───────────────────────────┤                 │
     │                          │                            │                 │
     │  GET  http://localhost:5104/                          │                 │
     │    /api/test/send-xml    │                            │                 │
     ├─────────────────────────▶│                            │                 │
     │                          │  3) POST /api/interchange  │                 │
     │                          │     Authorization: Bearer  │                 │
     │                          │     <eyJ...>               │                 │
     │                          ├───────────────────────────▶│                 │
     │                          │  (TLS + JWT verify)        │                 │
     │                          │                            │  ISO8583 0420   │
     │                          │                            ├────────────────▶│
     │                          │                            │                 │
     │                          │                            │  ISO8583 0430   │
     │                          │                            │◀────────────────┤
     │                          │  HTTPS 200 (ISO8583)       │                 │
     │                          │◀───────────────────────────┤                 │
     │  200 OK + ISO8583        │                            │                 │
     │◀─────────────────────────┤                            │                 │
```

---

## 對照表

| 模式 | Tester UI 入口 | MaintGateway 內部 endpoint | NCCC mock | 認證 |
|------|----------------|---------------------------|-----------|------|
| HTTP  | `http://localhost:5000/api/test/send-xml` | `http://localhost:5000/api/interchange` (`MaintGateway`) | 14801 | 無 |
| HTTPS | `http://localhost:5102/api/test/send-xml` | `https://localhost:5101/api/interchange` (`MaintGatewayHttps`) | 14801 | TLS（信任 self-signed）|
| JWT   | `http://localhost:5104/api/test/send-xml` | `https://localhost:5103/api/interchange` (`MaintGatewayJwt`) | 14801 | TLS + Bearer token |

> 注意：Tester UI 自身是 HTTP（明文），用瀏覽器手動測試方便。TLS 只發生在 Tester → MaintGateway 之間。

## WpfStressGatewayTester（直接打 gateway，不走 Tester UI）

WPF stress tester 略過 Tester UI，**直接**呼叫各個 MaintGateway 的 `/api/interchange`（已驗證三模式 OK）：

| 模式 | 目標 endpoint |
|------|--------------|
| Stress HTTP  | `http://localhost:5000/api/interchange` |
| Stress HTTPS | `https://localhost:5101/api/interchange` |
| Stress JWT   | `POST https://localhost:5103/api/token` → `POST https://localhost:5103/api/interchange` |
