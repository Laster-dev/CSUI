using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using static CSUI.Tests.TestHelpers;

namespace CSUI.Tests;

internal static class WpfAllControlsTests
{
    private sealed class GridRowItem
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool Active { get; set; }
        public Uri? Website { get; set; }
    }

    private sealed class TestHwndHost : HwndHost
    {
        public bool Built { get; private set; }
        public bool Destroyed { get; private set; }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            Built = true;
            return new HandleRef(this, new IntPtr(0x1234));
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            Destroyed = true;
        }
    }

    public static void RunAll()
    {
        Console.WriteLine("\n--- Layer 2D: Complete WPF Controls, Shapes, Navigation, Ink, DataGrid & Documents ---");
        TestShapesAndGpuRendering();
        TestBatch1PrimitivesAndConverters();
        TestBatch2SelectionTabsListViewTreeView();
        TestBatch3MenusToolBarStatusBarToolTip();
        TestBatch4CalendarAndDatePicker();
        TestBatch5DataGridSuite();
        TestBatch6NavigationInkDocumentsAndHosting();
        TestBatch7MessageBoxFileDialogsGlyphsAndRibbon();
        TestAero2AuthenticDefaultThemes();
    }

    private static void TestShapesAndGpuRendering()
    {
        var canvas = new Canvas { Width = 100, Height = 100, Background = Brushes.Black };

        var rect = new Rectangle { Width = 40, Height = 40, Fill = Brushes.Red };
        Canvas.SetLeft(rect, 5);
        Canvas.SetTop(rect, 5);

        var ellipse = new Ellipse { Width = 30, Height = 30, Fill = Brushes.Lime };
        Canvas.SetLeft(ellipse, 55);
        Canvas.SetTop(ellipse, 10);

        var line = new Line { X1 = 0, Y1 = 80, X2 = 100, Y2 = 80, Stroke = Brushes.White, StrokeThickness = 2 };
        var poly = new Polygon
        {
            Fill = Brushes.Blue,
            Points = new PointCollection { new Point(10, 90), new Point(30, 90), new Point(20, 70) }
        };
        var polyline = new Polyline
        {
            Stroke = Brushes.Yellow,
            StrokeThickness = 2,
            Points = new PointCollection { new Point(50, 90), new Point(90, 90) }
        };
        var path = new System.Windows.Shapes.Path
        {
            Stroke = Brushes.Cyan,
            StrokeThickness = 2,
            Data = new RectangleGeometry(new Rect(50, 50, 20, 20))
        };

        canvas.Children.Add(rect);
        canvas.Children.Add(ellipse);
        canvas.Children.Add(line);
        canvas.Children.Add(poly);
        canvas.Children.Add(polyline);
        canvas.Children.Add(path);

        canvas.Measure(new Size(100, 100));
        canvas.Arrange(new Rect(0, 0, 100, 100));

        var pxRect = RenderAndSamplePixel(canvas, 100, 100, 20, 20);
        Assert(pxRect.R > 200 && pxRect.G < 30 && pxRect.B < 30, $"Expected red rectangle pixel, got R={pxRect.R},G={pxRect.G},B={pxRect.B}");

        var pxEllipse = RenderAndSamplePixel(canvas, 100, 100, 70, 25);
        Assert(pxEllipse.G > 200 && pxEllipse.R < 30, $"Expected green ellipse pixel, got R={pxEllipse.R},G={pxEllipse.G}");

        Pass("CTRL-1. Shapes (Rectangle, Ellipse, Line, Path, Polygon, Polyline) & GPU rendering");
    }

    private static void TestBatch1PrimitivesAndConverters()
    {
        var b2v = new BooleanToVisibilityConverter();
        AssertEqual(b2v.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture), Visibility.Visible, "BoolToVis true");
        AssertEqual(b2v.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture), Visibility.Collapsed, "BoolToVis false");
        AssertEqual(b2v.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture), true, "BoolToVis back");

        var alt = new AlternationConverter();
        alt.Values.Add("Even");
        alt.Values.Add("Odd");
        AssertEqual(alt.Convert(1, typeof(string), null, CultureInfo.InvariantCulture), "Odd", "AlternationConverter");

        var expander = new Expander { Header = "Details", Content = new TextBlock { Text = "Body" }, IsExpanded = true };
        var groupBox = new GroupBox { Header = "Settings", Content = new CheckBox { Content = "Enabled", IsChecked = true } };
        var groupItem = new GroupItem { Content = "Group" };
        var label = new Label { Content = new AccessText { Text = "_Save" } };
        var separator = new Separator();
        var uc = new UserControl { Content = label };
        var splitter = new GridSplitter { Width = 5 };
        var repeatBtn = new RepeatButton { Content = "Repeat", Interval = 50, Delay = 200 };
        var grip = new ResizeGrip();
        var bullet = new BulletDecorator { Bullet = new Rectangle { Width = 8, Height = 8 }, Child = new TextBlock { Text = "Item" } };
        var tickBar = new TickBar { Minimum = 0, Maximum = 10, TickFrequency = 2, Height = 10, Width = 100 };
        var track = new Track { Minimum = 0, Maximum = 100, Value = 50, Thumb = new Thumb() };
        var progress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 42, IsIndeterminate = false };

        var cb = (CheckBox)groupBox.Content;
        var cbPeer = UIElementAutomationPeer.CreatePeerForElement(cb) as CheckBoxAutomationPeer;
        Assert(cbPeer != null, "CheckBox peer");
        AssertEqual(((IToggleProvider)cbPeer!).ToggleState, System.Windows.Automation.ToggleState.On, "CheckBox ToggleState");
        var pbPeer = UIElementAutomationPeer.CreatePeerForElement(progress) as ProgressBarAutomationPeer;
        Assert(pbPeer != null, "ProgressBar peer");
        AssertEqual(((IRangeValueProvider)pbPeer!).IsReadOnly, true, "ProgressBar IRangeValueProvider.IsReadOnly");
        Assert(UIElementAutomationPeer.CreatePeerForElement(expander) is ExpanderAutomationPeer, "Expander peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(groupBox) is GroupBoxAutomationPeer, "GroupBox peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(repeatBtn) is RepeatButtonAutomationPeer, "RepeatButton peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(splitter) is GridSplitterAutomationPeer, "GridSplitter peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(separator) is SeparatorAutomationPeer, "Separator peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(uc) is UserControlAutomationPeer, "UserControl peer");

        tickBar.Measure(new Size(100, 10));
        track.Measure(new Size(100, 20));
        bullet.Measure(new Size(100, 30));
        grip.Measure(new Size(16, 16));
        groupItem.Measure(new Size(100, 30));
        AssertEqual(label.Content != null, true, "Label content");

        Pass("CTRL-2. Foundation Controls, Primitives, Converters & AutomationPeers");
    }

    private static void TestBatch2SelectionTabsListViewTreeView()
    {
        var listBox = new ListBox();
        listBox.Items.Add("Alpha");
        listBox.Items.Add("Beta");
        listBox.Items.Add("Gamma");
        listBox.SelectedIndex = 1;
        AssertEqual(listBox.SelectedItem, "Beta", "ListBox.SelectedItem");

        var combo = new ComboBox { IsEditable = true };
        combo.Items.Add("One");
        combo.Items.Add("Two");
        combo.SelectedIndex = 0;
        AssertEqual(combo.SelectedItem, "One", "ComboBox.SelectedItem");

        var tabs = new TabControl();
        var tab1 = new TabItem { Header = "First", Content = new TextBlock { Text = "Tab1" } };
        var tab2 = new TabItem { Header = "Second", Content = new TextBlock { Text = "Tab2" } };
        tabs.Items.Add(tab1);
        tabs.Items.Add(tab2);
        tabs.SelectedIndex = 1;
        AssertEqual(tabs.SelectedItem, tab2, "TabControl.SelectedItem");
        var tabPanel = new TabPanel();
        tabPanel.Measure(new Size(200, 30));

        var gv = new GridView();
        gv.Columns.Add(new GridViewColumn { Header = "Name", Width = 100 });
        gv.Columns.Add(new GridViewColumn { Header = "Role", Width = 80 });
        var listView = new ListView { View = gv };
        listView.Items.Add("Alice");
        AssertEqual(gv.Columns.Count, 2, "GridView.Columns.Count");
        var headerPresenter = new GridViewHeaderRowPresenter { Columns = gv.Columns };
        headerPresenter.Measure(new Size(200, 30));
        var rowPresenter = new GridViewRowPresenter { Columns = gv.Columns };
        rowPresenter.Measure(new Size(200, 30));

        var tree = new TreeView();
        var rootNode = new TreeViewItem { Header = "Root", IsExpanded = true };
        var childNode = new TreeViewItem { Header = "Leaf", IsSelected = true };
        rootNode.Items.Add(childNode);
        tree.Items.Add(rootNode);
        AssertEqual(childNode.IsSelected, true, "TreeViewItem.IsSelected");

        Assert(UIElementAutomationPeer.CreatePeerForElement(listBox) is ListBoxAutomationPeer, "ListBox peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(combo) is ComboBoxAutomationPeer, "ComboBox peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(tabs) is TabControlAutomationPeer, "TabControl peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(listView) is ListViewAutomationPeer, "ListView peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(tree) is TreeViewAutomationPeer, "TreeView peer");

        Pass("CTRL-3. ListBox, ComboBox, TabControl, ListView/GridView & TreeView");
    }

    private static void TestBatch3MenusToolBarStatusBarToolTip()
    {
        var menu = new Menu();
        var fileMenu = new MenuItem { Header = "_File" };
        var checkItem = new MenuItem { Header = "Auto Save", IsCheckable = true, IsChecked = true };
        fileMenu.Items.Add(checkItem);
        menu.Items.Add(fileMenu);
        menu.Measure(new Size(300, 28));

        var ctx = new ContextMenu();
        ctx.Items.Add(new MenuItem { Header = "Copy" });
        var target = new Border { ContextMenu = ctx, ToolTip = "Right-click me" };
        AssertEqual(ContextMenuService.GetContextMenu(target), ctx, "ContextMenu attachment");
        AssertEqual(ToolTipService.GetToolTip(target), "Right-click me", "ToolTip attachment");

        var tray = new ToolBarTray { Orientation = Orientation.Horizontal };
        var tb = new ToolBar { Band = 0, BandIndex = 0 };
        tb.Items.Add(new Button { Content = "New" });
        tb.Items.Add(new Separator());
        tray.ToolBars.Add(tb);
        tray.Measure(new Size(300, 40));

        var statusBar = new StatusBar();
        var statusItem = new StatusBarItem { Content = "Ready" };
        statusBar.Items.Add(statusItem);
        statusBar.Measure(new Size(300, 24));

        var popup = new Popup { Child = new TextBlock { Text = "Pop" } };
        Assert(popup.Child != null, "Popup child");

        Assert(UIElementAutomationPeer.CreatePeerForElement(menu) is MenuAutomationPeer, "Menu peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(ctx) is ContextMenuAutomationPeer, "ContextMenu peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(tb) is ToolBarAutomationPeer, "ToolBar peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(statusBar) is StatusBarAutomationPeer, "StatusBar peer");

        Pass("CTRL-4. Menu, MenuItem, ContextMenu, ToolBar/Tray, StatusBar & ToolTip");
    }

    private static void TestBatch4CalendarAndDatePicker()
    {
        var cal = new System.Windows.Controls.Calendar
        {
            DisplayDate = new DateTime(2026, 5, 1),
            SelectionMode = CalendarSelectionMode.SingleRange
        };
        cal.SelectedDates.Add(new DateTime(2026, 5, 10));
        cal.BlackoutDates.Add(new CalendarDateRange(new DateTime(2026, 5, 20), new DateTime(2026, 5, 22)));
        AssertEqual(cal.SelectedDates.Count, 1, "Calendar.SelectedDates");
        Assert(cal.BlackoutDates.Contains(new DateTime(2026, 5, 21)), "Calendar.BlackoutDates contains");

        var dp = new DatePicker
        {
            SelectedDate = new DateTime(2026, 5, 12),
            SelectedDateFormat = DatePickerFormat.Short
        };
        AssertEqual(dp.SelectedDate, new DateTime(2026, 5, 12), "DatePicker.SelectedDate");

        Assert(UIElementAutomationPeer.CreatePeerForElement(cal) is CalendarAutomationPeer, "Calendar peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(dp) is DatePickerAutomationPeer, "DatePicker peer");

        Pass("CTRL-5. Calendar & DatePicker with SelectedDates, BlackoutDates & Peers");
    }

    private static void TestBatch5DataGridSuite()
    {
        var items = new ObservableCollection<GridRowItem>
        {
            new GridRowItem { Name = "Alice", Age = 30, Active = true, Website = new Uri("https://example.com") },
            new GridRowItem { Name = "Bob", Age = 25, Active = false, Website = new Uri("https://contoso.com") }
        };

        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = items,
            Width = 400,
            Height = 200
        };
        dg.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") });
        dg.Columns.Add(new DataGridCheckBoxColumn { Header = "Active", Binding = new Binding("Active") });
        dg.Columns.Add(new DataGridHyperlinkColumn { Header = "Website", Binding = new Binding("Website") });
        dg.Columns.Add(new DataGridComboBoxColumn { Header = "Age" });
        dg.Columns.Add(new DataGridTemplateColumn { Header = "Custom" });

        dg.Measure(new Size(400, 200));
        dg.Arrange(new Rect(0, 0, 400, 200));

        AssertEqual(dg.Columns.Count, 5, "DataGrid.Columns.Count");
        dg.SelectedIndex = 0;
        Assert(dg.SelectedItem is GridRowItem selected && selected.Name == "Alice", "DataGrid selection");
        Assert(UIElementAutomationPeer.CreatePeerForElement(dg) is DataGridAutomationPeer, "DataGrid peer");

        Pass("CTRL-6. DataGrid (Text/CheckBox/Hyperlink/ComboBox/Template columns, selection, peers)");
    }

    private static void TestBatch6NavigationInkDocumentsAndHosting()
    {
        // 1. Navigation
        var page1 = new Page { Title = "Home", Content = new TextBlock { Text = "Home Page" } };
        var page2 = new Page { Title = "Details", Content = new TextBlock { Text = "Details Page" } };
        var frame = new Frame { Width = 300, Height = 200 };
        frame.Navigate(page1);
        AssertEqual(frame.Content, page1, "Frame first page");
        AssertEqual(frame.CanGoBack, false, "Frame CanGoBack false on first page");
        AssertEqual(NavigationService.GetNavigationService(page1), frame.NavigationService, "NavigationService inherited on Page");

        frame.Navigate(page2);
        AssertEqual(frame.Content, page2, "Frame second page");
        AssertEqual(frame.CanGoBack, true, "Frame CanGoBack true");
        frame.GoBack();
        AssertEqual(frame.Content, page1, "Frame GoBack");
        AssertEqual(frame.CanGoForward, true, "Frame CanGoForward");
        frame.GoForward();
        AssertEqual(frame.Content, page2, "Frame GoForward");

        bool fragmentHit = false;
        frame.FragmentNavigation += (_, e) => { if (e.Fragment == "sec2") fragmentHit = true; };
        frame.Navigate(new Uri("page.xaml#sec2", UriKind.Relative));
        Assert(fragmentHit, "FragmentNavigation raised");
        Assert(UIElementAutomationPeer.CreatePeerForElement(frame) is FrameAutomationPeer, "Frame peer");

        // 2. Ink
        var inkCanvas = new InkCanvas { Width = 120, Height = 80, Background = Brushes.White };
        var pts = new StylusPointCollection { new StylusPoint(10, 40), new StylusPoint(100, 40) };
        var da = new DrawingAttributes { Color = Colors.Red, Width = 6, Height = 6 };
        var stroke = new Stroke(pts, da);
        inkCanvas.Strokes.Add(stroke);
        AssertEqual(inkCanvas.Strokes.Count, 1, "InkCanvas.Strokes.Count");
        Assert(stroke.HitTest(new Point(50, 40), 4.0), "Stroke HitTest hit");
        Assert(!stroke.HitTest(new Point(50, 10), 2.0), "Stroke HitTest miss");

        inkCanvas.Measure(new Size(120, 80));
        inkCanvas.Arrange(new Rect(0, 0, 120, 80));
        var pxInk = RenderAndSamplePixel(inkCanvas, 120, 80, 50, 40);
        Assert(pxInk.R > 200 && pxInk.G < 40 && pxInk.B < 40, $"Expected red ink stroke pixel, got R={pxInk.R},G={pxInk.G}");

        using (var ms = new MemoryStream())
        {
            inkCanvas.Strokes.Save(ms);
            Assert(ms.Length > 0, "StrokeCollection.Save wrote data");
        }
        using (var recognizer = new GestureRecognizer())
        {
            var gres = recognizer.Recognize(inkCanvas.Strokes);
            AssertEqual(gres[0].ApplicationGesture, ApplicationGesture.Right, "GestureRecognizer Right");
        }
        Assert(UIElementAutomationPeer.CreatePeerForElement(inkCanvas) is InkCanvasAutomationPeer, "InkCanvas peer");

        // 3. FlowDocument, FixedDocument, RichTextBox & Document Viewers
        var flowDoc = new FlowDocument();
        var para = new Paragraph(new Run("Hello RichText"));
        var link = new Hyperlink(new Run("Open")) { NavigateUri = new Uri("https://example.com") };
        para.Inlines.Add(link);
        flowDoc.Blocks.Add(para);

        var rtb = new RichTextBox(flowDoc) { Width = 240, Height = 80 };
        rtb.Measure(new Size(240, 80));
        rtb.Arrange(new Rect(0, 0, 240, 80));
        var rtbPeer = UIElementAutomationPeer.CreatePeerForElement(rtb) as RichTextBoxAutomationPeer;
        Assert(rtbPeer != null, "RichTextBox peer");
        AssertEqual(((IValueProvider)rtbPeer!).Value, "Hello RichTextOpen", "RichTextBox IValueProvider.Value");
        rtb.AppendText("!");
        AssertEqual(flowDoc.GetPlainText(), "Hello RichTextOpen!", "RichTextBox AppendText");
        AssertEqual(rtb.SpellCheck.IsEnabled, false, "SpellCheck default false");

        var docViewer = new DocumentViewer { Document = flowDoc };
        AssertEqual(docViewer.PageCount, 1, "DocumentViewer PageCount");
        docViewer.IncreaseZoom();
        Assert(docViewer.Zoom > 100.0, "DocumentViewer Zoom increased");

        var fixedPage = new FixedPage { Width = 200, Height = 300 };
        var fixedDoc = new FixedDocument();
        fixedDoc.Pages.Add(new PageContent { Child = fixedPage });
        var fdPageViewer = new FlowDocumentPageViewer { Document = fixedDoc };
        AssertEqual(fdPageViewer.PageCount, 1, "FlowDocumentPageViewer PageCount");
        var fdScrollViewer = new FlowDocumentScrollViewer { Document = flowDoc };
        var fdReader = new FlowDocumentReader { Document = flowDoc, ViewingMode = FlowDocumentReaderViewingMode.TwoPage };
        AssertEqual(fdReader.PageCount, 1, "FlowDocumentReader PageCount");
        var note = new StickyNoteControl { Author = "Reviewer" };
        AssertEqual(note.Author, "Reviewer", "StickyNoteControl Author");

        // 4. Hosting, PrintDialog, MediaElement, WebBrowser, SoundPlayerAction & Text Peers
        using (var host = new TestHwndHost())
        {
            host.Measure(new Size(100, 50));
            host.Arrange(new Rect(0, 0, 100, 50));
            Assert(host.Built, "HwndHost BuildWindowCore invoked");
            AssertEqual(host.Handle, new IntPtr(0x1234), "HwndHost Handle");
        }

        var wb = new WebBrowser();
        wb.NavigateToString("<html><body>Hi</body></html>");
        AssertEqual(wb.Document, "<html><body>Hi</body></html>", "WebBrowser.NavigateToString");

        var media = new MediaElement { LoadedBehavior = MediaState.Manual };
        media.Play();
        Assert(media.CanPause, "MediaElement Play -> CanPause");
        media.Stop();

        using var soundAction = new SoundPlayerAction { Source = new Uri("click.wav", UriKind.Relative) };

        var printDialog = new PrintDialog { MinPage = 1, MaxPage = 10, PageRange = new PageRange(5, 2) };
        AssertEqual(printDialog.PageRange.PageFrom, 2, "PrintDialog normalized PageFrom");
        AssertEqual(printDialog.PageRange.PageTo, 5, "PrintDialog normalized PageTo");
        printDialog.PrintDocument(flowDoc.DocumentPaginator, "TestPrint");

        var tb = new TextBlock { Text = "Caption" };
        var tbox = new TextBox { Text = "Editable" };
        var pbox = new PasswordBox { Password = "Secret" };
        Assert(UIElementAutomationPeer.CreatePeerForElement(tb) is TextBlockAutomationPeer, "TextBlock peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(tbox) is TextBoxAutomationPeer, "TextBox peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(pbox) is PasswordBoxAutomationPeer, "PasswordBox peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(fdScrollViewer) is FlowDocumentScrollViewerAutomationPeer, "FlowDocumentScrollViewer peer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(fdReader) is FlowDocumentReaderAutomationPeer, "FlowDocumentReader peer");

        Pass("CTRL-7. Navigation, InkCanvas, RichTextBox, DocumentViewers, HwndHost, WebBrowser, MediaElement & Peers");
    }

    private static void TestBatch7MessageBoxFileDialogsGlyphsAndRibbon()
    {
        // 1. MessageBox validation & Win32 flags
        AssertThrows<InvalidEnumArgumentException>(() =>
            MessageBox.Show("Test", "Cap", (MessageBoxButton)999), "Invalid MessageBoxButton");
        AssertThrows<InvalidEnumArgumentException>(() =>
            MessageBox.Show("Test", "Cap", MessageBoxButton.OK, (MessageBoxImage)999), "Invalid MessageBoxImage");
        AssertThrows<ArgumentException>(() =>
            MessageBox.ShowCore(new IntPtr(1), "Test", "Cap", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.ServiceNotification),
            "ServiceNotification with owner");
        AssertEqual(MessageBox.DefaultResultToButtonNumber(MessageBoxResult.Cancel, MessageBoxButton.YesNoCancel), 0x200, "MessageBox MB_DEFBUTTON3");
        AssertEqual(MessageBox.Win32ToMessageBoxResult(6), MessageBoxResult.Yes, "MessageBox IDYES mapping");

        // 2. Microsoft.Win32 File & Folder Dialogs
        var ofd = new OpenFileDialog
        {
            Title = "Open",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            Multiselect = true,
            FileName = @"C:\Temp\sample.txt"
        };
        AssertEqual(ofd.DefaultExt, "txt", "OpenFileDialog stripped dot in DefaultExt");
        AssertEqual(ofd.SafeFileName, "sample.txt", "OpenFileDialog SafeFileName");
        AssertThrows<ArgumentException>(() => ofd.Filter = "BrokenFilterOnlyOnePart", "Invalid Filter string");
        ofd.CustomPlaces.Add(FileDialogCustomPlaces.Documents);
        AssertEqual(ofd.CustomPlaces.Count, 1, "OpenFileDialog CustomPlaces");
        ofd.Reset();
        AssertEqual(ofd.Multiselect, false, "OpenFileDialog Reset");

        var sfd = new SaveFileDialog { OverwritePrompt = true, CreatePrompt = true };
        Assert(sfd.OverwritePrompt && sfd.CreatePrompt, "SaveFileDialog flags");

        var folderDlg = new OpenFolderDialog { Multiselect = true, FolderName = @"C:\Windows\System32" };
        AssertEqual(folderDlg.SafeFolderName, "System32", "OpenFolderDialog SafeFolderName");
        folderDlg.Reset();
        AssertEqual(folderDlg.Multiselect, false, "OpenFolderDialog Reset");

        // 3. Glyphs element & GlyphRun rendering
        string fontPath = File.Exists(@"C:\Windows\Fonts\segoeui.ttf")
            ? @"C:\Windows\Fonts\segoeui.ttf"
            : @"C:\Windows\Fonts\arial.ttf";
        if (File.Exists(fontPath))
        {
            var glyphs = new Glyphs
            {
                FontUri = new Uri(fontPath, UriKind.Absolute),
                FontRenderingEmSize = 18.0,
                UnicodeString = "CSUI",
                Fill = Brushes.Navy,
                OriginX = 8,
                OriginY = 24,
                Width = 100,
                Height = 40
            };
            GlyphRun run = glyphs.ToGlyphRun();
            Assert(run != null && run.GlyphIndices.Count == 4, "Glyphs ToGlyphRun built 4 glyphs");
            glyphs.Measure(new Size(100, 40));
            glyphs.Arrange(new Rect(0, 0, 100, 40));
            byte[] glyphPixels = RenderToBgra32(glyphs, 100, 40);
            Assert(HasNonBackgroundPixels(glyphPixels, 100, 40, 100 * 4), "Glyphs rendered pixels");
        }

        // 4. Complete System.Windows.Controls.Ribbon suite
        var conv = new RibbonControlLengthConverter();
        object? converted = conv.ConvertFromString("2*");
        Assert(converted is RibbonControlLength starLen && starLen.IsStar && Math.Abs(starLen.Value - 2.0) < 1e-6, "RibbonControlLengthConverter star");

        var ribbonWindow = new RibbonWindow { Title = "App" };
        Assert(UIElementAutomationPeer.CreatePeerForElement(ribbonWindow) is RibbonWindowAutomationPeer, "RibbonWindow peer");

        var ribbonBtn = new RibbonButton { Label = "Paste", KeyTip = "V" };
        int splitClicks = 0;
        var splitBtn = new RibbonSplitButton { Label = "Split", IsCheckable = true };
        splitBtn.Click += (_, _) => splitClicks++;
        var toggleBtn = new RibbonToggleButton { Label = "Bold", IsChecked = true };
        var radioBtn = new RibbonRadioButton { Label = "Opt1" };
        var checkBtn = new RibbonCheckBox { Label = "Gridlines", IsChecked = true };
        var rTextBox = new RibbonTextBox { Text = "Find", TextBoxWidth = 80 };
        var rCombo = new RibbonComboBox { Label = "Font", IsEditable = true, Text = "Segoe UI" };
        var rFilter = new RibbonFilterMenuButton { Label = "Filter" };
        var rSep = new RibbonSeparator { Label = "More" };
        var rTwoLine = new RibbonTwoLineText { Text = "Wrap Text", HasTwoLines = true };
        var rTip = new RibbonToolTip { Title = "Help", Description = "Desc", FooterTitle = "F1" };
        Assert(rTip.HasHeader && rTip.HasFooter, "RibbonToolTip HasHeader/HasFooter");

        var gItem1 = new RibbonGalleryItem { Content = "Style1" };
        var gItem2 = new RibbonGalleryItem { Content = "Style2" };
        var gCat = new RibbonGalleryCategory { Header = "Styles" };
        gCat.Items.Add(gItem1);
        gCat.Items.Add(gItem2);
        var gallery = new RibbonGallery { MaxColumnCount = 4 };
        gallery.Items.Add(gCat);

        var appMenu = new RibbonApplicationMenu { Label = "File" };
        appMenu.Items.Add(new RibbonApplicationMenuItem { Header = "New" });
        appMenu.Items.Add(new RibbonApplicationSplitMenuItem { Header = "Save As" });

        var qat = new RibbonQuickAccessToolBar();
        qat.Items.Add(new RibbonButton { Label = "Save" });

        var ctrlGroup = new RibbonControlGroup();
        ctrlGroup.Items.Add(toggleBtn);
        ctrlGroup.Items.Add(radioBtn);

        var groupSizeDef = new RibbonGroupSizeDefinition();
        groupSizeDef.ControlDefinitions.Add(new RibbonControlSizeDefinition { ImageSize = RibbonImageSize.Large });

        var homeGroup = new RibbonGroup { Header = "Clipboard" };
        homeGroup.GroupSizeDefinitions.Add(groupSizeDef);
        homeGroup.Items.Add(ribbonBtn);
        homeGroup.Items.Add(splitBtn);
        homeGroup.Items.Add(ctrlGroup);
        homeGroup.Items.Add(checkBtn);
        homeGroup.Items.Add(rTextBox);
        homeGroup.Items.Add(rCombo);
        homeGroup.Items.Add(rFilter);
        homeGroup.Items.Add(rSep);
        homeGroup.Items.Add(rTwoLine);
        homeGroup.Items.Add(gallery);

        var homeTab = new RibbonTab { Header = "Home" };
        homeTab.Items.Add(homeGroup);
        var insertTab = new RibbonTab { Header = "Insert" };

        var ribbon = new Ribbon
        {
            Width = 640,
            Height = 140,
            ApplicationMenu = appMenu,
            QuickAccessToolBar = qat
        };
        ribbon.ContextualTabGroups.Add(new RibbonContextualTabGroup { Header = "Tools" });
        ribbon.Items.Add(homeTab);
        ribbon.Items.Add(insertTab);

        AssertEqual(ribbon.SelectedIndex, 0, "Ribbon auto-selects first RibbonTab");
        ribbon.Measure(new Size(640, 140));
        ribbon.Arrange(new Rect(0, 0, 640, 140));
        byte[] ribbonPixels = RenderToBgra32(ribbon, 640, 140);
        Assert(HasNonBackgroundPixels(ribbonPixels, 640, 140, 640 * 4), "Ribbon rendered visual chrome & controls");

        RibbonCommands.MinimizeRibbonCommand.Execute(null, ribbon);
        Assert(ribbon.IsMinimized, "RibbonCommands.MinimizeRibbonCommand sets IsMinimized");
        RibbonCommands.MaximizeRibbonCommand.Execute(null, ribbon);
        Assert(!ribbon.IsMinimized, "RibbonCommands.MaximizeRibbonCommand clears IsMinimized");

        var rPeer = UIElementAutomationPeer.CreatePeerForElement(ribbon) as RibbonAutomationPeer;
        Assert(rPeer != null, "RibbonAutomationPeer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(homeTab) is RibbonTabAutomationPeer, "RibbonTabAutomationPeer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(homeGroup) is RibbonGroupAutomationPeer, "RibbonGroupAutomationPeer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(ribbonBtn) is RibbonButtonAutomationPeer, "RibbonButtonAutomationPeer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(toggleBtn) is RibbonToggleButtonAutomationPeer, "RibbonToggleButtonAutomationPeer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(appMenu) is RibbonApplicationMenuAutomationPeer, "RibbonApplicationMenuAutomationPeer");
        Assert(UIElementAutomationPeer.CreatePeerForElement(qat) is RibbonQuickAccessToolBarAutomationPeer, "RibbonQuickAccessToolBarAutomationPeer");

        var splitPeer = UIElementAutomationPeer.CreatePeerForElement(splitBtn) as RibbonSplitButtonAutomationPeer;
        Assert(splitPeer != null, "RibbonSplitButtonAutomationPeer");
        ((IToggleProvider)splitPeer!).Toggle();
        Assert(splitBtn.IsChecked, "RibbonSplitButton IToggleProvider.Toggle");
        splitBtn.IsCheckable = false;
        ((IInvokeProvider)splitPeer).Invoke();
        AssertEqual(splitClicks, 1, "RibbonSplitButton IInvokeProvider.Invoke");

        var giPeer = UIElementAutomationPeer.CreatePeerForElement(gItem2) as RibbonGalleryItemAutomationPeer;
        Assert(giPeer != null, "RibbonGalleryItemAutomationPeer");
        ((ISelectionItemProvider)giPeer!).Select();
        Assert(gItem2.IsSelected, "RibbonGalleryItem ISelectionItemProvider.Select");

        KeyTipService.SetKeyTip(ribbonBtn, "PT");
        AssertEqual(KeyTipService.GetKeyTip(ribbonBtn), "PT", "KeyTipService GetKeyTip");

        Pass("CTRL-8. MessageBox, FileDialogs, Glyphs & Complete Ribbon Suite + Peers");
    }

    private static T? FindVisualPart<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T fe && fe.Name == name)
        {
            return fe;
        }
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            T? found = FindVisualPart<T>(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private static void TestAero2AuthenticDefaultThemes()
    {
        var host = new StackPanel();
        T LayoutControl<T>(T c, double w, double h) where T : FrameworkElement
        {
            host.Children.Add(c);
            c.Measure(new Size(w, h));
            c.Arrange(new Rect(0, 0, w, h));
            return c;
        }

        var btn = LayoutControl(new Button { Content = "Aero2" }, 120, 40);
        Assert(btn.Template != null, "Aero2 Button applies default ControlTemplate");
        Assert(FindVisualPart<Border>(btn, "border") != null, "Aero2 Button resolves named 'border'");

        var chk = LayoutControl(new CheckBox { Content = "Check" }, 120, 30);
        Assert(FindVisualPart<System.Windows.Shapes.Path>(chk, "optionMark") != null, "Aero2 CheckBox resolves 'optionMark'");
        Assert(FindVisualPart<System.Windows.Shapes.Rectangle>(chk, "indeterminateMark") != null, "Aero2 CheckBox resolves 'indeterminateMark'");

        var rb = LayoutControl(new RadioButton { Content = "Radio" }, 120, 30);
        Assert(FindVisualPart<Ellipse>(rb, "optionMark") != null, "Aero2 RadioButton resolves 'optionMark'");

        var tb = LayoutControl(new TextBox { Text = "Hello" }, 160, 30);
        Assert(tb.Template != null, "Aero2 TextBox has Template");
        Assert(FindVisualPart<ScrollViewer>(tb, "PART_ContentHost") != null, "Aero2 TextBox resolves 'PART_ContentHost'");

        var pb = LayoutControl(new PasswordBox { Password = "Secret" }, 160, 30);
        Assert(FindVisualPart<ScrollViewer>(pb, "PART_ContentHost") != null, "Aero2 PasswordBox resolves 'PART_ContentHost'");

        var prog = LayoutControl(new ProgressBar { Value = 50, Maximum = 100 }, 160, 20);
        Assert(FindVisualPart<FrameworkElement>(prog, "PART_Track") != null, "Aero2 ProgressBar resolves 'PART_Track'");
        Assert(FindVisualPart<FrameworkElement>(prog, "PART_Indicator") != null, "Aero2 ProgressBar resolves 'PART_Indicator'");
        Assert(FindVisualPart<FrameworkElement>(prog, "Indicator") != null, "Aero2 ProgressBar resolves 'Indicator'");
        Assert(FindVisualPart<FrameworkElement>(prog, "Animation") != null, "Aero2 ProgressBar resolves 'Animation'");

        var sbar = LayoutControl(new ScrollBar { Orientation = Orientation.Vertical, Maximum = 100 }, 20, 140);
        Assert(FindVisualPart<Track>(sbar, "PART_Track") != null, "Aero2 ScrollBar resolves 'PART_Track'");

        var slider = LayoutControl(new Slider { Minimum = 0, Maximum = 100, Value = 40 }, 160, 30);
        Assert(FindVisualPart<Track>(slider, "PART_Track") != null, "Aero2 Slider resolves 'PART_Track'");

        var combo = new ComboBox();
        combo.Items.Add("A");
        LayoutControl(combo, 160, 30);
        Assert(FindVisualPart<ToggleButton>(combo, "toggleButton") != null, "Aero2 ComboBox resolves 'toggleButton'");
        Assert(FindVisualPart<Popup>(combo, "PART_Popup") != null, "Aero2 ComboBox resolves 'PART_Popup'");

        var tab = new TabControl();
        tab.Items.Add(new TabItem { Header = "Tab1", Content = "Page1" });
        LayoutControl(tab, 240, 140);
        Assert(FindVisualPart<ContentPresenter>(tab, "PART_SelectedContentHost") != null, "Aero2 TabControl resolves 'PART_SelectedContentHost'");
        Assert(FindVisualPart<TabPanel>(tab, "HeaderPanel") != null, "Aero2 TabControl resolves 'HeaderPanel'");

        var listBox = new ListBox();
        listBox.Items.Add("Item1");
        LayoutControl(listBox, 160, 120);
        Assert(FindVisualPart<Border>(listBox, "Bd") != null, "Aero2 ListBox resolves 'Bd'");

        var dg = LayoutControl(new DataGrid(), 300, 180);
        Assert(FindVisualPart<ScrollViewer>(dg, "DG_ScrollViewer") != null, "Aero2 DataGrid resolves 'DG_ScrollViewer'");

        var dp = LayoutControl(new DatePicker(), 180, 32);
        Assert(FindVisualPart<DatePickerTextBox>(dp, "PART_TextBox") != null, "Aero2 DatePicker resolves 'PART_TextBox'");
        Assert(FindVisualPart<Button>(dp, "PART_Button") != null, "Aero2 DatePicker resolves 'PART_Button'");

        var cal = LayoutControl(new System.Windows.Controls.Calendar(), 220, 220);
        Assert(FindVisualPart<CalendarItem>(cal, "PART_CalendarItem") != null, "Aero2 Calendar resolves 'PART_CalendarItem'");

        var exp = LayoutControl(new Expander { Header = "More", Content = "Details" }, 180, 100);
        Assert(FindVisualPart<ToggleButton>(exp, "HeaderSite") != null, "Aero2 Expander resolves 'HeaderSite'");
        Assert(FindVisualPart<ContentPresenter>(exp, "ExpandSite") != null, "Aero2 Expander resolves 'ExpandSite'");

        Pass("CTRL-9. Authentic Aero2 Default Themes & ControlTemplate PART_* Wiring");
    }

    private static bool HasNonBackgroundPixels(byte[] pixels, int width, int height, int stride)
    {
        for (int y = 0; y < height; y++)
        {
            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                int i = row + x * 4;
                if (pixels[i + 3] != 0)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
