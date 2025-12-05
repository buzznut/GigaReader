//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using UtilitiesLibrary;
using Clipboard = System.Windows.Forms.Clipboard;
using Rectangle = System.Drawing.Rectangle;

namespace HugeFileReader;

[SupportedOSPlatform("windows6.1")]
public partial class TextControl : UserControl
{
    [Category("Behavior")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int TabSize { get; set; } = 4;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public long Rows { get { return (lines?.Rows).GetValueOrDefault(); } }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Cols { get { return (lines?.Cols).GetValueOrDefault(); } }

    public const string CursorKey = "TextControl.Cursor";
    public const string ScreenKey = "TextControl.Screen";
    public static readonly Dictionary<string, Type> CursorKeyTypes = new Dictionary<string, Type>()
    {
        { CursorKey, typeof(long[]) },
        { ScreenKey, typeof(long[]) },
    };

    public long CursorRow;
    public int CursorCol;
    public int ScreenCols;
    public int ScreenRows;
    public long ScreenStartRow;
    public int ScreenStartCol;
    public float FontWidthPixels;
    public float FontHeightPixels;
    public int DisplayWidth;
    public int DisplayHeight;
    public long SelectionAnchorRow = -1;
    public int SelectionAnchorCol = -1;
    public long SelectionReleaseRow = -1;
    public int SelectionReleaseCol = -1;
    public bool SearchDown = true;
    public bool UseCase;
    public string TextToFind;
    public const long MaxClipboardRows = 1024 * 1024;

    //[DllImport("user32.dll")]
    //public static extern int GetKeyboardState(byte[] lpKeyState);

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    private Lines lines;
    private Brush brushNormalBackground;
    private Brush brushNormalText;
    private Brush brushSelectionBackground;
    private Brush brushSelectionText;
    private bool wasHandled;
    private bool controlKeyDown;
    private bool shiftKeyDown;
    private Rectangle oldRect = new Rectangle(0, 0, 0, 0);
    private bool cursorIsOn = true;
    private Pen cursorPen = new Pen(Color.Black, 1);
    private bool searchFound = false;
    private int searchCol = -1;
    private long searchRow = -1;
    private readonly ConcurrentDictionary<string, object> state = new();
    private RectangleF tcRect;
    private bool initd = false;

    public TextControl()
    {
        SuspendLayout();

        InitializeComponent();

        DoubleBuffered = true;

        vScrollBar.Visible = false;
        hScrollBar.Visible = false;
        Padding = new Padding(0, 0, 0, 0);

        brushNormalBackground = new SolidBrush(BackColor);
        brushNormalText = new SolidBrush(ForeColor);
        brushSelectionBackground = new SolidBrush(SystemColors.Highlight);
        brushSelectionText = new SolidBrush(SystemColors.HighlightText);

        RegisterStateHandler(LineProgressChanged, Lines.LinesProgressKey);
        RegisterStateHandler(SearchFound, Search.FinishedKey);
        RegisterStateHandler(SearchCursor, Search.SearchCursorKey);
        RegisterStateHandler(LinesLoaded, Lines.LinesLoadedKey);

        DisplayHeight = Height;
        DisplayWidth = Width;

        Clear();

        ResumeLayout(true);

        initd = true;
        CalculateScreenDimensions();
    }

    private void SearchCursor(KeyValuePair<string, object> state)
    {
        if (state.Value is long[] rc && rc != null && rc.Length == 2)
        {
            searchRow = rc[0];
            searchCol = (int)rc[1];
            SetSearchResult();
        }
        else
        {
            searchRow = -1;
            searchCol = -1;
        }
    }

    private void SearchFound(KeyValuePair<string, object> state)
    {
        if (state.Value is bool f)
        {
            searchFound = f;
            SetSearchResult();
        }
        else
        {
            searchFound = false;
        }
    }

    private void SetSearchResult()
    {
        if (!searchFound || searchRow == -1 || searchCol == -1)
        {
            return;
        }

        long srow = CursorRow;
        int scol = CursorCol;
        int oldScreenCol = ScreenStartCol;
        long oldScreenRow = ScreenStartRow;

        GoToLine(searchRow, false);
        CursorCol = searchCol;

        HandleCursorChange(srow, scol, oldScreenRow, oldScreenCol);
    }

    private void LineProgressChanged(KeyValuePair<string, object> _)
    {
        if (lines != null && lines.Rows > ScreenRows)
        {
            InvalidateText();
        }
    }

    private void LinesLoaded(KeyValuePair<string, object> kvp)
    {
        bool loaded = kvp.Value is bool f && f;
        if (loaded)
        {
            CalculateScreenDimensions();
            state[CursorKey] = new long[] { CursorRow, CursorCol };
            state[ScreenKey] = new long[] { ScreenStartRow, ScreenStartCol };
        }
    }

    private bool ScrollBarEnable()
    {
        if (lines == null) return false;

        bool oldVSVisible = vScrollBar.Visible;
        bool oldHSVisible = hScrollBar.Visible;

        if (lines.Rows > ScreenRows)
        {
            if (!oldVSVisible)
            {
                vScrollBar.Visible = true;
                CalculateScollMaximum(vScrollBar, lines.Rows, ScreenRows);
            }
        }
        else
        {
            if (oldVSVisible)
            {
                vScrollBar.Visible = false;
            }
        }

        if (lines.Cols > ScreenCols)
        {
            if (!oldVSVisible)
            {
                hScrollBar.Visible = true;
                CalculateScollMaximum(hScrollBar, lines.Cols, ScreenCols);
            }
        }
        else
        {
            if (oldVSVisible)
            {
                hScrollBar.Visible = false;
            }
        }

        return oldVSVisible != vScrollBar.Visible || oldHSVisible && hScrollBar.Visible;
    }

    private static void CalculateScollMaximum(ScrollBar sb, long value, int pageSize)
    {
        int i = 1;
        long m = value;
        while (m >= int.MaxValue)
        {
            m = m / 10;
            i *= 10;
        }

        int max = (int)m;

        sb.Maximum = max;
        sb.SmallChange = i;
        sb.LargeChange = Math.Max(pageSize, max / 1000);
    }

    public delegate void StateHandler(KeyValuePair<string, object> state);
    private Dictionary<string, List<StateHandler>> stateHandlers = new();
    public void RegisterStateHandler(StateHandler stateHandler, string key)
    {
        if (!stateHandlers.TryGetValue(key, out List<StateHandler> value))
        {
            value = new List<StateHandler>();
            stateHandlers[key] = value;
        }

        value.Add(stateHandler);
    }

    private void stateHandler_Tick(object sender, EventArgs e)
    {
        string[] keys = state.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            if (state.TryRemove(key, out object value))
            {
                if (stateHandlers.TryGetValue(key, out List<StateHandler> handlers))
                {
                    foreach (StateHandler sh in handlers)
                    {
#pragma warning disable CS0168 // Variable is declared but never used
                        try
                        {
                            sh(new KeyValuePair<string, object>(key, value));
                        }
                        catch (Exception ex)
                        {
                            if (Debugger.IsAttached)
                            {
                                Debugger.Break();
                            }
                        }
#pragma warning restore CS0168 // Variable is declared but never used
                    }
                }
            }
        }
    }

