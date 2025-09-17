// Color Configuration System for Visual Controls
// Customize these values to change colors across all controls

const COLOR_CONFIG = {
    // Status Colors (used across multiple controls)
    status: {
        normal: '#28a745',      // Green for good/normal status
        warning: '#ffc107',     // Yellow for warning status
        critical: '#dc3545',    // Red for critical/high status
        info: '#17a2b8'         // Blue for informational status
    },

    // Gradient Combinations
    gradients: {
        success: 'linear-gradient(90deg, #28a745, #20c997)',
        warning: 'linear-gradient(90deg, #ffc107, #fd7e14)',
        danger: 'linear-gradient(90deg, #dc3545, #c82333)',
        info: 'linear-gradient(90deg, #17a2b8, #138496)',
        primary: 'linear-gradient(45deg, #007bff, #0056b3)'
    },

    // Background Colors
    backgrounds: {
        primary: 'rgba(16, 16, 13, 0.9)',
        secondary: 'rgba(28, 51, 39, 0.8)',
        accent: 'rgba(28, 51, 39, 0.1)',
        overlay: 'rgba(16, 16, 13, 0.95)'
    },

    // Text Colors
    text: {
        primary: 'rgb(225, 221, 206)',      // Main headings and labels
        secondary: 'rgba(225, 221, 206, 0.8)', // Subheadings and descriptions
        muted: 'rgba(225, 221, 206, 0.6)',   // Muted text and placeholders
        accent: 'rgb(255, 215, 0)',          // Highlighted/important text
        success: 'rgb(40, 167, 69)',         // Success messages
        warning: 'rgb(255, 193, 7)',         // Warning messages
        danger: 'rgb(220, 53, 69)',          // Error messages
        info: 'rgb(23, 162, 184)'            // Informational text
    },

    // Typography Scale
    typography: {
        fontFamily: '"Segoe UI", Tahoma, Geneva, Verdana, sans-serif',
        fontSize: {
            xs: '10px',
            sm: '12px',
            md: '14px',
            lg: '16px',
            xl: '18px',
            xxl: '24px',
            xxxl: '32px'
        },
        fontWeight: {
            normal: '400',
            medium: '500',
            semibold: '600',
            bold: '700',
            extrabold: '800'
        }
    },

    // Border Colors
    borders: {
        primary: 'rgba(28, 51, 39, 0.8)',
        secondary: 'rgba(28, 51, 39, 0.5)',
        accent: 'rgba(28, 51, 39, 0.3)'
    }
};

// Utility functions for color manipulation
const ColorUtils = {
    // Lighten a color by percentage
    lighten: (color, percent) => {
        // Implementation for lightening colors
        return color; // Placeholder
    },

    // Darken a color by percentage
    darken: (color, percent) => {
        // Implementation for darkening colors
        return color; // Placeholder
    },

    // Get status color based on value and thresholds
    getStatusColor: (value, thresholds = { normal: 50, warning: 75 }) => {
        if (value < thresholds.normal) return COLOR_CONFIG.status.normal;
        if (value < thresholds.warning) return COLOR_CONFIG.status.warning;
        return COLOR_CONFIG.status.critical;
    },

    // Get gradient based on status
    getStatusGradient: (value, thresholds = { normal: 50, warning: 75 }) => {
        if (value < thresholds.normal) return COLOR_CONFIG.gradients.success;
        if (value < thresholds.warning) return COLOR_CONFIG.gradients.warning;
        return COLOR_CONFIG.gradients.danger;
    }
};

// Export for use in other files
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { COLOR_CONFIG, ColorUtils };
}