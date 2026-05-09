const HARDWARE_API_URL = '/api/equipo/current';
const SECURITY_API_URL = '/api/security/current';
const PERFORMANCE_API_URL = '/api/performance/current';
const NAVIGATION_API_URL = '/api/navigation/current?limit=2000';
const REFRESH_INTERVAL_MS = 15000;
const CACHE_KEYS = {
    hardware: 'simpe.cache.hardware',
    security: 'simpe.cache.security',
    performance: 'simpe.cache.performance',
    navigation: 'simpe.cache.navigation'
};

let currentView = 'hardware';
const viewCache = {
    hardware: readCachedView('hardware'),
    security: readCachedView('security'),
    performance: readCachedView('performance'),
    navigation: readCachedView('navigation')
};
let navigationFilters = {
    browser: 'all',
    query: ''
};

const icons = {
    monitor: '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8"/><path d="M12 16v4"/>',
    user: '<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>',
    server: '<rect x="2" y="2" width="20" height="8" rx="2" ry="2"/><rect x="2" y="14" width="20" height="8" rx="2" ry="2"/><line x1="6" y1="6" x2="6" y2="6"/><line x1="6" y1="18" x2="6" y2="18"/>',
    cpu: '<rect x="4" y="4" width="16" height="16" rx="2" ry="2"/><rect x="9" y="9" width="6" height="6"/><line x1="9" y1="1" x2="9" y2="4"/><line x1="15" y1="1" x2="15" y2="4"/><line x1="9" y1="20" x2="9" y2="23"/><line x1="15" y1="20" x2="15" y2="23"/><line x1="20" y1="9" x2="23" y2="9"/><line x1="20" y1="14" x2="23" y2="14"/><line x1="1" y1="9" x2="4" y2="9"/><line x1="1" y1="14" x2="4" y2="14"/>',
    memory: '<rect x="2" y="4" width="20" height="16" rx="2" ry="2"/><path d="M12 12V20"/><path d="M8 12V20"/><path d="M16 12V20"/><path d="M22 10H2"/><path d="M12 4v4"/><path d="M8 4v4"/><path d="M16 4v4"/>',
    disk: '<ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/>',
    window: '<rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><line x1="3" y1="9" x2="21" y2="9"/><line x1="9" y1="21" x2="9" y2="9"/>',
    shield: '<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>',
    clock: '<circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>',
    account: '<circle cx="12" cy="8" r="4"/><path d="M6 21v-2a6 6 0 0 1 12 0v2"/><path d="M17 11l2 2 4-4"/>',
    wifi: '<path d="M5 13a10 10 0 0 1 14 0"/><path d="M8.5 16.5a5 5 0 0 1 7 0"/><path d="M2 9a15 15 0 0 1 20 0"/><line x1="12" y1="20" x2="12.01" y2="20"/>',
    browser: '<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M3 9h18"/><path d="M8 4v5"/>',
    chip: '<rect x="7" y="7" width="10" height="10" rx="1"/><path d="M4 10h3"/><path d="M4 14h3"/><path d="M17 10h3"/><path d="M17 14h3"/><path d="M10 4v3"/><path d="M14 4v3"/><path d="M10 17v3"/><path d="M14 17v3"/>',
    history: '<path d="M3 12a9 9 0 1 0 3-6.7"/><path d="M3 4v5h5"/><path d="M12 7v5l3 2"/>',
    lock: '<rect x="3" y="11" width="18" height="10" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>',
    activity: '<polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>',
    thermometer: '<path d="M14 14.76V3.5a2.5 2.5 0 0 0-5 0v11.26a4.5 4.5 0 1 0 5 0z"/>',
    gauge: '<path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z"/><path d="M19.4 15a8 8 0 1 0-14.8 0"/>',
    database: '<ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/>',
    network: '<line x1="22" y1="12" x2="2" y2="12"/><path d="M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/><line x1="6" y1="16" x2="6.01" y2="16"/><line x1="10" y1="16" x2="10.01" y2="16"/>',
    arrowDown: '<line x1="12" y1="5" x2="12" y2="19"/><polyline points="19 12 12 19 5 12"/>',
    arrowUp: '<line x1="12" y1="19" x2="12" y2="5"/><polyline points="5 12 12 5 19 12"/>',
    layers: '<polygon points="12 2 2 7 12 12 22 7 12 2"/><polyline points="2 17 12 22 22 17"/><polyline points="2 12 12 17 22 12"/>'
};

