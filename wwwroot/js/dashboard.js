// Dashboard functionality

let pushSupported = false;
let serviceWorkerRegistration = null;
let currentTab = 'dashboard';

// HTML escape function to prevent XSS
function escapeHtml(unsafe) {
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

// Initialize on page load
if (!requireAuth()) {
    // Will redirect automatically
} else {
    initDashboard();
}

function initDashboard() {
    const creds = getCredentials();
    if (creds) {
        document.getElementById('username-display').textContent = creds.username;
    }
    
    // Load dashboard data
    loadDashboardStats();
    
    // Initialize subscriptions tab
    checkBrowserSupport();
    autofillDeviceName();
}

// Tab switching
function switchTab(tabName) {
    currentTab = tabName;
    
    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
    event.target.closest('.tab-btn').classList.add('active');
    
    // Update tab content
    document.querySelectorAll('.tab-content').forEach(content => content.classList.remove('active'));
    document.getElementById(`${tabName}-tab`).classList.add('active');
    
    // Load data for the tab
    if (tabName === 'topics') {
        loadTopics();
    } else if (tabName === 'subscriptions') {
        loadEndpoints();
    }
}

// User menu
function toggleUserMenu() {
    const dropdown = document.getElementById('user-dropdown');
    dropdown.classList.toggle('hidden');
}

// Close dropdown when clicking outside
document.addEventListener('click', (e) => {
    const userMenu = document.querySelector('.user-menu');
    if (userMenu && !userMenu.contains(e.target)) {
        document.getElementById('user-dropdown').classList.add('hidden');
    }
});

// Change password modal
function showChangePassword() {
    document.getElementById('user-dropdown').classList.add('hidden');
    document.getElementById('change-password-modal').classList.remove('hidden');
}

function closeChangePassword() {
    document.getElementById('change-password-modal').classList.add('hidden');
    document.getElementById('new-password').value = '';
    document.getElementById('confirm-password').value = '';
}

async function handleChangePassword(event) {
    event.preventDefault();
    
    const newPassword = document.getElementById('new-password').value;
    const confirmPassword = document.getElementById('confirm-password').value;
    
    if (newPassword !== confirmPassword) {
        showAlert('Passwords do not match', 'error');
        return;
    }
    
    if (newPassword.length < 6) {
        showAlert('Password must be at least 6 characters', 'error');
        return;
    }
    
    try {
        const creds = getCredentials();
        await apiCall('/auth/change-password', {
            method: 'POST',
            body: JSON.stringify({ newPassword })
        });
        
        // Update stored credentials
        saveCredentials(creds.username, newPassword);
        
        showAlert('Password changed successfully!', 'success');
        closeChangePassword();
    } catch (error) {
        showAlert('Failed to change password: ' + error.message, 'error');
    }
}

// Dashboard stats
async function loadDashboardStats() {
    try {
        const [topics, endpoints] = await Promise.all([
            apiCall('/topics', { method: 'GET' }),
            apiCall('/push-endpoints', { method: 'GET' })
        ]);
        
        document.getElementById('topic-count').textContent = topics?.length || 0;
        document.getElementById('device-count').textContent = endpoints?.length || 0;
    } catch (error) {
        console.error('Failed to load dashboard stats:', error);
    }
}

// Topics functionality
async function loadTopics() {
    const loadingDiv = document.getElementById('topics-loading');
    const topicsContainer = document.getElementById('topics-container');
    
    loadingDiv.classList.remove('hidden');
    
    try {
        const topics = await apiCall('/topics', { method: 'GET' });
        
        loadingDiv.classList.add('hidden');
        
        if (!topics || topics.length === 0) {
            topicsContainer.innerHTML = `
                <div class="empty-state">
                    <span class="material-symbols-outlined">topic</span>
                    <p>No topics yet. Create your first topic above!</p>
                </div>
            `;
            return;
        }
        
        topicsContainer.innerHTML = topics.map(topic => createTopicCard(topic)).join('');
        
        // Attach event listeners
        topics.forEach(topic => {
            const card = document.querySelector(`[data-topic="${escapeHtml(topic.name)}"]`);
            if (card) {
                const deleteBtn = card.querySelector('.topic-delete-btn');
                const expandBtn = card.querySelector('.topic-expand-btn');
                
                if (deleteBtn) {
                    deleteBtn.addEventListener('click', () => deleteTopic(topic.name));
                }
                if (expandBtn) {
                    expandBtn.addEventListener('click', () => toggleTopicExpand(topic.name));
                }
            }
        });
    } catch (error) {
        loadingDiv.classList.add('hidden');
        showAlert('Failed to load topics: ' + error.message, 'error');
    }
}

function createTopicCard(topic) {
    const topicName = escapeHtml(topic.name);
    const endpoint = `${window.location.origin}/send/${topicName}`;
    
    return `
        <div class="topic-card" data-topic="${topicName}">
            <div class="topic-card-header">
                <div class="topic-card-title">
                    <span class="material-symbols-outlined">topic</span>
                    ${topicName}
                </div>
                <div class="topic-card-actions">
                    <button class="icon-btn topic-expand-btn" title="Show code examples">
                        <span class="material-symbols-outlined">code</span>
                    </button>
                    <button class="icon-btn danger topic-delete-btn" title="Delete topic">
                        <span class="material-symbols-outlined">close</span>
                    </button>
                </div>
            </div>
            <div class="topic-card-body">
                <div class="topic-endpoint">
                    <code>POST ${endpoint}</code>
                </div>
                <div class="topic-card-expanded hidden">
                    <div class="code-tabs">
                        <button class="code-tab active" onclick="switchCodeTab(event, '${topicName}', 'curl-cmd')">CMD (curl)</button>
                        <button class="code-tab" onclick="switchCodeTab(event, '${topicName}', 'curl-bash')">Bash (curl)</button>
                        <button class="code-tab" onclick="switchCodeTab(event, '${topicName}', 'powershell')">PowerShell</button>
                        <button class="code-tab" onclick="switchCodeTab(event, '${topicName}', 'python')">Python</button>
                        <button class="code-tab" onclick="switchCodeTab(event, '${topicName}', 'csharp')">C#</button>
                    </div>
                    <div class="code-content">
                        <div class="code-panel active" data-code="curl-cmd">
                            <pre><code>curl -X POST "${endpoint}" ^
  -H "Content-Type: text/plain" ^
  -d "Your notification message"</code></pre>
                        </div>
                        <div class="code-panel" data-code="curl-bash">
                            <pre><code>curl -X POST "${endpoint}" \\
  -H "Content-Type: text/plain" \\
  -d "Your notification message"</code></pre>
                        </div>
                        <div class="code-panel" data-code="powershell">
                            <pre><code>Invoke-RestMethod -Uri "${endpoint}" \`
  -Method Post \`
  -ContentType "text/plain" \`
  -Body "Your notification message"</code></pre>
                        </div>
                        <div class="code-panel" data-code="python">
                            <pre><code>import requests

response = requests.post(
    "${endpoint}",
    data="Your notification message",
    headers={"Content-Type": "text/plain"}
)
print(response.json())</code></pre>
                        </div>
                        <div class="code-panel" data-code="csharp">
                            <pre><code>using var client = new HttpClient();
var content = new StringContent(
    "Your notification message",
    Encoding.UTF8,
    "text/plain"
);
var response = await client.PostAsync(
    "${endpoint}",
    content
);
var result = await response.Content.ReadAsStringAsync();</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

function switchCodeTab(event, topicName, codeType) {
    const card = document.querySelector(`[data-topic="${topicName}"]`);
    if (!card) return;
    
    // Update tabs
    card.querySelectorAll('.code-tab').forEach(tab => tab.classList.remove('active'));
    event.target.classList.add('active');
    
    // Update panels
    card.querySelectorAll('.code-panel').forEach(panel => panel.classList.remove('active'));
    card.querySelector(`[data-code="${codeType}"]`).classList.add('active');
}

function toggleTopicExpand(topicName) {
    const card = document.querySelector(`[data-topic="${topicName}"]`);
    if (!card) return;
    
    const expandedSection = card.querySelector('.topic-card-expanded');
    const expandBtn = card.querySelector('.topic-expand-btn span');
    
    expandedSection.classList.toggle('hidden');
    expandBtn.textContent = expandedSection.classList.contains('hidden') ? 'code' : 'code_off';
}

async function handleCreateTopic(event) {
    event.preventDefault();
    
    const topicName = document.getElementById('topic-name').value.trim();
    
    try {
        await apiCall('/topics', {
            method: 'POST',
            body: JSON.stringify({ name: topicName })
        });
        
        showAlert('Topic created successfully!', 'success');
        document.getElementById('topic-name').value = '';
        loadTopics();
        loadDashboardStats();
    } catch (error) {
        showAlert('Failed to create topic: ' + error.message, 'error');
    }
}

async function deleteTopic(topicName) {
    if (!confirm(`Delete topic "${topicName}"?`)) {
        return;
    }
    
    showAlert('Note: DELETE endpoint not implemented. Topic deletion requires backend support.', 'info');
}

// Subscriptions functionality
function autofillDeviceName() {
    try {
        const parser = new UAParser();
        const result = parser.getResult();
        
        const browserName = result.browser.name || 'Unknown Browser';
        const osName = result.os.name || 'Unknown OS';
        const deviceType = result.device.type || 'desktop';
        
        let deviceName = '';
        if (deviceType === 'mobile' || deviceType === 'tablet') {
            const deviceVendor = result.device.vendor || '';
            const deviceModel = result.device.model || '';
            deviceName = `${deviceVendor} ${deviceModel} ${browserName}`.trim();
        } else {
            deviceName = `${osName} - ${browserName}`;
        }
        
        const deviceNameInput = document.getElementById('device-name');
        if (deviceNameInput && deviceName) {
            deviceNameInput.value = deviceName;
        }
    } catch (error) {
        console.error('Failed to auto-detect device name:', error);
    }
}

async function checkBrowserSupport() {
    const checkDiv = document.getElementById('browser-check');
    const registerSection = document.getElementById('register-section');
    
    if (!('serviceWorker' in navigator)) {
        checkDiv.innerHTML = '<span class="material-symbols-outlined">error</span> Service Workers not supported';
        checkDiv.className = 'alert alert-error';
        return;
    }
    
    if (!('PushManager' in window)) {
        checkDiv.innerHTML = '<span class="material-symbols-outlined">error</span> Push notifications not supported';
        checkDiv.className = 'alert alert-error';
        return;
    }
    
    try {
        const registration = await navigator.serviceWorker.register('/sw.js');
        serviceWorkerRegistration = registration;
        
        checkDiv.innerHTML = '<span class="material-symbols-outlined">check_circle</span> Browser supports push notifications';
        checkDiv.className = 'alert alert-success';
        registerSection.classList.remove('hidden');
        pushSupported = true;
    } catch (error) {
        checkDiv.innerHTML = '<span class="material-symbols-outlined">error</span> Service Worker registration failed';
        checkDiv.className = 'alert alert-error';
    }
}

async function handleRegisterPush(event) {
    event.preventDefault();
    
    if (!pushSupported || !serviceWorkerRegistration) {
        showAlert('Push notifications not supported', 'error');
        return;
    }
    
    const deviceName = document.getElementById('device-name').value.trim();
    const registerBtn = document.getElementById('register-btn');
    
    try {
        registerBtn.disabled = true;
        registerBtn.innerHTML = '<span class="material-symbols-outlined rotating">progress_activity</span> Registering...';
        
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') {
            showAlert('Notification permission denied', 'error');
            return;
        }
        
        const vapidResponse = await apiCall('/vapid-public-key', { method: 'GET' });
        if (!vapidResponse?.configured || !vapidResponse.publicKey) {
            showAlert('VAPID keys not configured', 'error');
            return;
        }
        
        const subscription = await serviceWorkerRegistration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(vapidResponse.publicKey)
        });
        
        const subscriptionJSON = subscription.toJSON();
        
        await apiCall('/push-endpoints', {
            method: 'POST',
            body: JSON.stringify({
                name: deviceName,
                endpoint: subscriptionJSON.endpoint,
                p256dh: subscriptionJSON.keys.p256dh,
                auth: subscriptionJSON.keys.auth
            })
        });
        
        showAlert('Browser registered successfully!', 'success');
        document.getElementById('device-name').value = '';
        autofillDeviceName();
        loadEndpoints();
        loadDashboardStats();
    } catch (error) {
        showAlert('Failed to register: ' + error.message, 'error');
    } finally {
        registerBtn.disabled = false;
        registerBtn.innerHTML = '<span class="material-symbols-outlined">add_circle</span> Register Browser';
    }
}

async function loadEndpoints() {
    const loadingDiv = document.getElementById('endpoints-loading');
    const endpointsContainer = document.getElementById('endpoints-container');
    
    loadingDiv.classList.remove('hidden');
    
    try {
        const endpoints = await apiCall('/push-endpoints', { method: 'GET' });
        
        loadingDiv.classList.add('hidden');
        
        if (!endpoints || endpoints.length === 0) {
            endpointsContainer.innerHTML = `
                <div class="empty-state">
                    <span class="material-symbols-outlined">devices</span>
                    <p>No devices registered. Register this browser above!</p>
                </div>
            `;
            return;
        }
        
        endpointsContainer.innerHTML = endpoints.map(endpoint => `
            <div class="device-card" data-endpoint-id="${endpoint.id}">
                <div class="device-card-content">
                    <span class="material-symbols-outlined">computer</span>
                    <div class="device-card-name">${escapeHtml(endpoint.name)}</div>
                </div>
                <button class="icon-btn danger" onclick="deleteEndpoint(${endpoint.id})" title="Remove device">
                    <span class="material-symbols-outlined">delete</span>
                </button>
            </div>
        `).join('');
    } catch (error) {
        loadingDiv.classList.add('hidden');
        showAlert('Failed to load devices: ' + error.message, 'error');
    }
}

async function deleteEndpoint(endpointId) {
    if (!confirm('Remove this device?')) {
        return;
    }
    
    try {
        await apiCall(`/push-endpoints/${endpointId}`, { method: 'DELETE' });
        showAlert('Device removed!', 'success');
        loadEndpoints();
        loadDashboardStats();
    } catch (error) {
        showAlert('Failed to remove device: ' + error.message, 'error');
    }
}

// Convert VAPID key from base64 to Uint8Array
function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/\-/g, '+')
        .replace(/_/g, '/');
    
    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);
    
    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}