    public void Open(string file)
    {
        Clear();
        lines = new Lines(state);
        lines.Load(file);
        Focus();
    }

    public void Clear()
    {
        if (lines == null) return;

        SuspendLayout();

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = SystemColors.Control;
        ForeColor = SystemColors.ControlText;
        Font = new Font("Consolas", 10f);

        lines?.StopSearch();
        lines?.StopLoad();
        lines?.Dispose();
        lines = null;
        CursorRow = 0;
        CursorCol = 0;
        ScreenStartRow = 0;
        ScreenStartCol = 0;
        ClearSelection();
        SearchDown = true;
        UseCase = false;
        TextToFind = null;
        vScrollBar.Visible = false;
        hScrollBar.Visible = false;

        ResumeLayout(true);

        Invalidate(true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        Graphics g = e.Graphics;
        g.FillRectangle(brushNormalBackground, e.ClipRectangle);
    }

    private void SizeChangedEvent(object sender, EventArgs e)
    {
        CalculateScreenDimensions();
    }

    private void MouseClickEvent(object sender, MouseEventArgs e)
    {
    }

    private bool CalculateFontWidth()
    {
        float oldFontHeight = FontHeightPixels;
        float oldFontWidth = FontWidthPixels;

        using (Graphics g = CreateGraphics())
        {
            string ms = "Now is the time for all good men to come to the aid of their country.";
            ms += " The quick brown fox jumped over the lazy dog's back.";
            ms += " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789,./?><';:][}{\\|=-+_])(*&^%$#@!~`\"";
            SizeF xx = g.MeasureString(ms, Font);

            FontHeightPixels = xx.Height;
            FontWidthPixels = xx.Width / ms.Length;
        }

        return oldFontHeight != FontHeightPixels || oldFontWidth != FontWidthPixels;
    }

    private void FontChangedEvent(object sender, EventArgs e)
    {
#pragma warning disable CS0168 // Variable is declared but never used
        try
        {
            CalculateScreenDimensions();
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached) Debugger.Break();
            throw;
        }
#pragma warning restore CS0168 // Variable is declared but never used
    }

    public void CalculateScreenDimensions()
    {
        if (!initd) return;
        bool changed = CalculateFontWidth();

        changed |= ScrollBarEnable();

        int oldDisplayWidth = DisplayWidth;
        int oldDisplayHeight = DisplayHeight;

        int vsbWidth = vScrollBar.Visible ? vScrollBar.Width : 0;
        int hsbHeight = hScrollBar.Visible ? hScrollBar.Height : 0;

        DisplayWidth = Width - vsbWidth;
        DisplayHeight = Height - hsbHeight;

        changed |= oldDisplayWidth != DisplayWidth || oldDisplayHeight != DisplayHeight;

        int oldScreenRows = ScreenRows;
        int oldScreenCols = ScreenCols;

        ScreenCols = Convert.ToInt32(Math.Truncate(DisplayWidth / FontWidthPixels));
        ScreenRows = Convert.ToInt32(Math.Truncate(DisplayHeight / FontHeightPixels));

        if (ScreenCols < 0) ScreenCols = 0;
        if (ScreenRows < 0) ScreenRows = 0;

        changed |= oldScreenRows != ScreenRows || oldScreenCols != ScreenCols;

        if (changed)
        {
            tcRect = new RectangleF(0, 0, DisplayWidth, DisplayHeight);
            InvalidateText();
        }
    }