const cardDataMapping = [
    { key: 'nombre', title: 'Equipo', icon: icons.monitor },
    { key: 'usuario', title: 'Usuario Actual', icon: icons.user },
    { key: 'ip', title: 'Direccion IP', icon: icons.server },
    { key: 'cpu_model', title: 'Procesador', icon: icons.cpu },
    { key: 'ram_total', title: 'Memoria RAM', icon: icons.memory },
    { key: 'disco_tipo', title: 'Almacenamiento', icon: icons.disk },
    { key: 'os_version', title: 'Sistema Operativo', icon: icons.window },
    { key: 'antivirus_nombre', title: 'Seguridad (Antivirus)', icon: icons.shield },
    { key: 'tiempo_arranque', title: 'Ultimo Inicio', icon: icons.clock }
];

const securityIconById = {
    'antivirus-threats': icons.shield,
    'account-protection': icons.account,
    'firewall-network': icons.wifi,
    'app-browser-control': icons.browser,
    'device-security': icons.monitor,
    'protection-history': icons.history,
    'core-isolation': icons.cpu,
    'security-processor': icons.chip,
    'secure-boot': icons.shield,
    'data-encryption': icons.lock
};

async function fetchStats(options = {}) {
    const showLoading = options.showLoading ?? !viewCache.hardware;

    if (currentView === 'security') {
        await fetchSecurity(options);
        return;
    }
    if (currentView === 'performance') {
        await fetchPerformance(options);
        return;
    }
    if (currentView === 'navigation') {
        await fetchNavigation(options);
        return;
    }

    const statusText = document.getElementById('last-update');
    const mainContainer = document.getElementById('dashboard-cards');
    try {
        statusText.innerText = viewCache.hardware ? 'Actualizando inventario...' : 'Cargando inventario...';
        if (showLoading) {
            mainContainer.innerHTML = '<div class="loading">Cargando datos del equipo...</div>';
        }

        const response = await fetch(HARDWARE_API_URL);

        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status}`);
        }

        const data = await response.json();
        cacheView('hardware', data);

        if (currentView === 'hardware') {
            renderDashboard(data);
            statusText.innerText = `Sincronizado: ${new Date().toLocaleTimeString()}`;
        }
    } catch (error) {
        console.error('Error fetching hardware stats:', error);
        statusText.innerText = 'Error al conectar con el agente local';
        if (!viewCache.hardware) {
            mainContainer.innerHTML = '<div class="loading">No fue posible cargar el inventario.</div>';
        }
    }
}

async function fetchSecurity(options = {}) {
    const showLoading = options.showLoading ?? !viewCache.security;
    const statusText = document.getElementById('last-update');
    const mainContainer = document.getElementById('dashboard-cards');

    try {
        statusText.innerText = viewCache.security ? 'Actualizando seguridad...' : 'Escaneando seguridad...';
        if (showLoading) {
            mainContainer.innerHTML = '<div class="loading">Analizando seguridad de Windows...</div>';
        }

        const response = await fetch(SECURITY_API_URL);
        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status}`);
        }

        const data = await response.json();
        cacheView('security', data);

        if (currentView === 'security') {
            renderSecurity(data);
            statusText.innerText = `Seguridad: ${statusLabel(data.overallStatus)} - ${data.generatedAt}`;
        }
    } catch (error) {
        console.error('Error fetching security stats:', error);
        statusText.innerText = 'Error al escanear seguridad';
        if (!viewCache.security) {
            mainContainer.innerHTML = '<div class="loading">No fue posible cargar el escaneo de seguridad.</div>';
        }
    }
}

