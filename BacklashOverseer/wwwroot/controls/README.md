# Visual Controls Color Customization Guide

## 🎨 **Yes! You Can Fully Customize Colors**

This modular control system provides multiple ways to customize colors across all visual controls.

## 📁 **Files Overview**

- **`color-config.js`** - Central color configuration system
- **`custom-theme-example.js`** - Example of creating custom themes
- **`badge.html`** & **`line-chart.html`** - Examples using the color system
- **Individual control files** - Each control can be customized independently

## 🎯 **Three Ways to Customize Colors**

### **Method 1: Global Theme (Recommended)**
```javascript
// 1. Create your custom theme file (copy custom-theme-example.js)
// 2. Modify colors in CUSTOM_COLOR_CONFIG
// 3. Include BEFORE control files:
<script src="your-custom-theme.js"></script>
<script src="controls/badge.html"></script>
```

### **Method 2: Direct CSS Override**
```html
<!-- Override specific colors in individual control files -->
<style>
.speed-dial-circle {
    background: conic-gradient(#ff6b6b 0% 60%, #ffd93d 60% 80%, #6bcf7f 80% 100%) !important;
}
</style>
```

### **Method 3: Dynamic Color Changes**
```javascript
// Change colors programmatically
function applyCustomColors() {
    COLOR_CONFIG.status.normal = '#00ff88';
    COLOR_CONFIG.gradients.success = 'linear-gradient(90deg, #00ff88, #00cc66)';
    // Controls will use new colors on next update
}
```

## 🎨 **Available Color Categories**

### **Status Colors**
```javascript
COLOR_CONFIG.status = {
    normal: '#28a745',    // Green - good status
    warning: '#ffc107',   // Yellow - warning status
    critical: '#dc3545',  // Red - critical status
    info: '#17a2b8'       // Blue - informational
}
```

### **Gradients**
```javascript
COLOR_CONFIG.gradients = {
    success: 'linear-gradient(90deg, #28a745, #20c997)',
    warning: 'linear-gradient(90deg, #ffc107, #fd7e14)',
    danger: 'linear-gradient(90deg, #dc3545, #c82333)',
    info: 'linear-gradient(90deg, #17a2b8, #138496)',
    primary: 'linear-gradient(45deg, #007bff, #0056b3)'
}
```

### **Backgrounds & UI**
```javascript
COLOR_CONFIG.backgrounds = {
    primary: 'rgba(16, 16, 13, 0.9)',
    secondary: 'rgba(28, 51, 39, 0.8)',
    accent: 'rgba(28, 51, 39, 0.1)',
    overlay: 'rgba(16, 16, 13, 0.95)'
}
```

### **Text Colors & Typography**
```javascript
COLOR_CONFIG.text = {
    primary: 'rgb(225, 221, 206)',      // Main headings and labels
    secondary: 'rgba(225, 221, 206, 0.8)', // Subheadings and descriptions
    muted: 'rgba(225, 221, 206, 0.6)',   // Muted text and placeholders
    accent: 'rgb(255, 215, 0)',          // Highlighted/important text
    success: 'rgb(40, 167, 69)',         // Success messages
    warning: 'rgb(255, 193, 7)',         // Warning messages
    danger: 'rgb(220, 53, 69)',          // Error messages
    info: 'rgb(23, 162, 184)'            // Informational text
}

COLOR_CONFIG.typography = {
    fontFamily: '"Segoe UI", Tahoma, Geneva, Verdana, sans-serif',
    fontSize: {
        xs: '10px', sm: '12px', md: '14px', lg: '16px',
        xl: '18px', xxl: '24px', xxxl: '32px'
    },
    fontWeight: {
        normal: '400', medium: '500', semibold: '600',
        bold: '700', extrabold: '800'
    }
}
```

## 🚀 **Quick Start Examples**

### **Create a Red Theme**
```javascript
const RED_THEME = {
    status: {
        normal: '#ff6b6b',
        warning: '#ff8e53',
        critical: '#ff4757',
        info: '#3742fa'
    },
    gradients: {
        success: 'linear-gradient(90deg, #ff6b6b, #ff3838)',
        warning: 'linear-gradient(90deg, #ff8e53, #ff6b35)',
        danger: 'linear-gradient(90deg, #ff4757, #ff3838)'
    }
};
Object.assign(COLOR_CONFIG, RED_THEME);
```

### **Create a Blue Theme**
```javascript
const BLUE_THEME = {
    status: {
        normal: '#4facfe',
        warning: '#00f2fe',
        critical: '#43e97b',
        info: '#38f9d7'
    },
    gradients: {
        success: 'linear-gradient(90deg, #4facfe, #00f2fe)',
        warning: 'linear-gradient(90deg, #00f2fe, #43e97b)',
        danger: 'linear-gradient(90deg, #43e97b, #38f9d7)'
    }
};
Object.assign(COLOR_CONFIG, BLUE_THEME);
```