    private void PaintEvent(object sender, PaintEventArgs e)
    {
#pragma warning disable CS0168 // Variable is declared but never used
        try
        {
            if (lines == null || lines.Rows < ScreenStartRow + ScreenRows)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            Graphics g = e.Graphics;

            RectangleF rc = RectangleF.Intersect(e.ClipRectangle, tcRect);
            if (rc.IsEmpty) return;

            long row = (long)MathF.Truncate(rc.Top / FontHeightPixels) - 1;
            int rows = (int)MathF.Truncate(rc.Height / FontHeightPixels) + 2;
            int col = (int)MathF.Truncate(rc.Left / FontWidthPixels) - 1;
            int cols = (int)MathF.Truncate(rc.Width / FontWidthPixels) + 2;

            if (row < 0)
            {
                row = 0;
                rows--;
            }

            if (col < 0)
            {
                col = 0;
                cols--;
            }

            if (row >= (lines.Rows - ScreenStartRow)) row = lines.Rows - ScreenStartRow;
            if (col >= (lines.Cols - ScreenStartCol)) col = lines.Cols - ScreenStartCol;

            long sr;
            int sc;
            long er;
            int ec;

            if (SelectionAnchorRow > SelectionReleaseRow || (SelectionAnchorRow == SelectionReleaseRow && SelectionAnchorCol > SelectionReleaseCol))
            {
                sr = SelectionReleaseRow;
                sc = SelectionReleaseCol;
                er = SelectionAnchorRow;
                ec = SelectionAnchorCol;
            }
            else
            {
                sr = SelectionAnchorRow;
                sc = SelectionAnchorCol;
                er = SelectionReleaseRow;
                ec = SelectionReleaseCol;
            }

            string line;
            for (int ii = 0; ii <= rows + 1; ii++)
            {
                long r = ii + row;

                float y = r * FontHeightPixels;
                bool useSpaceRow = false;

                long textRow = r + ScreenStartRow;
                if (textRow >= lines.Rows) continue;

                line = lines[textRow];
                if (line == null)
                {
                    InvalidateText();
                    return;
                }

                line = line.Replace("\t", " ".PadRight(TabSize));

                Brush lastTextBrush = brushNormalText;
                Brush lastBackground = brushNormalBackground;
                float xLast = -1;
                float xStart = 0;

                bool inSelectionRow = sr != -1 && textRow >= sr && textRow <= er;

                sb.Clear();
                for (int jj = 0; jj <= cols + 1; jj++)
                {
                    int c = col + jj;
                    float x = c * FontWidthPixels;

                    Brush brBackground = brushNormalBackground;
                    Brush brText = brushNormalText;

                    int textCol = c + ScreenStartCol;
                    if (textCol > lines.Cols) continue;

                    char ch = ' ';
                    if (textCol < line.Length && textCol >= 0)
                    {
                        if (inSelectionRow)
                        {
                            // special case for the start and end of the selection
                            if ((textRow != sr || textCol >= sc) && (textRow != er || textCol < ec))
                            {
                                brText = brushSelectionText;
                                brBackground = brushSelectionBackground;
                            }
                        }

                        ch = GetChar(line, textCol);
                    }

                    if (useSpaceRow || x > DisplayWidth - FontWidthPixels - (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) || x < 0)
                    {
                        ch = ' ';
                    }

                    if (xLast.CompareTo(-1F) == 0)
                    {
                        //brush = brText;
                        //lastBackground = brBackground;
                        xLast = x;
                        xStart = 0;
                    }

                    if (lastTextBrush == brText && lastBackground == brBackground)
                    {
                        sb.Append(ch);
                    }
                    else
                    {
                        // draw what was accumulated
                        g.FillRectangle(lastBackground, xStart, y, (FontWidthPixels * sb.Length), FontHeightPixels);

                        DrawStringByCharacter(sb, g, y, lastTextBrush, xLast);

                        xLast = x;
                        xStart = x;
                        lastTextBrush = brText;
                        lastBackground = brBackground;

                        sb.Clear();
                        sb.Append(ch);
                    }
                }

                if (sb.Length > 0)
                {
                    g.FillRectangle(lastBackground, xLast, y, (FontWidthPixels * sb.Length), FontHeightPixels);

                    DrawStringByCharacter(sb, g, y, lastTextBrush, xLast);
                }
            }

            bool showCursor = cursorIsOn &&
                CursorRow >= ScreenStartRow && CursorRow < ScreenStartRow + ScreenRows &&
                CursorCol >= ScreenStartCol && CursorCol < ScreenStartCol + ScreenCols;

            if (showCursor)
            {
                float x = (CursorCol - ScreenStartCol) * FontWidthPixels + 2;
                float y = (CursorRow - ScreenStartRow) * FontHeightPixels + 2;
                PointF topPoint = new PointF(x, y);
                PointF bottomPoint = new PointF(x, y + FontHeightPixels * 0.7F);

                g.DrawLine(cursorPen, topPoint, bottomPoint);
            }

            //RectangleF foo = new RectangleF(rc.Left, rc.Top, rc.Width - 1, rc.Height - 1);
            //g.DrawRectangle(Pens.Red, foo);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached) Debugger.Break();
        }
#pragma warning restore CS0168 // Variable is declared but never used
    }