async function fetchPerformance(options = {}) {
    const showLoading = options.showLoading ?? !viewCache.performance;
    const statusText = document.getElementById('last-update');
    const mainContainer = document.getElementById('dashboard-cards');

    try {
        statusText.innerText = viewCache.performance ? 'Actualizando rendimiento...' : 'Escaneando rendimiento...';
        if (showLoading) {
            mainContainer.innerHTML = '<div class="loading">Analizando rendimiento del sistema...</div>';
        }

        const response = await fetch(PERFORMANCE_API_URL);
        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status}`);
        }

        const data = await response.json();
        cacheView('performance', data);

        if (currentView === 'performance') {
            renderPerformance(data);
            statusText.innerText = `Rendimiento: ${new Date().toLocaleTimeString()}`;
        }
    } catch (error) {
        console.error('Error fetching performance stats:', error);
        statusText.innerText = 'Error al escanear rendimiento';
        if (!viewCache.performance) {
            mainContainer.innerHTML = '<div class="loading">No fue posible cargar las metricas de rendimiento.</div>';
        }
    }
}

async function fetchNavigation(options = {}) {
    const showLoading = options.showLoading ?? !viewCache.navigation;
    const statusText = document.getElementById('last-update');
    const mainContainer = document.getElementById('dashboard-cards');

    try {
        statusText.innerText = viewCache.navigation ? 'Actualizando navegacion...' : 'Escaneando navegacion...';
        if (showLoading) {
            mainContainer.innerHTML = '<div class="loading">Analizando historial de navegacion...</div>';
        }

        const response = await fetch(NAVIGATION_API_URL);
        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status}`);
        }

        const data = await response.json();
        cacheView('navigation', data);

        if (currentView === 'navigation') {
            renderNavigation(data);
            statusText.innerText = `Navegacion: ${data.returnedEntries || 0}/${data.totalEntries || 0} - ${data.generatedAt}`;
        }
    } catch (error) {
        console.error('Error fetching navigation history:', error);
        statusText.innerText = 'Error al escanear navegacion';
        if (!viewCache.navigation) {
            mainContainer.innerHTML = '<div class="loading">No fue posible cargar el historial de navegacion.</div>';
        }
    }
}

function renderDashboard(data) {
    const mainContainer = document.getElementById('dashboard-cards');
    mainContainer.className = 'grid-container';
    mainContainer.innerHTML = '';

    cardDataMapping.forEach((item, index) => {
        let value = data[item.key] || 'N/A';
        if (item.key === 'ram_total') value = `${value} GB`;

        const cardHTML = `
            <div class="card glass" style="animation-delay: ${(index + 1) * 0.1}s">
                <div class="card-icon">
                    <svg viewBox="0 0 24 24" fill="none" class="icon-svg">${item.icon}</svg>
                </div>
                <div class="card-content">
                    <h3 class="card-title">${escapeHtml(item.title)}</h3>
                    <p class="card-value">${escapeHtml(value)}</p>
                </div>
            </div>
        `;
        mainContainer.insertAdjacentHTML('beforeend', cardHTML);
    });
}

function renderSecurity(data) {
    const mainContainer = document.getElementById('dashboard-cards');
    mainContainer.className = 'grid-container security-grid';
    mainContainer.innerHTML = '';

    data.items.forEach((item, index) => {
        const details = item.details.map(detail => `
            <li>
                <span>${escapeHtml(detail.label)}</span>
                <strong>${escapeHtml(detail.value)}</strong>
            </li>
        `).join('');

        const cardHTML = `
            <article class="card glass security-card status-${escapeHtml(item.status)}" style="animation-delay: ${(index + 1) * 0.08}s">
                <div class="security-card-header">
                    <div class="card-icon">
                        <svg viewBox="0 0 24 24" fill="none" class="icon-svg">${securityIconById[item.id] || icons.shield}</svg>
                    </div>
                    <span class="status-pill">${statusLabel(item.status)}</span>
                </div>
                <div class="card-content">
                    <h3 class="card-title">${escapeHtml(item.title)}</h3>
                    <p class="security-summary">${escapeHtml(item.summary)}</p>
                    <ul class="security-details">${details}</ul>
                </div>
            </article>
        `;
        mainContainer.insertAdjacentHTML('beforeend', cardHTML);
    });
}

