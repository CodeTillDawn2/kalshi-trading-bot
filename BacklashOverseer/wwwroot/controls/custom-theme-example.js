// Example: Custom Color Theme
// Copy this file and modify the colors to create your own theme

const CUSTOM_COLOR_CONFIG = {
    // Custom Status Colors
    status: {
        normal: '#00ff88',      // Bright green
        warning: '#ffaa00',     // Orange
        critical: '#ff4444',    // Bright red
        info: '#4488ff'         // Bright blue
    },

    // Custom Gradients
    gradients: {
        success: 'linear-gradient(90deg, #00ff88, #00cc66)',
        warning: 'linear-gradient(90deg, #ffaa00, #ff8800)',
        danger: 'linear-gradient(90deg, #ff4444, #cc2222)',
        info: 'linear-gradient(90deg, #4488ff, #2266cc)',
        primary: 'linear-gradient(45deg, #aa66ff, #8844cc)'  // Purple theme
    },

    // Dark Theme Backgrounds
    backgrounds: {
        primary: 'rgba(10, 10, 10, 0.95)',
        secondary: 'rgba(20, 20, 20, 0.9)',
        accent: 'rgba(30, 30, 30, 0.8)',
        overlay: 'rgba(5, 5, 5, 0.98)'
    },

    // Custom Text Colors
    text: {
        primary: 'rgb(240, 240, 240)',      // Bright white for headings
        secondary: 'rgba(240, 240, 240, 0.8)', // Slightly transparent for subtext
        muted: 'rgba(240, 240, 240, 0.6)',   // More transparent for muted text
        accent: 'rgb(255, 215, 0)',          // Gold for highlights
        success: 'rgb(76, 175, 80)',         // Material green
        warning: 'rgb(255, 152, 0)',         // Material orange
        danger: 'rgb(244, 67, 54)',          // Material red
        info: 'rgb(33, 150, 243)'            // Material blue
    },

    // Custom Typography
    typography: {
        fontFamily: '"Roboto", "Segoe UI", Tahoma, Geneva, Verdana, sans-serif',
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

    // Subtle Borders
    borders: {
        primary: 'rgba(100, 100, 100, 0.8)',
        secondary: 'rgba(100, 100, 100, 0.5)',
        accent: 'rgba(100, 100, 100, 0.3)'
    }
};

// Apply custom theme by overriding default colors
Object.assign(COLOR_CONFIG, CUSTOM_COLOR_CONFIG);

/*
USAGE:
1. Copy this file to a new name (e.g., 'my-custom-theme.js')
2. Modify the color values above to your preference
3. Include this file BEFORE your control files:
   <script src="my-custom-theme.js"></script>
   <script src="controls/badge.html"></script>

This will override the default colors with your custom theme!
*/