    private void DrawStringByCharacter(StringBuilder sb, Graphics g, float y, Brush brush, float xLast)
    {
        float xpart = xLast;
        foreach (char cpart in sb.ToString())
        {
            g.DrawString(cpart.ToString(), Font, brush, xpart, y);
            xpart += FontWidthPixels;
        }
    }

    private void KeyDownEvent(object sender, KeyEventArgs e)
    {
        if (e.Shift)
        {
            shiftKeyDown = true;
        }

        if (e.Control)
        {
            controlKeyDown = true;
        }

        e.Handled = wasHandled;
        e.SuppressKeyPress = wasHandled;
        wasHandled = false;
    }

    private bool ProcessKey(KeyEventArgs e)
    {
        if (Rows == 0) return false;

        controlKeyDown = e.Control;
        shiftKeyDown = e.Shift;

        switch (e.KeyCode)
        {
            case Keys.C:
            {
                if (controlKeyDown)
                {
                    ToClipboard();
                    return true;
                }
                break;
            }

            case Keys.Left:
            {
                if (controlKeyDown)
                {
                    WordLeftRight(false, true);
                    return true;
                }

                CursorLeftRight(-1, true);
                return true;
            }

            case Keys.Right:
            {
                if (controlKeyDown)
                {
                    WordLeftRight(true, true);
                    return true;
                }

                CursorLeftRight(1, true);
                return true;
            }

            case Keys.Up:
            {
                if (controlKeyDown)
                {
                    ScrollRowUpDown(1);
                    return true;
                }

                CursorUpDown(-1, true);
                return true;
            }

            case Keys.Down:
            {
                if (controlKeyDown)
                {
                    ScrollRowUpDown(-1);
                    return true;
                }

                CursorUpDown(1, true);
                return true;
            }

            case Keys.PageUp:
            {
                if (controlKeyDown)
                {
                    long rowsToMove = ScreenStartRow - CursorRow;
                    CursorUpDown(rowsToMove, true);
                    return true;
                }

                CursorUpDown(-ScreenRows, true);
                return true;
            }

            case Keys.PageDown:
            {
                if (controlKeyDown)
                {
                    long rowsToMove = ScreenStartRow + ScreenRows - CursorRow - 1;
                    CursorUpDown(rowsToMove, true);
                    return true;
                }

                CursorUpDown(ScreenRows, true);
                return true;
            }

            case Keys.Home:
            {
                if (controlKeyDown)
                {
                    GoToHome(true);
                    return true;
                }

                LineBegin(true);
                return true;
            }

            case Keys.End:
            {
                if (controlKeyDown)
                {
                    GoToEnd(true);
                    return true;
                }

                LineEnd(true);
                return true;
            }

            case Keys.F3:
            {
                SearchDown = !shiftKeyDown;
                SearchAgain();
                return true;
            }

            case Keys.F:
            {
                if (controlKeyDown)
                {
                    Find();
                    return true;
                }
                break;
            }

            case Keys.G:
            {
                if (controlKeyDown)
                {
                    AskGoTo();
                    return true;
                }
                break;
            }

            case Keys.Escape:
            {
                if (SelectionAnchorRow != -1)
                {
                    SelectionAnchorRow = -1;
                    InvalidateText();
                    return true;
                }
                break;
            }

            case Keys.A:
            {
                if (controlKeyDown && lines != null)
                {
                    SelectionAnchorRow = 0;
                    SelectionAnchorCol = 0;
                    SelectionReleaseRow = lines.Rows - 1;
                    SelectionReleaseCol = lines.Cols;
                    InvalidateText();
                    return true;
                }
                break;
            }
        }

        return false;
    }

    private void WordLeftRight(bool goRight, bool handleCursorChange)
    {
        long oldRow = CursorRow;
        int oldCol = CursorCol;
        int oldStartCol = ScreenStartCol;
        long oldStartRow = ScreenStartRow;

        GetWord(goRight);

        if (handleCursorChange) HandleCursorChange(oldRow, oldCol, oldStartRow, oldStartCol);
    }

    /*
    X-GM-THRID: 1834746907652177196
    X-Gmail-Labels: Important,Trash,Opened,Category Updates
    */
    private void GetWord(bool goRight)
    {
        string line = GetLine(CursorRow);
        if (line == null) return;

        int addValue = goRight ? 1 : -1;
        //if (goRight)
        //{
        //    // moving right
        //    addValue = 1;
        //    if (CursorCol >= line.Length)
        //    {
        //        CursorLeftRight(addValue, false);
        //        return;
        //    }
        //}
        //else
        //{
        //    // moving left
        //    addValue = -1;
        //    CursorCol--;
        //    if (CursorCol < 0)
        //    {
        //        CursorLeftRight(addValue, false);
        //        return;
        //    }
        //}

        char ch = GetChar(line, CursorCol);
        if (!char.IsLetterOrDigit(ch))
        {
            // move until we find a letter/digit
            while (CursorCol < lines.Cols && CursorCol >= 0 && !char.IsLetterOrDigit(GetChar(line, CursorCol)))
            {
                CursorCol += addValue;
            }
        }
        else
        {
            // move until we find a non-space character
            while (CursorCol < lines.Cols && CursorCol >= 0 && char.IsLetterOrDigit(GetChar(line, CursorCol)))
            {
                CursorCol += addValue;
            }
        }

        CursorCol += goRight ? 0 : 1;
    }