function renderPerformance(data) {
    const mainContainer = document.getElementById('dashboard-cards');
    mainContainer.className = 'grid-container performance-grid';
    mainContainer.innerHTML = '';

    let delay = 1;

    // CPU Card
    const cpu = data.cpu || {};
    const cpuCard = `
        <div class="card glass performance-card" style="animation-delay: ${delay++ * 0.08}s">
            <div class="performance-header">
                <div class="card-icon">
                    <svg viewBox="0 0 24 24" fill="none" class="icon-svg">${icons.cpu}</svg>
                </div>
                <div class="performance-title-group">
                    <h3 class="card-title">Procesador</h3>
                    <p class="card-value">${escapeHtml(cpu.name || 'N/A')}</p>
                </div>
            </div>
            <div class="performance-body">
                <div class="metric-row">
                    <span>Uso CPU</span>
                    <span class="metric-value">${cpu.usagePercent ?? 0}%</span>
                </div>
                <div class="progress-bar">
                    <div class="progress-fill ${getProgressColor(cpu.usagePercent ?? 0)}" style="width: ${Math.min(cpu.usagePercent ?? 0, 100)}%"></div>
                </div>
                <div class="metric-grid">
                    <div class="metric-item"><span>Nucleos</span><strong>${cpu.coreCount || 0}</strong></div>
                    <div class="metric-item"><span>Reloj</span><strong>${escapeHtml(cpu.clockSpeed || 'N/A')}</strong></div>
                    <div class="metric-item"><span>Procesos</span><strong>${cpu.processCount || 0}</strong></div>
                    <div class="metric-item"><span>Hilos</span><strong>${cpu.threadCount || 0}</strong></div>
                    <div class="metric-item"><span>Uptime</span><strong>${escapeHtml(cpu.uptime || 'N/A')}</strong></div>
                </div>
            </div>
        </div>
    `;
    mainContainer.insertAdjacentHTML('beforeend', cpuCard);

    // Memory Card
    const mem = data.memory || {};
    const memCard = `
        <div class="card glass performance-card" style="animation-delay: ${delay++ * 0.08}s">
            <div class="performance-header">
                <div class="card-icon">
                    <svg viewBox="0 0 24 24" fill="none" class="icon-svg">${icons.memory}</svg>
                </div>
                <div class="performance-title-group">
                    <h3 class="card-title">Memoria RAM</h3>
                    <p class="card-value">${mem.totalGB || 0} GB Total</p>
                </div>
            </div>
            <div class="performance-body">
                <div class="metric-row">
                    <span>Uso RAM</span>
                    <span class="metric-value">${mem.usagePercent ?? 0}%</span>
                </div>
                <div class="progress-bar">
                    <div class="progress-fill ${getProgressColor(mem.usagePercent ?? 0)}" style="width: ${Math.min(mem.usagePercent ?? 0, 100)}%"></div>
                </div>
                <div class="metric-grid">
                    <div class="metric-item"><span>Usada</span><strong>${mem.usedGB || 0} GB</strong></div>
                    <div class="metric-item"><span>Libre</span><strong>${mem.freeGB || 0} GB</strong></div>
                    <div class="metric-item"><span>Disponible</span><strong>${mem.availableGB || 0} GB</strong></div>
                    <div class="metric-item"><span>Cache</span><strong>${mem.cachedGB || 0} GB</strong></div>
                </div>
            </div>
        </div>
    `;
    mainContainer.insertAdjacentHTML('beforeend', memCard);

    // Disk Cards
    const disks = data.disks || [];
    disks.forEach((disk, idx) => {
        const diskCard = `
            <div class="card glass performance-card" style="animation-delay: ${delay++ * 0.08}s">
                <div class="performance-header">
                    <div class="card-icon">
                        <svg viewBox="0 0 24 24" fill="none" class="icon-svg">${icons.disk}</svg>
                    </div>
                    <div class="performance-title-group">
                        <h3 class="card-title">Disco ${escapeHtml(disk.drive || '')}</h3>
                        <p class="card-value">${escapeHtml(disk.label || '')} ${escapeHtml(disk.fileSystem || '')}</p>
                    </div>
                </div>
                <div class="performance-body">
                    <div class="metric-row">
                        <span>Uso Disco</span>
                        <span class="metric-value">${disk.usagePercent ?? 0}%</span>
                    </div>
                    <div class="progress-bar">
                        <div class="progress-fill ${getProgressColor(disk.usagePercent ?? 0)}" style="width: ${Math.min(disk.usagePercent ?? 0, 100)}%"></div>
                    </div>
                    <div class="metric-grid">
                        <div class="metric-item"><span>Total</span><strong>${disk.totalGB || 0} GB</strong></div>
                        <div class="metric-item"><span>Usado</span><strong>${disk.usedGB || 0} GB</strong></div>
                        <div class="metric-item"><span>Libre</span><strong>${disk.freeGB || 0} GB</strong></div>
                        <div class="metric-item"><span>Lectura</span><strong>${disk.readSpeedMB || 0} MB/s</strong></div>
                        <div class="metric-item"><span>Escritura</span><strong>${disk.writeSpeedMB || 0} MB/s</strong></div>
                        <div class="metric-item"><span>Cola</span><strong>${disk.queueLength || 0}</strong></div>
                    </div>
                </div>
            </div>
        `;
        mainContainer.insertAdjacentHTML('beforeend', diskCard);
    });

    // Network Cards
    const networks = data.networks || [];
    networks.forEach((net, idx) => {
        const netCard = `
            <div class="card glass performance-card" style="animation-delay: ${delay++ * 0.08}s">
                <div class="performance-header">
                    <div class="card-icon">
                        <svg viewBox="0 0 24 24" fill="none" class="icon-svg">${icons.wifi}</svg>
                    </div>
                    <div class="performance-title-group">
                        <h3 class="card-title">Red</h3>
                        <p class="card-value">${escapeHtml(net.name || 'N/A')}</p>
                    </div>
                </div>
                <div class="performance-body">
                    <div class="metric-row">
                        <span>Estado</span>
                        <span class="metric-value status-ok-text">${escapeHtml(net.status || 'N/A')}</span>
                    </div>
                    <div class="metric-grid">
                        <div class="metric-item"><span>IP</span><strong>${escapeHtml(net.ipAddress || 'N/A')}</strong></div>
                        <div class="metric-item"><span>MAC</span><strong>${escapeHtml(net.macAddress || 'N/A')}</strong></div>
                        <div class="metric-item"><span>Velocidad</span><strong>${net.linkSpeedMbps || 0} Mbps</strong></div>
                        <div class="metric-item"><span>Recibido</span><strong>${formatBytes(net.bytesReceived || 0)}</strong></div>
                        <div class="metric-item"><span>Enviado</span><strong>${formatBytes(net.bytesSent || 0)}</strong></div>
                        <div class="metric-item"><span>Rx Vel.</span><strong>${net.receiveSpeedMbps || 0} Mbps</strong></div>
                        <div class="metric-item"><span>Tx Vel.</span><strong>${net.sendSpeedMbps || 0} Mbps</strong></div>
                    </div>
                </div>
            </div>
        `;
        mainContainer.insertAdjacentHTML('beforeend', netCard);
    });

    if (mainContainer.innerHTML === '') {
        mainContainer.innerHTML = '<div class="loading">No se encontraron metricas de rendimiento.</div>';
    }
}

