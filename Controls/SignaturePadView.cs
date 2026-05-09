using Microsoft.Maui.Graphics;

namespace TestHub.Controls;

/// <summary>
/// Lightweight signature pad backed by <see cref="GraphicsView"/>. The
/// user draws strokes by dragging a finger / pointer; the resulting
/// strokes can be cleared and rotated as a whole.
/// </summary>
public class SignaturePadView : GraphicsView
{
    private readonly SignatureDrawable _drawable = new();
    private List<PointF>? _currentStroke;

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(SignaturePadView),
            Colors.Black, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty StrokeWidthProperty =
        BindableProperty.Create(nameof(StrokeWidth), typeof(float), typeof(SignaturePadView),
            2.5f, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty RotationDegreesProperty =
        BindableProperty.Create(nameof(RotationDegrees), typeof(float), typeof(SignaturePadView),
            0f, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty IsEmptyProperty =
        BindableProperty.Create(nameof(IsEmpty), typeof(bool), typeof(SignaturePadView), true,
            BindingMode.OneWayToSource);

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public float StrokeWidth
    {
        get => (float)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public float RotationDegrees
    {
        get => (float)GetValue(RotationDegreesProperty);
        set => SetValue(RotationDegreesProperty, value);
    }

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        private set => SetValue(IsEmptyProperty, value);
    }

    public SignaturePadView()
    {
        BackgroundColor = Colors.White;
        Drawable = _drawable;

        StartInteraction += OnStartInteraction;
        DragInteraction  += OnDragInteraction;
        EndInteraction   += OnEndInteraction;
    }

    /// <summary>Removes every stroke and resets rotation.</summary>
    public void Clear()
    {
        _drawable.Strokes.Clear();
        _currentStroke = null;
        RotationDegrees = 0f;
        _drawable.Rotation = 0f;
        IsEmpty = true;
        Invalidate();
    }

    /// <summary>Rotates the entire signature by the given delta (degrees).</summary>
    public void RotateBy(float deltaDegrees)
    {
        var rot = (RotationDegrees + deltaDegrees) % 360f;
        if (rot < 0) rot += 360f;
        RotationDegrees = rot;
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        _currentStroke = new List<PointF>();
        if (e.Touches.Length > 0)
        {
            _currentStroke.Add(e.Touches[0]);
        }
        _drawable.Strokes.Add(_currentStroke);
        IsEmpty = false;
        Invalidate();
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (_currentStroke is null || e.Touches.Length == 0)
        {
            return;
        }

        var p = e.Touches[0];
        // Skip duplicates so the path stays clean.
        if (_currentStroke.Count == 0 ||
            DistanceSquared(_currentStroke[^1], p) > 0.25f)
        {
            _currentStroke.Add(p);
            Invalidate();
        }
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        _currentStroke = null;
    }

    private static float DistanceSquared(PointF a, PointF b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SignaturePadView pad)
        {
            pad._drawable.StrokeColor = pad.StrokeColor;
            pad._drawable.StrokeWidth = pad.StrokeWidth;
            pad._drawable.Rotation   = pad.RotationDegrees;
            pad.Invalidate();
        }
    }
}

internal sealed class SignatureDrawable : IDrawable
{
    public List<List<PointF>> Strokes { get; } = new();
    public Color StrokeColor { get; set; } = Colors.Black;
    public float StrokeWidth { get; set; } = 2.5f;
    public float Rotation { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cx = dirtyRect.Width  / 2f;
        var cy = dirtyRect.Height / 2f;

        canvas.SaveState();
        canvas.Translate(cx, cy);
        canvas.Rotate(Rotation);
        canvas.Translate(-cx, -cy);

        canvas.StrokeColor    = StrokeColor;
        canvas.StrokeSize     = StrokeWidth;
        canvas.StrokeLineCap  = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.Antialias      = true;

        foreach (var stroke in Strokes)
        {
            if (stroke.Count == 0)
            {
                continue;
            }

            if (stroke.Count == 1)
            {
                canvas.FillColor = StrokeColor;
                canvas.FillCircle(stroke[0].X, stroke[0].Y, StrokeWidth / 2f);
                continue;
            }

            var path = new PathF();
            path.MoveTo(stroke[0]);
            for (int i = 1; i < stroke.Count; i++)
            {
                path.LineTo(stroke[i]);
            }
            canvas.DrawPath(path);
        }

        canvas.RestoreState();
    }
}
