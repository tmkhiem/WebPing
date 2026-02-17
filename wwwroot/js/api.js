// WebPing API Utilities

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
    alertDiv.textContent = message;
    
    const container = document.querySelector('.container');
    const firstChild = container.firstChild;
    container.insertBefore(alertDiv, firstChild);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        alertDiv.remove();
    }, 5000);
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