function renderNavigation(data) {
    const mainContainer = document.getElementById('dashboard-cards');
    mainContainer.className = 'navigation-container';

    const entries = data.entries || [];
    const browsers = [...new Set(entries.map(entry => entry.browser).filter(Boolean))].sort();
    const filteredEntries = filterNavigationEntries(entries);
    const groupedEntries = groupNavigationEntries(filteredEntries);
    const browserOptions = browsers.map(browser => `
        <option value="${escapeHtml(browser)}" ${navigationFilters.browser === browser ? 'selected' : ''}>${escapeHtml(browser)}</option>
    `).join('');
    const notes = (data.notes || []).map(note => `<li>${escapeHtml(note)}</li>`).join('');

    mainContainer.innerHTML = `
        <section class="navigation-panel glass">
            <div class="navigation-header">
                <div>
                    <h2>Historial de navegacion</h2>
                    <p id="navigation-count">${escapeHtml(groupedEntries.length)} paginas agrupadas de ${escapeHtml(filteredEntries.length)} visitas visibles</p>
                </div>
                <div class="navigation-summary">
                    <span>${escapeHtml((data.scannedBrowsers || []).join(', ') || 'Sin navegadores detectados')}</span>
                </div>
            </div>

            <div class="navigation-toolbar">
                <input id="navigation-search" type="search" value="${escapeHtml(navigationFilters.query)}" placeholder="Buscar pagina, URL o titulo">
                <select id="navigation-browser-filter">
                    <option value="all">Todos los navegadores</option>
                    ${browserOptions}
                </select>
            </div>

            <ul class="navigation-notes">${notes}</ul>

            <div class="navigation-table-wrap">
                <table class="navigation-table">
                    <thead>
                        <tr>
                            <th>Pagina</th>
                            <th>Navegador</th>
                            <th>Visitas</th>
                            <th>Fecha</th>
                            <th>Hora</th>
                            <th>Modo</th>
                            <th>Duracion</th>
                        </tr>
                    </thead>
                    <tbody id="navigation-tbody">
                        ${renderNavigationRows(groupedEntries)}
                    </tbody>
                </table>
            </div>
        </section>
    `;

    bindNavigationFilters(data);
}

