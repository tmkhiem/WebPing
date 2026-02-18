// Simplified User Agent Parser
// Detects browser and OS from navigator.userAgent

function parseUserAgent() {
    const ua = navigator.userAgent;
    const result = {
        browser: { name: 'Unknown Browser' },
        os: { name: 'Unknown OS' },
        device: { type: 'desktop' }
    };

    // Detect browser - order matters! Check more specific browsers first
    if (ua.indexOf('Opera') > -1 || ua.indexOf('OPR') > -1) {
        result.browser.name = 'Opera';
    } else if (ua.indexOf('Edg') > -1 || ua.indexOf('Edge') > -1) {
        result.browser.name = 'Edge';
    } else if (ua.indexOf('Chrome') > -1) {
        result.browser.name = 'Chrome';
    } else if (ua.indexOf('Safari') > -1) {
        result.browser.name = 'Safari';
    } else if (ua.indexOf('Firefox') > -1) {
        result.browser.name = 'Firefox';
    }

    // Detect OS
    if (ua.indexOf('Windows') > -1) {
        result.os.name = 'Windows';
    } else if (ua.indexOf('Mac OS X') > -1) {
        result.os.name = 'macOS';
    } else if (ua.indexOf('Linux') > -1 && ua.indexOf('Android') === -1) {
        result.os.name = 'Linux';
    } else if (ua.indexOf('Android') > -1) {
        result.os.name = 'Android';
        result.device.type = 'mobile';
    } else if (ua.indexOf('iPhone') > -1 || ua.indexOf('iPad') > -1) {
        result.os.name = ua.indexOf('iPad') > -1 ? 'iPadOS' : 'iOS';
        result.device.type = ua.indexOf('iPad') > -1 ? 'tablet' : 'mobile';
    }

    // Try to extract device info for mobile
    if (result.device.type === 'mobile' || result.device.type === 'tablet') {
        // Try to get device model from user agent
        const matches = ua.match(/\(([^)]+)\)/);
        if (matches && matches[1]) {
            const parts = matches[1].split(';');
            if (parts.length > 1) {
                result.device.model = parts[1].trim();
            }
        }
    }

    return result;
}

// Simple UAParser compatible interface
class UAParser {
    constructor() {
        this.result = parseUserAgent();
    }

    getResult() {
        return this.result;
    }

    getBrowser() {
        return this.result.browser;
    }

    getOS() {
        return this.result.os;
    }

    getDevice() {
        return this.result.device;
    }
}
