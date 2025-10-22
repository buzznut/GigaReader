using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using UtilitiesLibrary;
using Clipboard = System.Windows.Forms.Clipboard;

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

    public const string CursorKey = "Cursor";
    public static readonly Dictionary<string, Type> CursorKeyTypes = new Dictionary<string, Type>()
    {
        { CursorKey, typeof(long[]) },
    };

    public long CursorRow;
    public int CursorCol;
    public int ScreenCols;
    public int ScreenRows;
    public long ScreenStartRow;
    public int ScreenStartCol;
    public float FontWidthPixels;
    public float FontHeightPixels;
    public long SelectionAnchorRow = -1;
    public int SelectionAnchorCol = -1;
    public long SelectionReleaseRow = -1;
    public int SelectionReleaseCol = -1;
    public bool SearchDown = true;
    public bool UseCase;
    public string TextToFind;

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
    private Pen cursorPen = new Pen(Color.DarkOrange, 2);
    private bool searchFound = false;
    private int searchCol = -1;
    private long searchRow = -1;

    public TextControl()
    {
        InitializeComponent();

        CalculateFontWidth();
        CalculateScreenDimensions();

        brushNormalBackground = new SolidBrush(BackColor);
        brushNormalText = new SolidBrush(ForeColor);
        brushSelectionBackground = new SolidBrush(SystemColors.Highlight);
        brushSelectionText = new SolidBrush(SystemColors.HighlightText);

        RegisterStateHandler(LineProgressChanged, Lines.LinesProgressKey);
        RegisterStateHandler(SearchFound, Search.FinishedKey);
        RegisterStateHandler(SearchCursor, Search.SearchCursorKey);
        RegisterStateHandler(LinesLoaded, Lines.LinesLoadedKey);

        HandleCursorChange(0, 0);

        Clear();
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

        if (oldScreenCol != ScreenStartCol || oldScreenRow != ScreenStartRow)
        {
            Invalidate();
        }

        HandleCursorChange(srow, scol);
    }

    private void LineProgressChanged(KeyValuePair<string, object> _)
    {
        if (lines != null && lines.Rows > ScreenRows)
        {
            Invalidate();
        }
    }

    private void LinesLoaded(KeyValuePair<string, object> _)
    {
        if (lines != null && lines.Rows > ScreenRows)
        {
            Invalidate();
        }
    }

    public delegate void StateHandler(KeyValuePair<string, object> state);
    private ConcurrentDictionary<string, List<StateHandler>> stateHandlers = new();
    public void RegisterStateHandler(StateHandler stateHandler, string key)
    {
        if (!stateHandlers.ContainsKey(key))
        {
            stateHandlers[key] = new List<StateHandler>();
        }
        stateHandlers[key].Add(stateHandler);
    }

    private void StateChanged(IDictionary<string, object> state)
    {
        if (state == null || state.Count == 0)
        {
            // nothing new
            return;
        }

        foreach (KeyValuePair<string, object> kvp in state)
        {
            if (stateHandlers.ContainsKey(kvp.Key))
            {
                foreach (StateHandler sh in stateHandlers[kvp.Key])
                {
                    try
                    {
                        sh(kvp);
                    }
                    catch (Exception ex)
                    {
                        if (Debugger.IsAttached)
                        {
                            Debugger.Break();
                        }
                    }
                }
            }
        }
    }

    public void Open(string file)
    {
        Clear();
        lines = new Lines(StateChanged);
        lines.Load(file);
        Focus();

        Dictionary<string, object> state = new Dictionary<string, object>();
        state[CursorKey] = (long[])[CursorRow, CursorCol];
        StateChanged(state);
    }

    public void Clear()
    {
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
        SelectionAnchorRow = -1;
        SelectionAnchorCol = -1;
        SelectionReleaseRow = -1;
        SelectionReleaseCol = -1;
        SearchDown = true;
        UseCase = false;
        TextToFind = null;
        Invalidate();

        ResumeLayout(true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    private void SizeChangedEvent(object sender, EventArgs e)
    {
        CalculateScreenDimensions();
    }

    private void MouseClickEvent(object sender, MouseEventArgs e)
    {
    }

    private void CalculateFontWidth()
    {
        using (Graphics g = CreateGraphics())
        {
            string ms = "Now is the time for all good men to come to the aid of their country.";
            ms += " The quick brown fox jumped over the lazy dog's back.";
            ms += " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789,./?><';:][}{\\|=-+_])(*&^%$#@!~`\"";
            SizeF xx = g.MeasureString(ms, Font);

            FontHeightPixels = xx.Height;
            FontWidthPixels = 0.5F + (xx.Width / ms.Length);

            g.Dispose();
        }
    }

    private void FontChangedEvent(object sender, EventArgs e)
    {
        try
        {
            CalculateFontWidth();
            CalculateScreenDimensions();
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached) Debugger.Break();
            throw;
        }
    }

    private void CalculateScreenDimensions()
    {
        if (FontWidthPixels <= 0 && FontHeightPixels <= 0) return;

        ScreenCols = Convert.ToInt32(Math.Truncate((Width - Padding.Left - Padding.Right + FontWidthPixels - 1) / FontWidthPixels));
        ScreenRows = Convert.ToInt32(Math.Truncate((Height - Padding.Top - Padding.Bottom + FontHeightPixels - 1) / FontHeightPixels));

        if (ScreenCols < 0) ScreenCols = 0;
        if (ScreenRows < 0) ScreenRows = 0;

        Invalidate();
    }

    private void PaintEvent(object sender, PaintEventArgs e)
    {
        try
        {
            StringBuilder sb = new StringBuilder();

            Graphics g = e.Graphics;

            RectangleF rc = new RectangleF(e.ClipRectangle.Left,
                e.ClipRectangle.Top,
                e.ClipRectangle.Width,
                e.ClipRectangle.Height);

            int row = (int)MathF.Round(MathF.Max((rc.Top - Padding.Top) / FontHeightPixels, 0));
            int rows = (int)MathF.Truncate(MathF.Max((rc.Height - Padding.Top - Padding.Bottom + FontHeightPixels - 1) / FontHeightPixels, 1));
            int col = (int)MathF.Round(MathF.Max((rc.Left - Padding.Left) / FontWidthPixels, 0));
            int cols = (int)MathF.Truncate(MathF.Max((rc.Width - Padding.Right - Padding.Left) / FontWidthPixels, 1));

            if (lines == null || lines.Rows < ScreenStartRow + ScreenRows)
            {
                //if (rows > 0)
                //{
                //    sb.Append("Please wait while loading...");
                //    g.FillRectangle(brushNormalBackground, 0, 0, (FontWidthPixels * sb.Length), FontHeightPixels);
                //    DrawStringByCharacter(sb, g, 0, brushNormalText, 0);
                //    sb.Clear();
                //}

                return;
            }

            if (col > 0)
            {
                col--;
                cols++;
            }

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

            bool showCursor = cursorIsOn &&
                CursorRow >= ScreenStartRow && CursorRow < ScreenStartRow + ScreenRows &&
                CursorCol >= ScreenStartCol && CursorCol < ScreenStartCol + ScreenCols;

            string line;
            for (int ii = 0; ii <= rows + 1; ii++)
            {
                int r = ii + row;

                float y = Padding.Top + (r * FontHeightPixels);
                bool useSpaceRow = false;

                long textRow = r + ScreenStartRow;
                if (textRow >= lines.Rows) continue;

                bool inSelectionRow = sr != -1 && textRow >= sr && textRow <= er;
                line = lines[textRow];
                if (line == null)
                {
                    Invalidate();
                    return;
                }

                line = line.Replace("\t", " ".PadRight(TabSize));

                Brush lastTextBrush = brushNormalText;
                Brush lastBackground = brushNormalBackground;
                float xLast = -1;
                float xStart = 0;

                sb.Clear();
                for (int jj = 0; jj <= cols + 1; jj++)
                {
                    int c = col + jj;
                    float x = Padding.Left + (c * FontWidthPixels);

                    Brush brBackground = brushNormalBackground;
                    Brush brText = brushNormalText;

                    int textCol = c + ScreenStartCol;
                    if (textCol >= lines.Cols) continue;

                    char ch = ' ';
                    if (textCol < line.Length && textCol >= 0)
                    {
                        if (inSelectionRow)
                        {
                            brText = brushSelectionText;
                            brBackground = brushSelectionBackground;

                            // special case for the start and end of the selection
                            if ((textRow == sr && textCol < sc) || (textRow == er && textCol > ec))
                            {
                                brText = brushNormalText;
                                brBackground = brushNormalBackground;
                            }
                        }

                        ch = line[textCol];
                    }

                    if (useSpaceRow ||
                        x > Width - Padding.Right - FontWidthPixels - (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) ||
                        x < Padding.Left)
                    {
                        ch = ' ';
                    }

                    if (xLast.CompareTo(-1F) == 0)
                    {
                        lastTextBrush = brText;
                        lastBackground = brBackground;
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

            if (showCursor)
            {
                float x = Padding.Left + ((CursorCol - ScreenStartCol) * FontWidthPixels) + 2;
                float y = Padding.Top + ((CursorRow - ScreenStartRow) * FontHeightPixels);
                PointF topPoint = new PointF(x, y);
                PointF bottomPoint = new PointF(x, y + FontHeightPixels - 4);

                g.DrawLine(cursorPen, topPoint, bottomPoint);
            }
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached) Debugger.Break();
            throw;
        }
    }

    private void DrawStringByCharacter(StringBuilder sb, Graphics g, float y, Brush lastTextBrush, float xLast)
    {
        float xpart = xLast;
        foreach (char cpart in sb.ToString())
        {
            g.DrawString(cpart.ToString(), Font, lastTextBrush, xpart, y);
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
        if (lines?.Rows == 0) return false;

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
                    WordLeftRight(-1);
                    return true;
                }

                CursorLeftRight(-1);
                return true;
            }

            case Keys.Right:
            {
                if (controlKeyDown)
                {
                    WordLeftRight(1);
                    return true;
                }
                
                CursorLeftRight(1);
                return true;
            }

            case Keys.Up:
            {
                if (controlKeyDown)
                {
                    ScrollRowUpDown(1);
                    return true;
                }
                
                CursorUpDown(-1);
                return true;
            }

            case Keys.Down:
            {
                if (controlKeyDown)
                {
                    ScrollRowUpDown(-1);
                    return true;
                }

                CursorUpDown(1);
                return true;
            }

            case Keys.PageUp:
            {
                if (controlKeyDown)
                {
                    long rowsToMove = ScreenStartRow - CursorRow;
                    CursorUpDown(rowsToMove);
                    return true;
                }
                
                CursorUpDown(-ScreenRows);
                return true;
            }

            case Keys.PageDown:
            {
                if (controlKeyDown)
                {
                    long rowsToMove = ScreenStartRow + ScreenRows - CursorRow - 1;
                    CursorUpDown(rowsToMove);
                    return true;
                }
                
                CursorUpDown(ScreenRows);
                return true;
            }

            case Keys.Home:
            {
                if (controlKeyDown)
                {
                    GoToHome();
                    return true;
                }
                
                LineBegin();
                return true;
            }

            case Keys.End:
            {
                if (controlKeyDown)
                {
                    GoToEnd();
                    return true;
                }

                return LineEnd();
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
        }

        return false;
    }

    private void WordLeftRight(int addrValue)
    {
        if (addrValue == 0) return;

        string line = GetLine(CursorRow);
        InvalidateCursorPosition(CursorRow, CursorCol);
        int currentCol = CursorCol;

        if (addrValue > 0)
        {
            // moving right
            if (currentCol >= line.Length)
            {
                CursorLeftRight(addrValue);
                return;
            }

            char ch = line[currentCol];
            if (!char.IsLetterOrDigit(ch))
            {
                // move until we find a letter/digit
                while (currentCol < line.Length && !char.IsLetterOrDigit(line[currentCol]))
                {
                    currentCol += addrValue;
                }
            }
            else
            {
                // move until we find a non-space character
                while (currentCol < line.Length && char.IsLetterOrDigit(line[currentCol]))
                {
                    currentCol += addrValue;
                }
            }
        }
        else
        {
            currentCol += addrValue;

            // moving left
            if (currentCol <= 0)
            {
                CursorLeftRight(addrValue);
                return;
            }

            char ch = line[currentCol];
            if (!char.IsLetterOrDigit(ch))
            {
                // move until we find a letter/digit
                while (currentCol >= 0 && !char.IsLetterOrDigit(line[currentCol]))
                {
                    currentCol += addrValue;
                }
            }
            else
            {
                // move until we find a non-space character
                while (currentCol >= 0 && char.IsLetterOrDigit(line[currentCol]))
                {
                    currentCol += addrValue;
                }
            }

            currentCol -= addrValue;
        }

        if (CursorCol != currentCol)
        {
            CursorLeftRight(currentCol - CursorCol);
        }
    }

    private void CursorLeftRight(int cols, bool handleCursorChange = true)
    {
        long srow = CursorRow;
        int scol = CursorCol;

        string line = GetLine(CursorRow);
        if (line == null) return;

        CursorCol += cols;

        if (CursorCol < 0 && ScreenStartRow > 0)
        {
            CursorUpDown(-1, false);
            LineEnd(false);
        }
        else if (CursorCol > line.Length)
        {
            if ((Rows - ScreenStartRow - ScreenRows) > (CursorRow + 1))
            {
                CursorUpDown(1, false);
                LineBegin(false);
            }
            else
            {
                CursorCol -= cols;
            }
        }

        if (handleCursorChange) HandleCursorChange(srow, scol);
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
                GoToLine(row - 1);
            }
        }
    }

    private void PreviewKeyDownEvent(object sender, PreviewKeyDownEventArgs e)
    {
        KeyEventArgs k = new KeyEventArgs(e.KeyData);
        wasHandled = ProcessKey(k);
    }

    private void MouseWheelEvent(object sender, MouseEventArgs e)
    {
        // Update the drawing based upon the mouse wheel scrolling.
        int numberOfTextLinesToMove = (e.Delta * SystemInformation.MouseWheelScrollLines / 120);
        ScrollRowUpDown(numberOfTextLinesToMove);
    }

    private void GoToHome(bool handleCursorChange = true)
    {
        long srow = CursorRow;
        int scol = CursorCol;
        int oldScreenCol = ScreenStartCol;
        long oldScreenRow = ScreenStartRow;

        CursorRow = 0;
        CursorCol = 0;
        ScreenStartCol = 0;
        ScreenStartRow = 0;

        if (oldScreenCol != ScreenStartCol || oldScreenRow != ScreenStartRow)
        {
            Invalidate();
        }

        if (handleCursorChange) HandleCursorChange(srow, scol);
    }

    private void GoToEnd(bool handleCursorChange = true)
    {
        long srow = CursorRow;
        int scol = CursorCol;
        int oldScreenCol = ScreenStartCol;
        long oldScreenRow = ScreenStartRow;

        CursorRow = Rows - 1;
        if (!LineEnd()) return;

        ScreenStartRow = Rows - ScreenRows;

        if (oldScreenCol != ScreenStartCol || oldScreenRow != ScreenStartRow)
        {
            Invalidate();
        }

        if (handleCursorChange) HandleCursorChange(srow, scol);
    }

    public void GoToLine(long row, bool handleCursorChange = true)
    {
        long srow = CursorRow;
        int scol = CursorCol;
        int oldScreenCol = ScreenStartCol;
        long oldScreenRow = ScreenStartRow;

        if (row > lines.Rows - 1) { row = lines.Rows - 1; }

        CursorRow = row;
        LineBegin(handleCursorChange);
        ScreenStartRow = row - ScreenRows/2 + 1;
        if (ScreenStartRow < 0)
        {
            ScreenStartRow = 0;
        }

        if (oldScreenCol != ScreenStartCol || oldScreenRow != ScreenStartRow)
        {
            Invalidate();
        }

        if (handleCursorChange) HandleCursorChange(srow, scol);
    }

    private void LineBegin(bool handleCursorChange = true)
    {
        long srow = CursorRow;
        int scol = CursorCol;
        int oldScreenCol = ScreenStartCol;

        CursorCol = 0;
        if (CursorCol < ScreenStartCol)
        {
            ScreenStartCol = 0;
        }

        if (oldScreenCol != ScreenStartCol)
        {
            Invalidate();
        }

        if (handleCursorChange) HandleCursorChange(srow, scol);
    }

    private bool LineEnd(bool handleCursorChange = true)
    {
        long srow = CursorRow;
        int scol = CursorCol;

        string line = GetLine(CursorRow);
        if (line == null) return false;

        CursorCol = line.Length;

        if (handleCursorChange) HandleCursorChange(srow, scol);
        return true;
    }

    private string GetLine(long cursorRow)
    {
        if (lines == null) return string.Empty;
        string text = lines[cursorRow];
        if (text == null) return null;
        return text.Replace("\t", "".PadRight(TabSize));
    }

    private void CursorUpDown(long rowsToMoveCursor, bool handleCursorChange = true)
    {
        long srow = CursorRow;
        int scol = CursorCol;
        long oldStartRow = ScreenStartRow;

        CursorRow += rowsToMoveCursor;
        string line = GetLine(CursorRow);
        if (line == null) return;

        if (CursorCol > line.Length)
        {
            CursorCol = line.Length;
        }

        if (CursorRow < 0)
        {
            CursorRow = 0;
        }
        else if (CursorRow >= Rows)
        {
            CursorRow = Rows - 1;
        }

        if (CursorRow < ScreenStartRow)
        {
            ScreenStartRow = CursorRow;
        }
        else if (CursorRow > ScreenStartRow + ScreenRows - 1)
        {
            ScreenStartRow += rowsToMoveCursor;
        }

        if (ScreenStartRow < 0)
        {
            ScreenStartRow = 0;
        }
        else if (ScreenStartRow > Rows - ScreenRows/2 && Rows > ScreenRows)
        {
            ScreenStartRow = Rows - ScreenRows;
        }

        if (oldStartRow != ScreenStartRow)
        {
            Invalidate();
        }

        if (handleCursorChange) HandleCursorChange(srow, scol);
    }

    private void KeyUpEvent(object sender, KeyEventArgs e)
    {
        //string txt = string.Format ("KeyUp: code={0}, data={1}, value={2}, modifiers={3}", state.KeyCode, state.KeyData, state.KeyValue, state.Modifiers);
        //Debug.WriteLine (txt);

        if (e.Shift)
        {
            shiftKeyDown = false;
        }

        if (e.Control)
        {
            controlKeyDown = false;
        }

        e.Handled = wasHandled;
        e.SuppressKeyPress = wasHandled;
    }

    private void ScrollRowUpDown(int linesToMove, bool handleCursorChange = true)
    {
        if (lines == null || lines.Rows == 0) return;

        long srow = CursorRow;
        int scol = CursorCol;

        long oldScreenStartRow = ScreenStartRow;

        ScreenStartRow -= linesToMove;

        if (ScreenStartRow >= Rows - ScreenRows)
        {
            ScreenStartRow = Rows - ScreenRows - 1;
        }

        if (ScreenStartRow <= 0)
        {
            ScreenStartRow = 0;
        }

        if (oldScreenStartRow != ScreenStartRow)
        {
            Invalidate();
        }

        //// if the cursor is no longer on the page, adjust the cursor appropriately
        //if (CursorRow < ScreenStartRow) CursorRow = ScreenStartRow;
        //if (CursorRow > ScreenStartRow + ScreenRows - 1) CursorRow = ScreenStartRow + ScreenRows - 1;

        //if (handleCursorChange) HandleCursorChange(srow, scol);
    }

    private void ScrollLeftRight(int columnsToMove, bool handleCursorChange = true)
    {
        long srow = CursorRow;
        int scol = CursorCol;

        ScreenStartCol -= columnsToMove;

        if (ScreenStartCol >= Cols)
        {
            ScreenStartCol = Cols - 1;
        }

        if (ScreenStartCol <= 0)
        {
            ScreenStartCol = 0;
        }

        if (CursorCol >= ScreenStartCol + ScreenCols)
        {
            CursorCol = ScreenStartCol + ScreenCols - 1;
        }
        else if (CursorCol < ScreenStartCol)
        {
            CursorCol = ScreenStartCol;
        }

        if (handleCursorChange) HandleCursorChange(srow, scol);
    }

    public void ToClipboard()
    {
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

    private void HandleCursorChange(long oldRow, int oldCol)
    {
        cursorIsOn = false;
        InvalidateCursorPosition(oldRow, oldCol);

        if (shiftKeyDown)
        {
            if (SelectionAnchorRow == -1)
            {
                SelectionAnchorRow = oldRow;
                SelectionReleaseCol = oldCol;
            }

            SelectionReleaseRow = CursorRow;
            SelectionReleaseCol = CursorCol;

            InvalidateSelection();
        }
        else
        {
            if (SelectionAnchorRow != -1)
            {
                SelectionAnchorRow = -1;
                Invalidate();
            }
        }

        Dictionary<string, object> state = new Dictionary<string, object>();
        state[CursorKey] = (long[])[CursorRow, CursorCol];
        StateChanged(state);
    }

    private void MouseDownEvent(object sender, MouseEventArgs e)
    {
        //InvalidateSelection();

        //// set the cursor to where the mouse is.
        //SelectionReleaseCol = (int)((e.X - Padding.Left) / FontWidthPixels) + ScreenStartCol;
        //SelectionAnchorRow = (int)((e.Y - Padding.Top) / FontHeightPixels) + ScreenStartRow;

        //Capture = true;
        //shiftKeyDown = true;

        //SelectionReleaseCol = SelectionAnchorCol;
        //SelectionReleaseRow = SelectionAnchorRow;
    }

    private void MouseUpEvent(object sender, MouseEventArgs e)
    {
        //if (Capture)
        //{
        //    Capture = false;
        //    shiftKeyDown = false;
        //}

        //if (noMouseUp)
        //{
        //    noMouseUp = false;
        //    return;
        //}

        //// set the cursor to where the mouse is.
        //SelectionReleaseCol = (int)((e.X - Padding.Left) / FontWidthPixels) + ScreenStartCol;
        //SelectionReleaseRow = (int)((e.Y - Padding.Top) / FontHeightPixels) + ScreenStartRow;

        //if (SelectionReleaseRow >= ScreenStartRow + ScreenRows)
        //{
        //    ScreenStartRow = SelectionReleaseRow - ScreenRows;
        //    if (ScreenStartRow > Rows)
        //    {
        //        ScreenStartRow = Rows;
        //    }
        //    Invalidate();
        //}
        //else if (SelectionReleaseRow < ScreenStartRow)
        //{
        //    ScreenStartRow = SelectionReleaseRow;
        //    Invalidate();
        //}
        //else
        //{
        //    InvalidateSelection();
        //}

        //if (SelectionReleaseCol >= ScreenStartCol + ScreenCols)
        //{
        //    ScreenStartCol = SelectionReleaseCol - ScreenCols;
        //    if (ScreenStartCol > Cols)
        //    {
        //        ScreenStartCol = Cols;
        //    }
        //    Invalidate();
        //}
        //else if (SelectionReleaseCol < ScreenStartCol)
        //{
        //    ScreenStartCol = SelectionReleaseCol;
        //    Invalidate();
        //}
        //else
        //{
        //    InvalidateSelection();
        //}

        ////if (SelectionReleaseRow < 0) SelectionReleaseRow = 0;
        ////if (SelectionReleaseCol < 0) SelectionReleaseCol = 0;
        ////if (ScreenStartRow < 0) ScreenStartRow = 0;
        ////if (ScreenStartCol < 0) ScreenStartCol = 0;

        //if (Math.Abs(SelectionReleaseCol - SelectionReleaseCol) < 1 &&
        //    Math.Abs(SelectionAnchorRow - SelectionReleaseRow) < 1)
        //{
        //    CursorCol = SelectionReleaseCol;
        //    CursorRow = SelectionReleaseRow;
        //    SelectionAnchorRow = -1;

        //    InvalidateSelection();
        //}
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

        float x = ((sc - ScreenStartCol) * FontWidthPixels) + Padding.Left;
        float y = ((sr - ScreenStartRow) * FontHeightPixels) + Padding.Top;

        float width = (ec - sc + 2) * FontWidthPixels;
        float height = (er - sr + 1) * FontHeightPixels;

        Rectangle rect = new Rectangle((int)x - 1, (int)y - 1, (int)width + 2, (int)height + 2);
        Invalidate(oldRect);
        Invalidate(rect);
        oldRect = rect;
    }

    private void MouseDoubleClickEvent(object sender, MouseEventArgs e)
    {
        //// select the word under the mouse
        //int c = (int)((e.X - Padding.Left) / FontWidthPixels) + ScreenStartCol;
        //long r = (int)((e.Y - Padding.Top) / FontHeightPixels) + ScreenStartRow;

        //if (r >= 0 && r < Rows)
        //{
        //    string line = lines[r];

        //    if (c >= 0 && c < line.Length)
        //    {
        //        if (line[c] != ' ')
        //        {
        //            int o = c;

        //            while (c > 0 && line[c] != ' ')
        //            {
        //                c--;
        //            }

        //            if (c < 0 || line[c] == ' ')
        //            {
        //                c++;
        //            }

        //            SelectionAnchorRow = r;
        //            SelectionReleaseCol = c;

        //            c = o;
        //            while (c < line.Length && line[c] != ' ')
        //            {
        //                c++;
        //            }

        //            if (c > line.Length)
        //            {
        //                c--;
        //            }

        //            SelectionReleaseRow = r;
        //            SelectionReleaseCol = c;

        //            noMouseUp = true;
        //            InvalidateSelection();
        //        }
        //    }
        //}
    }

    private void MouseMoveEvent(object sender, MouseEventArgs e)
    {
        //if (e.Button == MouseButtons.Left)
        //{
        //    InvalidateSelection();

        //    SelectionReleaseCol = (int)((e.X - Padding.Left) / FontWidthPixels) + ScreenStartCol;
        //    SelectionReleaseRow = (int)((e.Y - Padding.Top) / FontHeightPixels) + ScreenStartRow;

        //    if (SelectionReleaseRow >= ScreenStartRow + ScreenRows)
        //    {
        //        ScreenStartRow = SelectionReleaseRow - ScreenRows;
        //        if (ScreenStartRow > Rows)
        //        {
        //            ScreenStartRow = Rows;
        //        }
        //        Invalidate();
        //    }
        //    else if (SelectionReleaseRow < ScreenStartRow)
        //    {
        //        ScreenStartRow = SelectionReleaseRow;
        //        if (ScreenStartRow < 0)
        //        {
        //            ScreenStartRow = 0;
        //        }
        //        Invalidate();
        //    }
        //    else
        //    {
        //        InvalidateSelection();
        //    }
        //}
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

    private void InvalidateCursorPosition(long cursorRow, int cursorCol)
    {
        float x = Padding.Left + ((cursorCol - ScreenStartCol) * FontWidthPixels);
        float y = Padding.Top + ((cursorRow - ScreenStartRow) * FontHeightPixels);
        PointF start = new PointF(x, y);
        SizeF size = new SizeF(MathF.Ceiling(FontWidthPixels), MathF.Ceiling(FontHeightPixels));

        RectangleF cursorRect = new RectangleF(start, size);
        Invalidate(Rectangle.Round(cursorRect));
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

    }

    internal void ForceGC()
    {
        // Collect all generations
        GC.Collect();

        // Wait for finalizers to complete
        GC.WaitForPendingFinalizers();
    }

}