function renderNavigationRows(entries) {
    if (!entries.length) {
        return '<tr><td colspan="7" class="empty-cell">No hay registros para el filtro seleccionado.</td></tr>';
    }

    return entries.map(entry => `
        <tr>
            <td class="page-cell">
                <strong>${escapeHtml(entry.title || entry.host || 'Sin titulo')}</strong>
                <a href="${escapeAttribute(safeNavigationHref(entry.url))}" target="_blank" rel="noreferrer" title="${escapeAttribute(entry.url || 'N/A')}">${escapeHtml(formatDisplayUrl(entry.url || 'N/A'))}</a>
                <small>${escapeHtml(entry.host || '')}</small>
            </td>
            <td>
                <div class="browser-stack">
                    ${entry.browsers.map(browser => `<span class="browser-pill">${escapeHtml(browser)}</span>`).join('')}
                </div>
                <small>${escapeHtml(entry.profiles.join(', '))}</small>
            </td>
            <td>
                <strong class="visit-count">${escapeHtml(entry.visitCount)}</strong>
                <small>Primera: ${escapeHtml(entry.firstDate)} ${escapeHtml(entry.firstTime)}</small>
            </td>
            <td>${escapeHtml(entry.lastDate)}</td>
            <td>${escapeHtml(entry.lastTime)}</td>
            <td>${escapeHtml(entry.mode || 'Normal')}</td>
            <td>
                <strong>${escapeHtml(entry.totalDuration)}</strong>
                <small>Ultima: ${escapeHtml(entry.lastDuration)}</small>
                ${entry.hasEstimatedDuration ? '<small>incluye estimadas</small>' : ''}
            </td>
        </tr>
    `).join('');
}

function bindNavigationFilters(data) {
    const search = document.getElementById('navigation-search');
    const browser = document.getElementById('navigation-browser-filter');

    if (search) {
        search.addEventListener('input', event => {
            navigationFilters.query = event.target.value;
            updateNavigationRows(data);
        });
    }

    if (browser) {
        browser.addEventListener('change', event => {
            navigationFilters.browser = event.target.value;
            updateNavigationRows(data);
        });
    }
}

function updateNavigationRows(data) {
    const filteredEntries = filterNavigationEntries(data.entries || []);
    const groupedEntries = groupNavigationEntries(filteredEntries);
    const body = document.getElementById('navigation-tbody');
    const count = document.getElementById('navigation-count');

    if (body) {
        body.innerHTML = renderNavigationRows(groupedEntries);
    }

    if (count) {
        count.innerText = `${groupedEntries.length} paginas agrupadas de ${filteredEntries.length} visitas visibles`;
    }
}