### **Customize Text Colors**
```javascript
const TEXT_THEME = {
    text: {
        primary: '#ffffff',        // Pure white headings
        secondary: '#e0e0e0',      // Light gray subtext
        muted: '#a0a0a0',          // Medium gray muted text
        accent: '#ffd700',         // Gold highlights
        success: '#00ff88',        // Bright green success
        warning: '#ffaa00',        // Orange warnings
        danger: '#ff4444',         // Red errors
        info: '#44aaff'            // Light blue info
    },
    typography: {
        fontFamily: '"Roboto", sans-serif',
        fontSize: {
            lg: '18px',           // Larger headings
            md: '16px',           // Standard text
            sm: '14px'            // Smaller text
        }
    }
};
Object.assign(COLOR_CONFIG, TEXT_THEME);
```

### **High Contrast Theme**
```javascript
const HIGH_CONTRAST_THEME = {
    text: {
        primary: '#ffffff',
        secondary: '#ffff00',      // Yellow for better contrast
        muted: '#ff6b6b',          // Red for warnings
        accent: '#00ffff',         // Cyan for highlights
        success: '#00ff00',
        warning: '#ffff00',
        danger: '#ff0000',
        info: '#0080ff'
    },
    backgrounds: {
        primary: '#000000',
        secondary: '#1a1a1a',
        accent: '#333333'
    }
};
Object.assign(COLOR_CONFIG, HIGH_CONTRAST_THEME);
```

## 📋 **Controls That Support Color Customization**

✅ **Badge** - Status-based color changes
✅ **Line Chart** - Bar colors based on values
✅ **Progress Bar** - Fill color gradients
✅ **Speed Dial** - Background gradients
✅ **Traffic Light** - Status bulb colors
✅ **Pie Chart** - Segment colors
✅ **Numeric Display** - Response bar colors
✅ **Data Grid** - Status text colors
✅ **Counter** - Background colors

## 🔧 **Implementation Notes**

- Colors are applied dynamically during control updates
- Changes take effect immediately for most controls
- Some controls may need a refresh for static elements
- CSS custom properties can also be used for advanced theming

## 🎨 **Advanced Text Customization with CSS Variables**

For even more control, use CSS custom properties in individual controls:

```css
/* In any control's <style> section */
:root {
    --control-text-primary: #ffffff;
    --control-text-secondary: #cccccc;
    --control-font-family: 'Arial', sans-serif;
    --control-font-size-lg: 18px;
    --control-font-weight-bold: 700;
}

/* Apply to elements */
.control-title {
    color: var(--control-text-primary);
    font-family: var(--control-font-family);
    font-size: var(--control-font-size-lg);
    font-weight: var(--control-font-weight-bold);
}
```

## 📝 **Text Customization Checklist**

- ✅ **Primary text**: Headings, labels, important data
- ✅ **Secondary text**: Descriptions, sub-labels
- ✅ **Muted text**: Placeholders, timestamps, metadata
- ✅ **Status text**: Success, warning, error, info messages
- ✅ **Typography**: Font family, sizes, weights
- ✅ **Interactive text**: Hover states, focus states

## 🎯 **Per-Element Label Visibility Control**

Control label visibility for individual controls instead of global toggles:

### **Method 1: CSS Classes**
```html
<!-- Hide labels for specific controls -->
<div class="control-card speed-dial-control hide-labels">
    <div class="control-title">CPU Usage</div>        <!-- Hidden -->
    <div class="control-description">Description</div> <!-- Hidden -->
</div>

<div class="control-card progress-control">           <!-- Labels shown -->
    <div class="control-title">Memory</div>           <!-- Visible -->
    <div class="control-description">Description</div> <!-- Visible -->
</div>
```

### **Method 2: Data Attributes**
```html
<!-- More semantic approach -->
<div class="control-card" data-show-labels="false">
    <div class="control-title">Title</div>        <!-- Hidden -->
    <div class="control-description">Desc</div>  <!-- Hidden -->
</div>

<div class="control-card" data-show-labels="true">
    <div class="control-title">Title</div>        <!-- Visible -->
    <div class="control-description">Desc</div>  <!-- Visible -->
</div>
```

### **Method 3: Direct CSS Override**
```css
/* Hide labels for specific control types */
.speed-dial-control .control-title,
.speed-dial-control .control-description {
    opacity: 0;
    pointer-events: none;
}

/* Or show labels for specific controls */
.progress-control .control-title,
.progress-control .control-description {
    opacity: 1;
    pointer-events: auto;
}
```

## 🎯 **Best Practices**

1. **Test color combinations** for accessibility (contrast ratios)
2. **Use consistent color schemes** across related controls
3. **Document your custom themes** for team consistency
4. **Consider color-blind users** when choosing color schemes
5. **Test in different lighting conditions**

---

**Need help?** Check the example files and modify the `COLOR_CONFIG` object to match your brand colors!