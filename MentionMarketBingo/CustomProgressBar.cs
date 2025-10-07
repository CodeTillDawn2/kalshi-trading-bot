using System.Drawing;
using System.Windows.Forms;

namespace MentionMarketBingo;

public class CustomProgressBar : Control
{
    private int _value;
    private int _maximum = 100;
    private Color _barColor = Color.Green;

    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Max(0, Math.Min(_maximum, value));
            Invalidate(); // Force repaint
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(1, value);
            if (_value > _maximum) _value = _maximum;
            Invalidate();
        }
    }

    public Color BarColor
    {
        get => _barColor;
        set
        {
            _barColor = value;
            Invalidate();
        }
    }

    public CustomProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.LightGray;
        MinimumSize = new Size(20, 10);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Fill background
        e.Graphics.FillRectangle(Brushes.LightGray, 0, 0, Width, Height);

        // Draw filled portion in custom color
        if (_value > 0)
        {
            var fillWidth = (int)(Width * _value / (double)_maximum);
            using (var brush = new SolidBrush(_barColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, fillWidth, Height);
            }
        }

        // Draw border
        using (var pen = new Pen(Color.Black, 1))
        {
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}