function filterNavigationEntries(entries) {
    const query = navigationFilters.query.trim().toLowerCase();
    return entries.filter(entry => {
        const browserMatches = navigationFilters.browser === 'all' || entry.browser === navigationFilters.browser;
        const queryMatches = !query ||
            (entry.title || '').toLowerCase().includes(query) ||
            (entry.url || '').toLowerCase().includes(query) ||
            (entry.browser || '').toLowerCase().includes(query);
        return browserMatches && queryMatches;
    });
}

function groupNavigationEntries(entries) {
    const groups = new Map();

    entries.forEach(entry => {
        const key = normalizeNavigationUrl(entry.url || '');
        if (!groups.has(key)) {
            groups.set(key, {
                url: entry.url || '',
                title: entry.title || '',
                host: getNavigationHost(entry.url || ''),
                browsers: new Set(),
                profiles: new Set(),
                visitCount: 0,
                firstVisit: null,
                lastVisit: null,
                mode: 'Normal',
                totalSeconds: 0,
                hasDuration: false,
                hasEstimatedDuration: false,
                lastDuration: 'No disponible'
            });
        }

        const group = groups.get(key);
        const visitDate = parseNavigationDate(entry.visitedAt);
        const durationSeconds = parseDurationToSeconds(entry.duration || '');

        group.visitCount += 1;
        if (entry.browser) group.browsers.add(entry.browser);
        if (entry.profile) group.profiles.add(entry.profile);
        if (!group.title && entry.title) group.title = entry.title;

        if (!group.firstVisit || visitDate < group.firstVisit) {
            group.firstVisit = visitDate;
        }

        if (!group.lastVisit || visitDate > group.lastVisit) {
            group.lastVisit = visitDate;
            group.mode = entry.mode || 'Normal';
            group.lastDuration = entry.duration || 'No disponible';
        }

        if (durationSeconds > 0) {
            group.totalSeconds += durationSeconds;
            group.hasDuration = true;
        }

        if (entry.durationEstimated) {
            group.hasEstimatedDuration = true;
        }
    });

    return [...groups.values()]
        .map(group => ({
            ...group,
            browsers: [...group.browsers].sort(),
            profiles: [...group.profiles].sort(),
            firstDate: formatDateOnly(group.firstVisit),
            firstTime: formatTimeOnly(group.firstVisit),
            lastDate: formatDateOnly(group.lastVisit),
            lastTime: formatTimeOnly(group.lastVisit),
            totalDuration: group.hasDuration ? formatSecondsDuration(group.totalSeconds) : 'No disponible'
        }))
        .sort((a, b) => (b.lastVisit?.getTime() || 0) - (a.lastVisit?.getTime() || 0));
}

function normalizeNavigationUrl(url) {
    try {
        const parsed = new URL(url);
        parsed.hash = '';
        parsed.searchParams.sort();
        const normalized = parsed.toString();
        return normalized.endsWith('/') ? normalized.slice(0, -1) : normalized;
    } catch {
        return url.trim().toLowerCase();
    }
}

function getNavigationHost(url) {
    try {
        return new URL(url).hostname.replace(/^www\./, '');
    } catch {
        return '';
    }
}

function safeNavigationHref(url) {
    try {
        const parsed = new URL(url);
        return parsed.protocol === 'http:' || parsed.protocol === 'https:' ? url : '#';
    } catch {
        return '#';
    }
}

function formatDisplayUrl(url) {
    if (!url || url === 'N/A') return 'N/A';

    try {
        const parsed = new URL(url);
        const path = parsed.pathname && parsed.pathname !== '/' ? parsed.pathname : '';
        const query = parsed.search ? '?...' : '';
        const display = `${parsed.hostname}${path}${query}`;
        return truncateText(display, 76);
    } catch {
        return truncateText(url, 76);
    }
}

function truncateText(value, maxLength) {
    return value.length > maxLength ? `${value.slice(0, maxLength - 1)}...` : value;
}

function parseNavigationDate(value) {
    const parsed = value ? new Date(value.replace(' ', 'T')) : new Date(0);
    return Number.isNaN(parsed.getTime()) ? new Date(0) : parsed;
}

function parseDurationToSeconds(value) {
    if (!value || value === 'No disponible') return 0;

    const hours = /(\d+)h/.exec(value)?.[1] || 0;
    const minutes = /(\d+)m/.exec(value)?.[1] || 0;
    const seconds = /(\d+)s/.exec(value)?.[1] || 0;

    return Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds);
}