    private static char GetChar(string line, int cursorCol)
    {
        return (cursorCol >= 0 && cursorCol < line.Length) ? line[cursorCol] : ' ';
    }

    private void CursorLeftRight(int rightLeftQty, bool handleCursorChange)
    {
        long oldRow = CursorRow;
        int oldColumn = CursorCol;
        long oldStartRow = ScreenStartRow;
        int oldStartColumn = ScreenStartCol;

        CursorCol += rightLeftQty;

        if (CursorCol < 0)
        {
            if (controlKeyDown)
            {
                CursorUpDown(-1, false);
                LineEnd(false);
                WordLeftRight(false, false);
            }
            else
            {
                CursorCol = 0;
            }
        }
        else if (CursorCol >= lines.Cols)
        {
            if ((Rows - ScreenStartRow - ScreenRows) > (CursorRow + 1))
            {
                if (controlKeyDown)
                {
                    CursorUpDown(1, false);
                    LineBegin(false);
                }
                else
                {
                    CursorCol = lines.Cols - 1;
                }
            }
            else
            {
                CursorCol -= rightLeftQty;
            }
        }

        if (handleCursorChange) HandleCursorChange(oldRow, oldColumn, oldStartRow, oldStartColumn);
    }

    public void Find()
    {
        if (lines == null) return;

        FindText ft = new FindText();
        if (ft.ShowDialog() == DialogResult.OK)
        {
            searchFound = false;
            searchCol = -1;
            searchRow = -1;

            TextToFind = ft.SearchText;
            lines.StartSearch(CursorRow, CursorCol, ft.UseCase, ft.SearchDown, TextToFind);
        }
    }

    private void AskGoTo()
    {
        Goto gt = new Goto(lines.Rows);
        if (gt.ShowDialog() == DialogResult.OK)
        {
            long row;
            if (long.TryParse(gt.GetLine(), out row))
            {
                GoToLine(row - 1, true);
            }
        }
    }

    private void PreviewKeyDownEvent(object sender, PreviewKeyDownEventArgs e)
    {
        KeyEventArgs k = new KeyEventArgs(e.KeyData);
        wasHandled = ProcessKey(k);
        k.Handled = wasHandled;
    }

    private void MouseWheelEvent(object sender, MouseEventArgs e)
    {
        // Update the drawing based upon the mouse wheel scrolling.
        int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
        ScrollRowUpDown(numberOfTextLinesToMove);
    }

    private void GoToHome(bool handleCursorChange)
    {
        long oldRow = CursorRow;
        int oldCol = CursorCol;
        int oldStartCol = ScreenStartCol;
        long oldStartRow = ScreenStartRow;

        CursorRow = 0;
        CursorCol = 0;
        ScreenStartCol = 0;
        ScreenStartRow = 0;

        if (handleCursorChange) HandleCursorChange(oldRow, oldCol, oldStartRow, oldStartCol);
    }

    private void GoToEnd(bool handleCursorChange)
    {
        long oldRow = CursorRow;
        int oldCol = CursorCol;
        int oldStartCol = ScreenStartCol;
        long oldStartRow = ScreenStartRow;

        CursorRow = Rows - 1;
        LineEnd(false);
        ScreenStartRow = Rows - ScreenRows;

        if (handleCursorChange) HandleCursorChange(oldRow, oldCol, oldStartRow, oldStartCol);
    }

    public void GoToLine(long row, bool handleCursorChange)
    {
        long oldRow = CursorRow;
        int oldCol = CursorCol;
        long oldStartRow = ScreenStartRow;
        int oldStartCol = ScreenStartCol;

        if (row >= lines.Rows) { row = lines.Rows - 1; }

        CursorRow = row;
        LineBegin(false);
        ScreenStartRow = row - ScreenRows / 2 + 1;
        if (ScreenStartRow < 0)
        {
            ScreenStartRow = 0;
        }

        if (handleCursorChange) HandleCursorChange(oldRow, oldCol, oldStartRow, oldStartCol);
    }

    private void LineBegin(bool handleCursorChange)
    {
        long oldRow = CursorRow;
        int oldCol = CursorCol;
        int oldStartCol = ScreenStartCol;
        long oldStartRow = ScreenStartRow;

        CursorCol = 0;
        if (CursorCol < ScreenStartCol)
        {
            ScreenStartCol = 0;
        }

        if (handleCursorChange) HandleCursorChange(oldRow, oldCol, oldStartRow, oldStartCol);
    }

    private void LineEnd(bool handleCursorChange)
    {
        long oldRow = CursorRow;
        int oldCol = CursorCol;
        int oldStartCol = ScreenStartCol;
        long oldStartRow = ScreenStartRow;

        string line = GetLine(CursorRow);
        CursorCol = line == null ? 0 : line.Length;

        if (handleCursorChange) HandleCursorChange(oldRow, oldCol, oldStartRow, oldStartCol);
    }

    private string GetLine(long cursorRow)
    {
        if (lines == null) return string.Empty;
        string text = lines[cursorRow];
        if (text == null) return null;
        return text.Replace("\t", "".PadRight(TabSize));
    }


