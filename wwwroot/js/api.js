// WebPing API Utilities

// Constants
const ALERT_DURATION_MS = 5000;

// Get credentials from localStorage
function getCredentials() {
    const username = localStorage.getItem('username');
    const password = localStorage.getItem('password');
    if (!username || !password) {
        return null;
    }
    return { username, password };
}

// Create Basic Auth header
function getAuthHeader() {
    const creds = getCredentials();
    if (!creds) {
        return null;
    }
    const encoded = btoa(`${creds.username}:${creds.password}`);
    return `Basic ${encoded}`;
}

// Check if user is logged in
function isLoggedIn() {
    return getCredentials() !== null;
}

// Logout user
function logout() {
    localStorage.removeItem('username');
    localStorage.removeItem('password');
    window.location.href = '/';
}

// Save credentials
function saveCredentials(username, password) {
    // NOTE: Storing passwords in localStorage is not recommended for production
    // as it's vulnerable to XSS attacks. This is acceptable for a demo where
    // Basic Auth is used anyway (credentials sent with every request).
    // For production, consider using HttpOnly cookies with session tokens.
    localStorage.setItem('username', username);
    localStorage.setItem('password', password);
}

// API base URL (relative for same server)
const API_BASE = '';

// Make API call with authentication
async function apiCall(endpoint, options = {}) {
    const authHeader = getAuthHeader();
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };
    
    if (authHeader && !options.skipAuth) {
        headers['Authorization'] = authHeader;
    }

    try {
        const response = await fetch(API_BASE + endpoint, {
            ...options,
            headers
        });

        const contentType = response.headers.get('content-type');
        let data = null;
        
        if (contentType && contentType.includes('application/json')) {
            data = await response.json();
        }

        if (!response.ok) {
            if (response.status === 401) {
                // Unauthorized - redirect to login
                logout();
                return null;
            }
            throw new Error(data?.message || `HTTP error! status: ${response.status}`);
        }

        return data;
    } catch (error) {
        console.error('API call error:', error);
        throw error;
    }
}

// Show alert message
function showAlert(message, type = 'info') {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type}`;
    
    // Add icon based on type
    const icon = document.createElement('span');
    icon.className = 'material-symbols-outlined';
    if (type === 'success') {
        icon.textContent = 'check_circle';
    } else if (type === 'error') {
        icon.textContent = 'error';
    } else {
        icon.textContent = 'info';
    }
    
    alertDiv.appendChild(icon);
    alertDiv.appendChild(document.createTextNode(' ' + message));
    
    // Try to find alert container first (dashboard), otherwise use container
    let container = document.getElementById('alert-container');
    if (!container) {
        container = document.querySelector('.container');
        if (container) {
            const firstChild = container.firstChild;
            container.insertBefore(alertDiv, firstChild);
        }
    } else {
        container.appendChild(alertDiv);
    }
    
    // Auto-remove after configured duration
    setTimeout(() => {
        alertDiv.remove();
    }, ALERT_DURATION_MS);
}

// Protect page - require authentication
function requireAuth() {
    if (!isLoggedIn()) {
        window.location.href = '/';
        return false;
    }
    return true;
}

// Display current user
function displayUserInfo() {
    const creds = getCredentials();
    if (creds) {
        const userInfoDiv = document.getElementById('user-info');
        if (userInfoDiv) {
            userInfoDiv.innerHTML = `
                <span>Logged in as: <strong>${creds.username}</strong></span>
                <button onclick="logout()" class="secondary">Logout</button>
            `;
        }
    }
}
