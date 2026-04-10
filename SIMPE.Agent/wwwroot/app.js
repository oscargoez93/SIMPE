const API_URL = '/api/equipo/current';

const cardDataMapping = [
    { key: 'nombre', title: 'Equipo', icon: '<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><path d="M22 6l-10 7L2 6"/>' },
    { key: 'usuario', title: 'Usuario Actual', icon: '<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>' },
    { key: 'ip', title: 'Dirección IP', icon: '<rect x="2" y="2" width="20" height="8" rx="2" ry="2"/><rect x="2" y="14" width="20" height="8" rx="2" ry="2"/><line x1="6" y1="6" x2="6" y2="6"/><line x1="6" y1="18" x2="6" y2="18"/>' },
    { key: 'cpu_model', title: 'Procesador', icon: '<rect x="4" y="4" width="16" height="16" rx="2" ry="2"/><rect x="9" y="9" width="6" height="6"/><line x1="9" y1="1" x2="9" y2="4"/><line x1="15" y1="1" x2="15" y2="4"/><line x1="9" y1="20" x2="9" y2="23"/><line x1="15" y1="20" x2="15" y2="23"/><line x1="20" y1="9" x2="23" y2="9"/><line x1="20" y1="14" x2="23" y2="14"/><line x1="1" y1="9" x2="4" y2="9"/><line x1="1" y1="14" x2="4" y2="14"/>' },
    { key: 'ram_total', title: 'Memoria RAM', icon: '<rect x="2" y="4" width="20" height="16" rx="2" ry="2"/><path d="M12 12V20"/><path d="M8 12V20"/><path d="M16 12V20"/><path d="M22 10H2"/><path d="M12 4v4"/><path d="M8 4v4"/><path d="M16 4v4"/>' },
    { key: 'disco_tipo', title: 'Almacenamiento', icon: '<ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/>' },
    { key: 'os_version', title: 'Sistema Operativo', icon: '<rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><line x1="3" y1="9" x2="21" y2="9"/><line x1="9" y1="21" x2="9" y2="9"/>' },
    { key: 'antivirus_nombre', title: 'Seguridad (Antivirus)', icon: '<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>' },
    { key: 'tiempo_arranque', title: 'Último Inicio', icon: '<circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>' }
];

async function fetchStats() {
    const statusText = document.getElementById('last-update');
    try {
        statusText.innerText = 'Actualizando...';
        const response = await fetch(API_URL);
        
        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status}`);
        }
        
        const data = await response.json();
        renderDashboard(data);
        
        // Format last update time
        const now = new Date();
        statusText.innerText = `Sincronizado: ${now.toLocaleTimeString()}`;
    } catch (error) {
        console.error("Error fetching hardware stats:", error);
        statusText.innerText = 'Error al conectar (Asegúrate de que el backend corra y pase 1 min)';
    }
}

function renderDashboard(data) {
    const mainContainer = document.getElementById('dashboard-cards');
    mainContainer.innerHTML = ''; // Clear loaded or old elements
    
    let delayCounter = 1;
    cardDataMapping.forEach(item => {
        let value = data[item.key] || 'N/A';
        
        // Format some special fields
        if(item.key === 'ram_total') value = value + ' GB';

        const cardHTML = `
            <div class="card glass" style="animation-delay: ${delayCounter * 0.1}s">
                <div class="card-icon">
                    <svg viewBox="0 0 24 24" fill="none" class="icon-svg">${item.icon}</svg>
                </div>
                <div class="card-content">
                    <h3 class="card-title">${item.title}</h3>
                    <p class="card-value">${value}</p>
                </div>
            </div>
        `;
        mainContainer.insertAdjacentHTML('beforeend', cardHTML);
        delayCounter++;
    });
}

// Inicializar
document.addEventListener('DOMContentLoaded', () => {
    fetchStats();
    // Actualizar cada 60 segundos por si hay cambios en Backend
    setInterval(fetchStats, 60000); 
});