    private void CursorUpDown(long rowsToMoveCursor, bool handleCursorChange)
    {
        long oldRow = CursorRow;
        int oldCol = CursorCol;
        long oldStartRow = ScreenStartRow;
        int oldStartCol = ScreenStartCol;

        CursorRow += rowsToMoveCursor;

        if (CursorRow < 0)
        {
            CursorRow = 0;
        }
        else if (CursorRow >= Rows)
        {
            CursorRow = Rows - 1;
        }

        if (CursorCol >= lines.Cols)
        {
            CursorCol = lines.Cols - 1;
        }

        if (handleCursorChange) HandleCursorChange(oldRow, oldCol, oldStartRow, oldStartCol);
    }

    private void KeyUpEvent(object sender, KeyEventArgs e)
    {
        //string txt = string.Format ("KeyUp: code={0}, data={1}, value={2}, modifiers={3}", state.KeyCode, state.KeyData, state.KeyValue, state.Modifiers);
        //Debug.WriteLine (txt);

        if (!e.Shift)
        {
            shiftKeyDown = false;
        }

        if (!e.Control)
        {
            controlKeyDown = false;
        }

        e.Handled = wasHandled;
        e.SuppressKeyPress = wasHandled;
    }

    private void ScrollRowUpDown(int linesToMove)
    {
        if (lines == null || lines.Rows == 0) return;

        long oldScreenStartRow = ScreenStartRow;

        ScreenStartRow -= linesToMove;

        if (ScreenStartRow >= Rows - ScreenRows)
        {
            ScreenStartRow = Rows - ScreenRows;
        }

        if (ScreenStartRow < 0)
        {
            ScreenStartRow = 0;
        }

        HandleScroll(oldScreenStartRow, ScreenStartCol);
    }

    private void HandleScroll(long oldScreenStartRow, int oldScreenStartCol)
    {
        if (oldScreenStartRow != ScreenStartRow || oldScreenStartCol != ScreenStartCol)
        {
            InvalidateText();
            state[ScreenKey] = new long[] { ScreenStartRow, ScreenStartCol };
        }
    }

    private void ScrollLeftRight(int columnsToMove)
    {
        if (lines == null || lines.Rows == 0) return;
        int oldScreenStartCol = ScreenStartCol;

        ScreenStartCol -= columnsToMove;

        int maxCols = Cols - ScreenCols;
        if (maxCols < 0) maxCols = 0;

        if (ScreenStartCol > maxCols)
        {
            ScreenStartCol = Cols - ScreenCols;
        }

        if (ScreenStartCol < 0)
        {
            ScreenStartCol = 0;
        }

        HandleScroll(ScreenStartRow, oldScreenStartCol);
    }

