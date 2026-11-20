document.addEventListener('DOMContentLoaded', () => {
    // Clock
    const clockEl = document.getElementById("clock");
    if (clockEl) {
        function updateClock() {
            const now = new Date();
            const yyyy = now.getFullYear();
            const mm = String(now.getMonth() + 1).padStart(2, "0");
            const dd = String(now.getDate()).padStart(2, "0");
            const hh = String(now.getHours()).padStart(2, "0");
            const mi = String(now.getMinutes()).padStart(2, "0");
            const ss = String(now.getSeconds()).padStart(2, "0");
            clockEl.textContent = `${yyyy}-${mm}-${dd} ${hh}:${mi}:${ss}`;
        }
        updateClock();
        setInterval(updateClock, 1000);
    }

    // UI Elements
    const statusDot = document.getElementById('statusDot');
    const statusText = document.getElementById('statusText');
    const valMaxConnections = document.getElementById('valMaxConnections');
    const valProcessed = document.getElementById('valProcessed');
    const valClock = document.getElementById('valClock');

    const inputPort = document.getElementById('inputPort');
    const inputConnections = document.getElementById('inputConnections');
    const inputBufferSize = document.getElementById('inputBufferSize');

    const btnStart = document.getElementById('btnStart');
    const btnStop = document.getElementById('btnStop');
    const btnResetMetrics = document.getElementById('btnResetMetrics');
    const btnCloseApp = document.getElementById('btnCloseApp');
    const btnClearLogs = document.getElementById('btnClearLogs');
    const logConsole = document.getElementById('logConsole');

    // State variables
    let logSource = null;
    let telemetryIntervalId = null;

    // Start Clock
    setInterval(updateClock, 1000);
    updateClock();

    // Init Status Polling
    fetchStatus();
    telemetryIntervalId = setInterval(fetchStatus, 1000);

    // Event Bindings
    btnStart.addEventListener('click', startService);
    btnStop.addEventListener('click', stopService);
    btnResetMetrics.addEventListener('click', resetMetrics);
    btnCloseApp.addEventListener('click', closeApplication);
    btnClearLogs.addEventListener('click', () => {
        logConsole.innerHTML = '<div class="system-line">[SYSTEM] Console logs cleared.</div>';
    });

    function updateClock() {
        const now = new Date();
        const yyyy = now.getFullYear();
        const mm = String(now.getMonth() + 1).padStart(2, '0');
        const dd = String(now.getDate()).padStart(2, '0');
        const hh = String(now.getHours()).padStart(2, '0');
        const min = String(now.getMinutes()).padStart(2, '0');
        const ss = String(now.getSeconds()).padStart(2, '0');
        valClock.textContent = `${yyyy}-${mm}-${dd} ${hh}:${min}:${ss}`;
    }

    function fetchStatus() {
        fetch('/api/status')
            .then(res => res.json())
            .then(data => {
                updateUI(data.isRunning, data);
                if (data.isRunning) {
                    connectLogsStream();
                } else {
                    disconnectLogsStream();
                }
            })
            .catch(err => {
                appendLog(`[ERROR] 無法連線至伺服器: ${err.message}`, 'error');
            });
    }

    function updateUI(isRunning, data = {}) {
        valProcessed.textContent = (data.processedCount !== undefined ? data.processedCount : 0).toLocaleString();
        valMaxConnections.textContent = data.maxConnection !== undefined ? data.maxConnection : 0;

        if (isRunning) {
            statusDot.className = 'status-dot status-online';
            statusText.textContent = '監聽中';
            btnStart.disabled = true;
            btnStop.disabled = false;
            
            // Lock inputs while running
            inputPort.disabled = true;
            inputConnections.disabled = true;
            inputBufferSize.disabled = true;

            if (data.port) inputPort.value = data.port;
            if (data.maxConnection) inputConnections.value = data.maxConnection;
            if (data.bufferSize) inputBufferSize.value = data.bufferSize;
        } else {
            statusDot.className = 'status-dot status-offline';
            statusText.textContent = '停止中';
            btnStart.disabled = false;
            btnStop.disabled = true;
            
            // Enable inputs
            inputPort.disabled = false;
            inputConnections.disabled = false;
            inputBufferSize.disabled = false;
        }
    }

    function startService() {
        btnStart.disabled = true;
        appendLog('[SYSTEM] 正在啟動簡訊模擬監聽服務...', 'system');

        const reqBody = {
            port: parseInt(inputPort.value),
            maxConnection: parseInt(inputConnections.value),
            bufferSize: parseInt(inputBufferSize.value)
        };

        fetch('/api/start', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(reqBody)
        })
            .then(res => {
                if (!res.ok) return res.json().then(e => { throw new Error(e.error); });
                return res.json();
            })
            .then(data => {
                appendLog(`[SYSTEM] 簡訊模擬監聽服務已啟動。監聽埠: ${reqBody.port}`, 'success');
                fetchStatus();
            })
            .catch(err => {
                appendLog(`[ERROR] 啟動失敗: ${err.message}`, 'error');
                btnStart.disabled = false;
            });
    }

    function stopService() {
        btnStop.disabled = true;
        appendLog('[SYSTEM] 正在停止簡訊模擬服務...', 'system');

        fetch('/api/stop', { method: 'POST' })
            .then(res => {
                if (!res.ok) throw new Error('停止失敗');
                appendLog('[SYSTEM] 服務已停止。', 'warn');
                fetchStatus();
            })
            .catch(err => {
                appendLog(`[ERROR] 停止失敗: ${err.message}`, 'error');
                btnStop.disabled = false;
            });
    }

    function resetMetrics() {
        if (!confirm('您確定要重設簡訊處理計數器嗎？')) return;
        fetch('/api/reset-metrics', { method: 'POST' })
            .then(() => {
                appendLog('[SYSTEM] 簡訊統計數據已重設。', 'system');
                fetchStatus();
            });
    }

    function closeApplication() {
        if (!confirm("您確定要關閉簡訊模擬器，並完全釋放監聽通訊埠等所有系統資源嗎？")) {
            return;
        }

        btnCloseApp.disabled = true;
        appendLog('[SYSTEM] 正在終止伺服器進程並釋放資源...', 'system');

        fetch('/api/close', { method: 'POST' })
            .then(() => {
                appendLog('[SYSTEM] 伺服器已關閉。', 'success');
                cleanupAndShowOverlay();
            })
            .catch(() => {
                cleanupAndShowOverlay();
            });
    }

    function cleanupAndShowOverlay() {
        clearInterval(telemetryIntervalId);
        disconnectLogsStream();
        updateUI(false);
        
        const overlay = document.createElement('div');
        overlay.className = 'shutdown-overlay';
        overlay.innerHTML = `
            <div class="shutdown-content">
                <svg class="shutdown-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2" />
                </svg>
                <h3>簡訊模擬器已安全結束</h3>
                <p>後端監聽進程已安全關閉，且監聽通訊埠與 Socket 資源已完整釋放。<br><br>現在您可以安全地關閉此瀏覽器頁籤。</p>
            </div>
        `;
        document.body.appendChild(overlay);
    }

    function connectLogsStream() {
        if (logSource) return;
        logSource = new EventSource('/api/logs');
        logSource.onmessage = (event) => {
            appendLog(event.data);
        };
        logSource.onerror = () => {
            disconnectLogsStream();
        };
    }

    function disconnectLogsStream() {
        if (!logSource) return;
        logSource.close();
        logSource = null;
    }

    function appendLog(message, type = '') {
        const line = document.createElement('div');
        
        let formattedMsg = message;
        const hasTimestamp = /^\[\d{2}:\d{2}:\d{2}/.test(message);
        if (!hasTimestamp) {
            const now = new Date();
            const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:${String(now.getSeconds()).padStart(2, '0')}.${String(now.getMilliseconds()).padStart(3, '0')}`;
            formattedMsg = `[${timeStr}] ${message}`;
        }

        // CSS coloring class based on log content
        if (type) {
            line.className = `${type}-line`;
        } else if (message.includes('[ERROR]') || message.includes('[FATAL]')) {
            line.className = 'error-line';
        } else if (message.includes('[WARN]')) {
            line.className = 'warn-line';
        } else if (message.includes('Receive:') || message.includes('Send:')) {
            line.className = 'success-line';
        } else {
            line.className = 'system-line';
        }

        line.textContent = formattedMsg;
        logConsole.appendChild(line);

        // Keep scroll at bottom
        logConsole.scrollTop = logConsole.scrollHeight;

        // Limit console log size to 1000 lines
        while (logConsole.childElementCount > 1000) {
            logConsole.removeChild(logConsole.firstChild);
        }
    }
});