function formatSecondsDuration(totalSeconds) {
    if (!totalSeconds) return 'No disponible';

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = Math.floor(totalSeconds % 60);

    if (hours > 0) return `${hours}h ${minutes}m`;
    if (minutes > 0) return `${minutes}m ${seconds}s`;
    return `${Math.max(1, seconds)}s`;
}

function formatDateOnly(date) {
    if (!date || date.getTime() === 0) return 'N/A';
    return date.toLocaleDateString();
}

function formatTimeOnly(date) {
    if (!date || date.getTime() === 0) return 'N/A';
    return date.toLocaleTimeString();
}

function getProgressColor(percent) {
    if (percent >= 90) return 'danger';
    if (percent >= 70) return 'warning';
    return 'ok';
}

function formatBytes(bytes) {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function setupNavigation() {
    document.querySelectorAll('.nav-item').forEach(button => {
        button.addEventListener('click', () => {
            const view = button.dataset.view;
            if (!view || view === currentView) return;

            currentView = view;
            document.querySelectorAll('.nav-item').forEach(item => item.classList.remove('active'));
            button.classList.add('active');

            renderCachedView(currentView);
            refreshView(currentView, { showLoading: !viewCache[currentView] });
        });
    });
}

function renderCachedView(view) {
    const cached = viewCache[view];
    if (!cached) {
        return false;
    }

    if (view === 'security') {
        renderSecurity(cached.data);
        document.getElementById('last-update').innerText = `Seguridad: ${statusLabel(cached.data.overallStatus)} - ${cached.data.generatedAt}`;
    } else if (view === 'performance') {
        renderPerformance(cached.data);
        document.getElementById('last-update').innerText = `Rendimiento: ${formatCachedTime(cached.cachedAt)}`;
    } else if (view === 'navigation') {
        renderNavigation(cached.data);
        document.getElementById('last-update').innerText = `Navegacion: ${cached.data.returnedEntries || 0}/${cached.data.totalEntries || 0} - ${cached.data.generatedAt}`;
    } else {
        renderDashboard(cached.data);
        document.getElementById('last-update').innerText = `Sincronizado: ${formatCachedTime(cached.cachedAt)}`;
    }

    return true;
}

function refreshView(view, options = {}) {
    if (view === 'security') {
        return fetchSecurity(options);
    }
    if (view === 'performance') {
        return fetchPerformance(options);
    }
    if (view === 'navigation') {
        return fetchNavigation(options);
    }
    return fetchStats(options);
}

function refreshActiveView() {
    refreshView(currentView, { showLoading: false });
}

function cacheView(view, data) {
    const cached = {
        data,
        cachedAt: new Date().toISOString()
    };

    viewCache[view] = cached;
    try {
        localStorage.setItem(CACHE_KEYS[view], JSON.stringify(cached));
    } catch (error) {
        console.warn(`No se pudo guardar cache de ${view}`, error);
    }
}

function readCachedView(view) {
    try {
        const raw = localStorage.getItem(CACHE_KEYS[view]);
        return raw ? JSON.parse(raw) : null;
    } catch (error) {
        console.warn(`No se pudo leer cache de ${view}`, error);
        return null;
    }
}

function statusLabel(status) {
    const labels = {
        ok: 'OK',
        warning: 'Atencion',
        danger: 'Critico',
        unknown: 'Sin dato'
    };
    return labels[status] || 'Sin dato';
}

function escapeHtml(value) {
    return String(value)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}

function escapeAttribute(value) {
    return escapeHtml(value).replaceAll('`', '&#096;');
}

function formatCachedTime(value) {
    const date = value ? new Date(value) : new Date();
    return Number.isNaN(date.getTime()) ? new Date().toLocaleTimeString() : date.toLocaleTimeString();
}

document.addEventListener('DOMContentLoaded', () => {
    setupNavigation();
    renderCachedView(currentView);
    refreshView(currentView, { showLoading: !viewCache[currentView] });
    setInterval(refreshActiveView, REFRESH_INTERVAL_MS);
});