    public void ToClipboard()
    {
        if (Rows == 0) return;

        long sr;
        int sc;
        long er;
        int ec;

        if (SelectionAnchorRow > SelectionReleaseRow ||
            (SelectionAnchorRow == SelectionReleaseRow && SelectionAnchorCol > SelectionReleaseCol))
        {
            sr = SelectionReleaseRow;
            sc = SelectionReleaseCol;
            er = SelectionAnchorRow;
            ec = SelectionReleaseCol;
        }
        else
        {
            sr = SelectionAnchorRow;
            sc = SelectionReleaseCol;
            er = SelectionReleaseRow;
            ec = SelectionReleaseCol;
        }

        long totalLines = er - sr;

        if (totalLines > MaxClipboardRows)
        {
            MessageBox.Show(this, $"Too many rows selected. (rows = 1..{FormatWithSuffix(MaxClipboardRows)})", "Selection Too Large", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        StringBuilder sb = new StringBuilder();
        string line;

        for (long ii = sr; ii <= er; ii++)
        {
            if (ii < 0) continue;

            int cs = ii == sr ? sc : 0;

            line = lines[ii];

            int ce = ii == er ? ec : int.MaxValue;

            if (ce > line.Length)
            {
                ce = line.Length;
            }

            sb.AppendLine(line.Substring(cs, ce - cs));
        }

        if (sb.Length > 0)
        {
            Clipboard.SetDataObject(sb.ToString(), true);
        }
    }

    private void HandleCursorChange(long oldRow, int oldCol, long oldStartRow, int oldStartCol)
    {
        if (Rows == 0) return;

        cursorIsOn = false;
        InvalidateCursorPosition(oldRow, oldCol);

        bool invalidatePage = oldStartRow != ScreenStartRow || oldStartCol != ScreenStartCol;

        if (shiftKeyDown)
        {
            if (SelectionAnchorRow == -1)
            {
                SelectionAnchorRow = oldRow;
                SelectionReleaseCol = oldCol;
            }

            SelectionReleaseRow = CursorRow;
            SelectionReleaseCol = CursorCol;

            invalidatePage = true;
        }
        else
        {
            if (SelectionAnchorRow != -1)
            {
                SelectionAnchorRow = -1;
                invalidatePage = true;
            }
        }

        if (CursorRow < ScreenStartRow)
        {
            ScreenStartRow = CursorRow;
        }
        else if (CursorRow >= ScreenStartRow + ScreenRows - 1)
        {
            ScreenStartRow = CursorRow - ScreenRows + 1;
        }

        if (ScreenStartRow < 0)
        {
            ScreenStartRow = 0;
        }

        if (CursorCol < ScreenStartCol)
        {
            ScreenStartCol = CursorCol;
        }
        else if (CursorCol >= ScreenStartCol + ScreenCols)
        {
            ScreenStartCol = CursorCol - ScreenCols + 1;
        }

        if (ScreenStartCol < 0)
        {
            ScreenStartCol = 0;
        }

        if (oldStartRow != ScreenStartRow || oldStartCol != ScreenStartCol)
        {
            invalidatePage = true;
            state[ScreenKey] = new long[] { ScreenStartRow, ScreenStartCol };
        }

        if (invalidatePage)
        {
            InvalidateText();
        }
        else
        {
            InvalidateCursorPosition(CursorRow, CursorCol);
        }

        if (vScrollBar.Visible)
        {
            vScrollBar.Value = Math.Clamp((int)(vScrollBar.Maximum * ScreenStartRow / (double)lines.Rows), 0, vScrollBar.Maximum);
        }

        if (hScrollBar.Visible)
        {
            hScrollBar.Value = Math.Clamp((int)(hScrollBar.Maximum * ScreenStartCol / (double)lines.Cols), 0, hScrollBar.Maximum);
        }

        state[CursorKey] = new long[] { CursorRow, CursorCol };
    }

    private void MouseDownEvent(object sender, MouseEventArgs e)
    {
        if (Rows == 0) return;

        if (e.Button == MouseButtons.Left)
        {
            Capture = true;

            int col = (int)((e.X) / FontWidthPixels) + ScreenStartCol;
            long row = (int)((e.Y) / FontHeightPixels) + ScreenStartRow;

            try
            {
                if (HasSelection && !shiftKeyDown)
                {
                    ClearSelection();
                    Invalidate();
                }
                else if (!HasSelection && !shiftKeyDown)
                {
                    // set the cursor to where the mouse is.
                    SelectionAnchorCol = col;
                    SelectionAnchorRow = row;
                    InvalidateSelection();
                }
                else if (shiftKeyDown)
                {
                    // set the cursor to where the mouse is.
                    SelectionReleaseCol = col;
                    SelectionReleaseRow = row;
                    InvalidateSelection();
                }
            }
            finally
            {
                CursorRow = row;
                CursorCol = col;
            }
        }
    }

    private void MouseUpEvent(object sender, MouseEventArgs e)
    {
        if (Rows == 0) return;

        if (e.Button == MouseButtons.Left && shiftKeyDown)
        {
            if (Capture)
            {
                Capture = false;
            }

            // set the cursor to where the mouse is.
            SelectionReleaseCol = (int)((e.X) / FontWidthPixels) + ScreenStartCol;
            SelectionReleaseRow = (int)((e.Y) / FontHeightPixels) + ScreenStartRow;

            InvalidateSelection();
        }
        else if (e.Button == MouseButtons.Right && HasSelection)
        {
            ContextMenuStrip cms = new ContextMenuStrip();
            ToolStripMenuItem tsmiCopy = new ToolStripMenuItem("Copy");
            tsmiCopy.Click += (s, ea) => ToClipboard();
            cms.Items.Add(tsmiCopy);
            cms.Show(this, e.Location);
        }
    }

    private void MouseMoveEvent(object sender, MouseEventArgs e)
    {
        if (Rows == 0) return;

        if (e.Button == MouseButtons.Left)
        {
            int col = (int)((e.X) / FontWidthPixels) + ScreenStartCol;
            long row = (int)((e.Y) / FontHeightPixels) + ScreenStartRow;

            CursorRow = row;
            CursorCol = col;

            if (HasSelection)
            {
                SelectionReleaseCol = col;
                SelectionReleaseRow = row;
            }

            InvalidateSelection();
        }
    }

    private void ClearSelection()
    {
        SelectionAnchorRow = -1;
        SelectionAnchorCol = -1;
        SelectionReleaseRow = -1;
        SelectionReleaseCol = -1;
    }

    private void MouseDoubleClickEvent(object sender, MouseEventArgs e)
    {
        if (Rows == 0) return;

        // select the word under the mouse
        int c = (int)(e.X / FontWidthPixels) + ScreenStartCol;
        long r = (int)(e.Y / FontHeightPixels) + ScreenStartRow;

        if (r >= 0 && r < Rows)
        {
            string line = lines[r];

            if (c >= 0 && c < line.Length)
            {
                if (char.IsLetterOrDigit(line[c]))
                {
                    int o = c;

                    while (c > 0 && char.IsLetterOrDigit(line[c]))
                    {
                        c--;
                    }

                    if (c < 0 || !char.IsLetterOrDigit(line[c]))
                    {
                        c++;
                    }

                    SelectionAnchorRow = r;
                    SelectionAnchorCol = c;

                    c = o;
                    while (c < line.Length && char.IsLetterOrDigit(line[c]))
                    {
                        c++;
                    }

                    if (c > line.Length)
                    {
                        c--;
                    }

                    SelectionReleaseRow = r;
                    SelectionReleaseCol = c;

                    InvalidateSelection();
                }
            }
        }
    }

    [Browsable(false)]
    public bool InSearch
    {
        get
        {
            if (lines == null || string.IsNullOrEmpty(TextToFind))
            {
                return false;
            }

            return lines.IsSearching();
        }
    }

    [Browsable(false)]
    public bool HasSelection
    {
        get
        {
            return SelectionAnchorRow != -1;
        }
    }

    public void SearchAgain()
    {
        if (lines == null || string.IsNullOrEmpty(TextToFind))
        {
            return;
        }

        lines.StartSearch(CursorRow, CursorCol + 1, UseCase, SearchDown, TextToFind);
    }

    public void StopSearch()
    {
        if (lines == null)
        {
            return;
        }

        lines.StopSearch();
    }

    public void StopLoad()
    {
        if (lines == null)
        {
            return;
        }

        lines.StopLoad();
    }

    private void timerSetStatus_Tick(object sender, EventArgs e)
    {
        Debugger.Break();

    }

    private void timerCursor_Tick(object sender, EventArgs e)
    {
        cursorIsOn = !cursorIsOn;

        if (lines != null &&
            CursorRow >= ScreenStartRow && CursorRow < ScreenStartRow + ScreenRows &&
            CursorCol >= ScreenStartCol && CursorCol < ScreenStartCol + ScreenCols)
        {
            InvalidateCursorPosition(CursorRow, CursorCol);
        }
    }

    private void InvalidateSelection()
    {
        if (SelectionAnchorRow == -1)
        {
            return;
        }

        int sc;
        int ec;

        long sr = Math.Min(SelectionAnchorRow, SelectionReleaseRow);
        long er = Math.Max(SelectionAnchorRow, SelectionReleaseRow);
        if (sr != er)
        {
            sc = 0;
            ec = ScreenCols;
        }
        else
        {
            sc = Math.Min(SelectionReleaseCol, SelectionReleaseCol);
            ec = Math.Max(SelectionReleaseCol, SelectionReleaseCol);
        }

        float x = (sc - ScreenStartCol - 1) * FontWidthPixels;
        float y = (sr - ScreenStartRow - 1) * FontHeightPixels;

        float width = (ec - sc + 2) * FontWidthPixels;
        float height = (er - sr + 2) * FontHeightPixels;

        Rectangle rect = new Rectangle((int)x, (int)y, (int)width + 2, (int)height + 2);
        Invalidate(oldRect);
        Invalidate(rect);
        oldRect = rect;
    }

    private void InvalidateText()
    {
        Invalidate(Rectangle.Round(tcRect));
    }

    private void InvalidateCursorPosition(long cursorRow, int cursorCol)
    {
        float x = (cursorCol - ScreenStartCol - 1) * FontWidthPixels;
        float y = (cursorRow - ScreenStartRow - 1) * FontHeightPixels;
        RectangleF cursorRectangle = new RectangleF(x, y, FontWidthPixels * 2, FontHeightPixels * 2);
        Rectangle cursorRect = Rectangle.Round(cursorRectangle);
        Invalidate(cursorRect);
    }

    private void LayoutEvent(object sender, LayoutEventArgs e)
    {
    }

    private void MouseCaptureChangedEvent(object sender, EventArgs e)
    {
    }

    private void MouseEnterEvent(object sender, EventArgs e)
    {
    }

    private void MouseLeaveEvent(object sender, EventArgs e)
    {
    }

    private void ScrollEvent(object sender, ScrollEventArgs e)
    {
        ScrollBar sb;

        if (sender is ScrollBar vscroll)
        {
            sb = vscroll;
        }
        else if (sender is ScrollBar hscroll)
        {
            sb = hscroll;
        }
        else
        {
            return;
        }

        int delta = e.OldValue - e.NewValue;
        sb.Value = e.NewValue;
        if (delta != 0)
        {
            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                ScrollRowUpDown(delta);
            }
            else if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
            {
                ScrollLeftRight(delta);
            }
        }
    }

    internal static void ForceGC()
    {
        // Collect all generations
        GC.Collect();

        // Wait for finalizers to complete
        GC.WaitForPendingFinalizers();
    }

    private void vScrollBar_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
    {
        PreviewKeyDownEvent(sender, e);
    }

    private void vScrollBar_KeyPress(object sender, KeyPressEventArgs e)
    {
        e.Handled = true;
    }

    private void hScrollBar_KeyPress(object sender, KeyPressEventArgs e)
    {
        e.Handled = true;
    }

    private void hScrollBar_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
    {
        PreviewKeyDownEvent(sender, e);
    }

    private void vScrollBar_Enter(object sender, EventArgs e)
    {
        //Focus();
    }

    private void vScrollBar_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
    }

    private void vScrollBar_KeyUp(object sender, KeyEventArgs e)
    {
        e.Handled = true;
    }

    private void hScrollBar_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
    }

    private void hScrollBar_KeyUp(object sender, KeyEventArgs e)
    {
        e.Handled = true;
    }

    public static string FormatWithSuffix(long value)
    {
        // Handle small numbers directly
        if (Math.Abs(value) < 1024)
            return value.ToString();

        // Define suffixes
        string[] suffixes = { "", "K", "M", "G", "T", "P", "E" }; // up to exa
        int suffixIndex = 0;
        decimal decimalValue = value;

        // Reduce number until it's under 1000, incrementing suffix index
        while (Math.Abs(decimalValue) >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            decimalValue /= 1024;
            suffixIndex++;
        }

        // Format with up to 1 decimal place if needed
        return string.Format("{0:0.#}{1}", decimalValue, suffixes[suffixIndex]);
    }
}
