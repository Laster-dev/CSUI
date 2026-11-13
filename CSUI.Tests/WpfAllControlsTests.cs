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
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
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
        TestAero2BehaviorAndTriggers();
        TestAero2LayoutTextAndInheritance();
        TestTextBoxCaretBlinkAndOutsideFocusClear();
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

    private sealed class TestToggleCommand : ICommand
    {
        private bool _canExecute;

        public TestToggleCommand(bool canExecute) => _canExecute = canExecute;

        public bool CanExecuteResult
        {
            get => _canExecute;
            set
            {
                if (_canExecute != value)
                {
                    _canExecute = value;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _canExecute;
        public void Execute(object? parameter) { }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private static void PumpDispatcher(int milliseconds = 25)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(milliseconds),
            DispatcherPriority.Background,
            (s, _) =>
            {
                ((DispatcherTimer)s!).Stop();
                frame.Continue = false;
            },
            Dispatcher.CurrentDispatcher);
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static void TestAero2BehaviorAndTriggers()
    {
        // 1. TextBox: auto-sizing inside Grid (300x100) must not stretch to 300px when auto-sized
        var grid = new Grid { Width = 300, Height = 100 };
        var tbAuto = new TextBox
        {
            Text = "Hi",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        grid.Children.Add(tbAuto);
        grid.Measure(new Size(300, 100));
        grid.Arrange(new Rect(0, 0, 300, 100));
        Assert(tbAuto.ActualWidth > 0 && tbAuto.ActualWidth < 200,
            $"Auto-sized TextBox in 300x100 Grid should not stretch to 300px, got ActualWidth={tbAuto.ActualWidth}");

        var host = new StackPanel();
        T LayoutControl<T>(T c, double w, double h) where T : FrameworkElement
        {
            host.Children.Add(c);
            c.Measure(new Size(w, h));
            c.Arrange(new Rect(0, 0, w, h));
            return c;
        }

        // Border Padding applied & SelectionBrush / SelectionTextBrush non-null
        var tbPadded = LayoutControl(new TextBox
        {
            Text = "Hello",
            Padding = new Thickness(10, 6, 10, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        }, 300, 100);
        var tbBorder = FindVisualPart<Border>(tbPadded, "border");
        Assert(tbBorder != null, "TextBox template must contain 'border'");
        AssertEqual(tbBorder!.Padding, new Thickness(10, 6, 10, 6), "TextBox Border Padding");
        Assert(tbPadded.SelectionBrush != null, "TextBox SelectionBrush must be non-null");
        Assert(tbPadded.SelectionTextBrush != null, "TextBox SelectionTextBrush must be non-null");

        // IsEnabled=false sets border Opacity=0.56
        tbPadded.IsEnabled = false;
        AssertNear(tbBorder.Opacity, 0.56, 0.001, "Disabled TextBox border Opacity == 0.56");
        tbPadded.IsEnabled = true;

        // TextWrapping="Wrap" wraps text to narrower height > single line
        string longText = "The quick brown fox jumps over the lazy dog repeatedly in a narrow box.";
        var tbSingle = new TextBox { Text = longText, TextWrapping = TextWrapping.NoWrap, Width = 100 };
        var tbWrap = new TextBox { Text = longText, TextWrapping = TextWrapping.Wrap, Width = 100 };
        host.Children.Add(tbSingle);
        host.Children.Add(tbWrap);
        tbSingle.Measure(new Size(100, 1000));
        tbWrap.Measure(new Size(100, 1000));
        Assert(tbWrap.DesiredSize.Height > tbSingle.DesiredSize.Height,
            $"Wrapped TextBox height ({tbWrap.DesiredSize.Height}) should exceed single-line height ({tbSingle.DesiredSize.Height})");

        // IsMouseOver / IsKeyboardFocused triggers update border.BorderBrush to #FF7EB4EA / #FF569DE5
        var hwndParams = new HwndSourceParameters("CTRL10_Host")
        {
            Width = 320,
            Height = 200,
            PositionX = -32000,
            PositionY = -32000,
            WindowStyle = unchecked((int)0x90000000) // WS_POPUP | WS_VISIBLE (offscreen)
        };
        using (var hwndSource = new HwndSource(hwndParams))
        {
            var hostGrid = new Grid { Width = 300, Height = 180 };
            var tbTrigger = new TextBox
            {
                Text = "FocusAndHover",
                Width = 160,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var rtb = new RichTextBox
            {
                Width = 160,
                Height = 60,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 40, 0, 0)
            };
            rtb.AppendText("RichTextBox content");
            hostGrid.Children.Add(tbTrigger);
            hostGrid.Children.Add(rtb);
            hwndSource.RootVisual = hostGrid;
            PumpDispatcher();

            var trigBorder = FindVisualPart<Border>(tbTrigger, "border");
            Assert(trigBorder != null, "Trigger TextBox must have 'border'");

            // Simulate mouse move over tbTrigger center (80, 15) -> WM_SETCURSOR (0x0020) + WM_MOUSEMOVE (0x0200)
            SendMessage(hwndSource.Handle, 0x0020, hwndSource.Handle, new IntPtr((0x0200 << 16) | 1));
            IntPtr lParam = new IntPtr((15 << 16) | 80);
            SendMessage(hwndSource.Handle, 0x0200, IntPtr.Zero, lParam);
            PumpDispatcher(10);
            if (tbTrigger.IsMouseOver)
            {
                var hoverBrush = trigBorder!.BorderBrush as SolidColorBrush;
                Assert(hoverBrush != null && hoverBrush.Color == Color.FromArgb(0xFF, 0x7E, 0xB4, 0xEA),
                    $"Expected MouseOver BorderBrush #FF7EB4EA, got {hoverBrush?.Color}");
            }

            // Keyboard focus -> IsKeyboardFocused trigger updates BorderBrush to #FF569DE5
            bool focused = tbTrigger.Focus();
            Assert(focused || tbTrigger.IsKeyboardFocused, "TextBox.Focus() should succeed inside HwndSource");
            Assert(tbTrigger.IsKeyboardFocused, "TextBox.IsKeyboardFocused should be true after Focus()");
            var focusBrush = trigBorder!.BorderBrush as SolidColorBrush;
            Assert(focusBrush != null && focusBrush.Color == Color.FromArgb(0xFF, 0x56, 0x9D, 0xE5),
                $"Expected KeyboardFocused BorderBrush #FF569DE5, got {focusBrush?.Color}");

            // RichTextBox Focus + hit-test
            Assert(rtb.Focusable, "RichTextBox must be Focusable");
            rtb.Focus();
            Assert(rtb.IsKeyboardFocused, "RichTextBox.IsKeyboardFocused should be true after Focus()");
            HitTestResult? rtbHit = VisualTreeHelper.HitTest(rtb, new Point(20, 20));
            Assert(rtbHit != null, "VisualTreeHelper.HitTest on RichTextBox must return non-null");
            IInputElement? rtbInputHit = rtb.InputHitTest(new Point(20, 20));
            Assert(rtbInputHit != null, "RichTextBox.InputHitTest must return non-null");
            Assert(rtb.GetPositionFromPoint(new Point(20, 20), true) != null, "RichTextBox.GetPositionFromPoint must return non-null");
        }

        // PasswordBox CaretIndex & SelectAll
        var pb = new PasswordBox { Password = "secret", Width = 140, Height = 28 };
        pb.Measure(new Size(140, 28));
        pb.Arrange(new Rect(0, 0, 140, 28));
        pb.CaretIndex = 3;
        AssertEqual(pb.CaretIndex, 3, "PasswordBox CaretIndex set to 3");
        pb.CaretIndex = 99;
        AssertEqual(pb.CaretIndex, 6, "PasswordBox CaretIndex clamps to Password.Length");
        pb.SelectAll();
        AssertEqual(pb.CaretIndex, 6, "PasswordBox SelectAll moves CaretIndex to end");

        // 2. ButtonBase / Button: IsEnabledCore respects CommandDisabled without clobbering local IsEnabled
        var cmd = new TestToggleCommand(false);
        var cmdBtn = new Button { Content = "Cmd", Command = cmd, Width = 100, Height = 30 };
        cmdBtn.Measure(new Size(100, 30));
        cmdBtn.Arrange(new Rect(0, 0, 100, 30));
        Assert(!cmdBtn.IsEnabled, "Button should be disabled when Command.CanExecute() == false");
        cmd.CanExecuteResult = true;
        Assert(cmdBtn.IsEnabled, "Button should re-enable when Command.CanExecute() becomes true (local IsEnabled preserved)");
        cmdBtn.IsEnabled = false;
        cmd.CanExecuteResult = false;
        cmd.CanExecuteResult = true;
        Assert(!cmdBtn.IsEnabled, "Button with local IsEnabled=false must stay disabled even when Command.CanExecute() == true");

        // ToggleButton IsChecked triggers state
        var toggle = new ToggleButton { Content = "Toggle", Width = 100, Height = 30 };
        int checkedCount = 0;
        toggle.Checked += (_, _) => checkedCount++;
        LayoutControl(toggle, 100, 30);
        toggle.IsChecked = true;
        AssertEqual(toggle.IsChecked, true, "ToggleButton IsChecked == true");
        AssertEqual(checkedCount, 1, "ToggleButton Checked event fired");

        var chk = LayoutControl(new CheckBox { Content = "Check", Width = 100, Height = 30 }, 100, 30);
        var optMark = FindVisualPart<System.Windows.Shapes.Path>(chk, "optionMark");
        Assert(optMark != null, "CheckBox must resolve 'optionMark'");
        AssertNear(optMark!.Opacity, 0.0, 0.001, "Unchecked CheckBox optionMark Opacity == 0");
        chk.IsChecked = true;
        AssertNear(optMark.Opacity, 1.0, 0.001, "Checked CheckBox optionMark Opacity == 1");

        // ProgressBar PART_Indicator ActualWidth/Width reflects Value/Maximum (50/100 -> ~half width)
        var prog = LayoutControl(new ProgressBar { Minimum = 0, Maximum = 100, Value = 50, Width = 200, Height = 20 }, 200, 20);
        var progTrack = FindVisualPart<FrameworkElement>(prog, "PART_Track");
        var progIndicator = FindVisualPart<FrameworkElement>(prog, "PART_Indicator");
        Assert(progTrack != null && progIndicator != null, "ProgressBar PART_Track and PART_Indicator must exist");
        Assert(progTrack!.ActualWidth > 150, $"ProgressBar PART_Track ActualWidth expected > 150, got {progTrack.ActualWidth}");
        AssertNear(progIndicator!.Width, progTrack.ActualWidth * 0.5, 2.0, "ProgressBar PART_Indicator.Width == ~half track width");
        AssertNear(progIndicator.ActualWidth, progTrack.ActualWidth * 0.5, 2.0, "ProgressBar PART_Indicator.ActualWidth == ~half track width");

        // Slider PART_SelectionRange updates when IsSelectionRangeEnabled=true and Thumb drag with IsSnapToTickEnabled=true accumulates deltas
        var slider = LayoutControl(new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            SelectionStart = 20,
            SelectionEnd = 60,
            IsSelectionRangeEnabled = true,
            TickFrequency = 10,
            IsSnapToTickEnabled = true,
            Width = 200,
            Height = 30
        }, 200, 30);
        var selRange = FindVisualPart<FrameworkElement>(slider, "PART_SelectionRange");
        Assert(selRange != null, "Slider PART_SelectionRange must exist");
        AssertEqual(selRange!.Visibility, Visibility.Visible, "Slider PART_SelectionRange Visible when IsSelectionRangeEnabled=true");
        Assert(selRange.Width > 0, $"Slider PART_SelectionRange.Width expected > 0, got {selRange.Width}");

        var sliderTrack = FindVisualPart<Track>(slider, "PART_Track");
        Assert(sliderTrack?.Thumb != null, "Slider PART_Track.Thumb must exist");
        var thumb = sliderTrack!.Thumb!;
        thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        // Each 8px delta on ~190px track is ~4.2 units; 3 deltas (~12.6 units) must accumulate past TickFrequency=10 and snap to 10
        thumb.RaiseEvent(new DragDeltaEventArgs(8, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        thumb.RaiseEvent(new DragDeltaEventArgs(8, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        thumb.RaiseEvent(new DragDeltaEventArgs(8, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        thumb.RaiseEvent(new DragCompletedEventArgs(24, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        AssertNear(slider.Value, 10.0, 0.001, "Slider Thumb drag with IsSnapToTickEnabled=true accumulates deltas cleanly to snap to 10");

        // ScrollBar has VisualChildrenCount == 1 when templated
        var sbar = LayoutControl(new ScrollBar { Orientation = Orientation.Vertical, Maximum = 100, Width = 20, Height = 140 }, 20, 140);
        AssertEqual(VisualTreeHelper.GetChildrenCount(sbar), 1, "Templated ScrollBar VisualChildrenCount == 1");

        // 3. DataTrigger / PropertyPath: Aero2 DataTriggers with static CLR properties and Storyboard PropertyPath DP parameters
        var pp1 = new PropertyPath("(Border.Background).(SolidColorBrush.Color)");
        var pp2 = new PropertyPath("(0).(1)", Control.BackgroundProperty, SolidColorBrush.ColorProperty);
        Assert(pp1.Path == "(Border.Background).(SolidColorBrush.Color)", "PropertyPath string resolves");
        Assert(pp2.PathParameters.Count == 2, "PropertyPath DP parameters resolve");

        var sb = new Storyboard();
        var anim = new ColorAnimation(Colors.Red, Colors.Blue, TimeSpan.FromMilliseconds(100));
        Storyboard.SetTargetProperty(anim, pp2);
        sb.Children.Add(anim);
        AssertEqual(sb.Children.Count, 1, "Storyboard with PropertyPath DP parameters constructed without throwing");

        // Verify Aero2 TextBox & Button styles (which contain DataTriggers with static CLR properties) instantiate cleanly
        var dtTb = LayoutControl(new TextBox { Text = "DataTriggerTest" }, 120, 30);
        var dtBtn = LayoutControl(new Button { Content = "DataTriggerBtn" }, 120, 30);
        Assert(dtTb.Template != null && dtBtn.Template != null, "Aero2 TextBox and Button styles with DataTriggers resolve without throwing");

        Pass("CTRL-10. Aero2 Visual/Trigger Behavior, Layout, Input, ProgressBar, Slider, ScrollBar & PropertyPath");
    }

    private static void TestAero2LayoutTextAndInheritance()
    {
        static T? FindVisualDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    return match;
                }
                T? nested = FindVisualDescendant<T>(child);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        // 1. Button with Aero2 default style propagates black Foreground to ContentPresenter / TextBlock (not white)
        var btn = new Button { Content = "Aero2 Button" };
        btn.Measure(new Size(140, 36));
        btn.Arrange(new Rect(0, 0, 140, 36));
        Assert(btn.Foreground is SolidColorBrush btnFg && btnFg.Color == SystemColors.ControlTextColor,
            $"Button Foreground expected {SystemColors.ControlTextColor}, got {btn.Foreground}");
        var btnCp = FindVisualDescendant<ContentPresenter>(btn);
        Assert(btnCp != null, "Button ContentPresenter must exist");
        var cpFg = TextElement.GetForeground(btnCp!);
        Assert(cpFg is SolidColorBrush cpScb && cpScb.Color == SystemColors.ControlTextColor && cpScb.Color != Colors.White,
            $"Button ContentPresenter Foreground expected {SystemColors.ControlTextColor} (not White), got {cpFg}");
        var btnTb = FindVisualDescendant<TextBlock>(btnCp!);
        Assert(btnTb != null, "Button ContentPresenter should generate a TextBlock for string Content");
        Assert(btnTb!.Foreground is SolidColorBrush tbScb && tbScb.Color == SystemColors.ControlTextColor && tbScb.Color != Colors.White,
            $"Button inner TextBlock Foreground expected {SystemColors.ControlTextColor} (not White), got {btnTb.Foreground}");

        // 2. TextBlock with TextWrapping="Wrap" and default TextTrimming="None" formats multi-word text without dropping "ToggleButton"
        const string multiWordText = "ButtonBase: Button, ToggleButton, CheckBox, RadioButton";
        var wrapTb = new TextBlock
        {
            Text = multiWordText,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };
        AssertEqual(wrapTb.TextTrimming, TextTrimming.None, "TextBlock default TextTrimming must be None");

        var singleLineTb = new TextBlock
        {
            Text = "ButtonBase",
            TextWrapping = TextWrapping.NoWrap,
            FontSize = 12
        };
        singleLineTb.Measure(new Size(160, double.PositiveInfinity));
        wrapTb.Measure(new Size(160, double.PositiveInfinity));
        Assert(wrapTb.DesiredSize.Width <= 160.0 + 0.1, $"Wrapped TextBlock DesiredSize.Width expected <= 160, got {wrapTb.DesiredSize.Width}");
        Assert(wrapTb.DesiredSize.Height > singleLineTb.DesiredSize.Height * 1.5,
            $"Wrapped TextBlock DesiredSize.Height ({wrapTb.DesiredSize.Height}) expected > 1.5x single-line height ({singleLineTb.DesiredSize.Height})");

        // Compare with a version missing "ToggleButton" to verify "ToggleButton" is formatted and contributes to wrapped layout
        var withoutToggleTb = new TextBlock
        {
            Text = "ButtonBase: Button, CheckBox, RadioButton",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };
        withoutToggleTb.Measure(new Size(135, double.PositiveInfinity));
        wrapTb.Measure(new Size(135, double.PositiveInfinity));
        Assert(wrapTb.DesiredSize.Height > withoutToggleTb.DesiredSize.Height,
            $"Wrapped TextBlock with ToggleButton ({wrapTb.DesiredSize.Height}) expected taller than without ToggleButton ({withoutToggleTb.DesiredSize.Height}) at width 135");

        var ft = new FormattedText(
            multiWordText,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12.0,
            Brushes.Black,
            1.0);
        AssertEqual(ft.Trimming, TextTrimming.None, "FormattedText default Trimming must be None");
        double unwrappedWidth = ft.Width;
        double singleLineHeight = ft.Height;
        ft.MaxTextWidth = 100.0;
        Assert(unwrappedWidth > 100.0, $"FormattedText unwrapped width ({unwrappedWidth}) expected > 100");
        Assert(ft.Height > singleLineHeight * 1.5, $"FormattedText wrapped height ({ft.Height}) expected > 1.5x single-line height ({singleLineHeight})");

        // 3. CheckBox and RadioButton inside horizontal StackPanel (PositiveInfinity width) do not collapse Grid * column
        var hStack = new StackPanel { Orientation = Orientation.Horizontal };
        var chk = new CheckBox { Content = "Enable Option" };
        var rb = new RadioButton { Content = "Choice Alpha" };
        hStack.Children.Add(chk);
        hStack.Children.Add(rb);
        hStack.Measure(new Size(400, 100));
        hStack.Arrange(new Rect(0, 0, 400, 100));
        Assert(chk.DesiredSize.Width > 40, $"CheckBox DesiredSize.Width in horizontal StackPanel expected > 40, got {chk.DesiredSize.Width}");
        Assert(rb.DesiredSize.Width > 40, $"RadioButton DesiredSize.Width in horizontal StackPanel expected > 40, got {rb.DesiredSize.Width}");
        var chkCp = FindVisualDescendant<ContentPresenter>(chk);
        var rbCp = FindVisualDescendant<ContentPresenter>(rb);
        Assert(chkCp != null && chkCp.ActualWidth > 20, $"CheckBox ContentPresenter.ActualWidth expected > 20, got {chkCp?.ActualWidth}");
        Assert(rbCp != null && rbCp.ActualWidth > 20, $"RadioButton ContentPresenter.ActualWidth expected > 20, got {rbCp?.ActualWidth}");

        // 4. ProgressBar inside a Grid measures/arranges non-zero PART_Indicator width
        var grid = new Grid { Width = 220, Height = 24 };
        var prog = new ProgressBar { Minimum = 0, Maximum = 100, Value = 50, Height = 20 };
        grid.Children.Add(prog);
        grid.Measure(new Size(220, 24));
        grid.Arrange(new Rect(0, 0, 220, 24));
        var progIndicator = FindVisualPart<FrameworkElement>(prog, "PART_Indicator");
        Assert(progIndicator != null && progIndicator.ActualWidth > 50,
            $"ProgressBar inside Grid PART_Indicator.ActualWidth expected > 50, got {progIndicator?.ActualWidth}");

        Pass("CTRL-11. Aero2 Foreground Inheritance, TextBlock Wrapping/Trimming, Horizontal StackPanel CheckBox/RadioButton & Grid ProgressBar");
    }

    private static void TestTextBoxCaretBlinkAndOutsideFocusClear()
    {
        // 1. Caret blinking lifecycle across TextBox, PasswordBox, and RichTextBox
        var tb = new TextBox { Text = "Blink", Width = 140, Height = 28 };
        var pb = new PasswordBox { Password = "Secret", Width = 140, Height = 28 };
        var rtb = new RichTextBox { Width = 140, Height = 50 };

        var rootPanel = new StackPanel();
        var outsideLabel = new TextBlock { Text = "Outside non-focusable label" };
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 25, Width = 140 };
        rootPanel.Children.Add(tb);
        rootPanel.Children.Add(pb);
        rootPanel.Children.Add(rtb);
        rootPanel.Children.Add(outsideLabel);
        rootPanel.Children.Add(slider);

        var win = new Window
        {
            Width = 320,
            Height = 260,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = rootPanel
        };

        try
        {
            win.Show();
            PumpDispatcher();
            var hwndSource = PresentationSource.FromVisual(win) as HwndSource;
            Assert(hwndSource != null, "Shown Window must have HwndSource");

            // TextBox caret blink + reset + stop
            tb.Focus();
            Assert(tb.IsKeyboardFocused, "TextBox must gain keyboard focus");
            Assert(tb.ShouldRenderCaret(false), "TextBox caret must initially be visible on focus");
            tb.CaretBlink.ToggleForTest();
            Assert(!tb.ShouldRenderCaret(false), "TextBox caret must toggle off on blink tick");
            tb.CaretBlink.ToggleForTest();
            Assert(tb.ShouldRenderCaret(false), "TextBox caret must toggle back on");
            tb.CaretBlink.ToggleForTest();
            Assert(!tb.ShouldRenderCaret(false), "TextBox caret off before input reset");
            tb.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, hwndSource!, 0, Key.Right) { RoutedEvent = UIElement.KeyDownEvent });
            Assert(tb.ShouldRenderCaret(false), "KeyDown must reset TextBox caret to visible immediately");

            // PasswordBox caret blink
            pb.Focus();
            Assert(pb.IsKeyboardFocused, "PasswordBox must gain keyboard focus");
            Assert(pb.CaretBlink.IsCaretVisible, "PasswordBox caret must initially be visible on focus");
            pb.CaretBlink.ToggleForTest();
            Assert(!pb.CaretBlink.IsCaretVisible, "PasswordBox caret must toggle off on blink tick");

            // RichTextBox caret blink
            rtb.Focus();
            Assert(rtb.IsKeyboardFocused, "RichTextBox must gain keyboard focus");
            Assert(rtb.ShouldRenderCaret(false), "RichTextBox caret must initially be visible on focus");
            rtb.CaretBlink.ToggleForTest();
            Assert(!rtb.ShouldRenderCaret(false), "RichTextBox caret must toggle off on blink tick");

            // 2. Clicking outside focused TextBox onto non-focusable background clears focus & border
            tb.Focus();
            var tbBorder = FindVisualPart<Border>(tb, "border");
            Assert(tbBorder != null, "TextBox must have 'border'");
            var focusedBrush = tbBorder!.BorderBrush as SolidColorBrush;
            Assert(focusedBrush != null && focusedBrush.Color == Color.FromArgb(0xFF, 0x56, 0x9D, 0xE5),
                $"Focused TextBox BorderBrush expected #FF569DE5, got {focusedBrush?.Color}");

            // Clicking inside TextBox (e.g. right click / unhandled MouseDown with OriginalSource inside tb) keeps focus
            var insideClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
            {
                RoutedEvent = UIElement.MouseDownEvent,
                Source = tb
            };
            tb.RaiseEvent(insideClick);
            Assert(tb.IsKeyboardFocused, "Clicking inside focused TextBox must NOT clear focus");

            // Clicking outside TextBox on non-focusable TextBlock bubbles to Window and clears focus
            var outsideClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseDownEvent,
                Source = outsideLabel
            };
            outsideLabel.RaiseEvent(outsideClick);
            Assert(!tb.IsKeyboardFocused, "Clicking outside TextBox must clear IsKeyboardFocused");
            Assert(!tb.IsFocused, "Clicking outside TextBox must clear IsFocused");
            Assert(!tb.ShouldRenderCaret(false), "TextBox caret must hide after losing focus");
            var unfocusedBrush = tbBorder.BorderBrush as SolidColorBrush;
            Assert(unfocusedBrush == null || unfocusedBrush.Color != Color.FromArgb(0xFF, 0x56, 0x9D, 0xE5),
                $"Unfocused TextBox BorderBrush must revert from #FF569DE5, got {unfocusedBrush?.Color}");

            // 3. Clicking a templated Slider moves focus off TextBox onto Slider
            tb.Focus();
            Assert(tb.IsKeyboardFocused, "TextBox re-focused before Slider click");
            var sliderClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                Source = slider
            };
            slider.RaiseEvent(sliderClick);
            Assert(slider.IsKeyboardFocused, "Clicking Slider must focus Slider");
            Assert(!tb.IsKeyboardFocused, "Clicking Slider must blur TextBox");
        }
        finally
        {
            win.Close();
        }

        // 4. Verify XmlnsDefinition / XmlnsPrefix metadata on CSUI.Core assembly for XAML IntelliSense
        var xmlnsDefs = typeof(Window).Assembly.GetCustomAttributes(typeof(System.Windows.Markup.XmlnsDefinitionAttribute), false);
        bool hasControlsMapping = false;
        bool hasWindowsMapping = false;
        foreach (System.Windows.Markup.XmlnsDefinitionAttribute def in xmlnsDefs)
        {
            if (def.XmlNamespace == "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            {
                if (def.ClrNamespace == "System.Windows.Controls") hasControlsMapping = true;
                if (def.ClrNamespace == "System.Windows") hasWindowsMapping = true;
            }
        }
        Assert(hasControlsMapping && hasWindowsMapping,
            "CSUI.Core must expose XmlnsDefinition mappings for http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        Pass("CTRL-12. TextBox/PasswordBox/RichTextBox Caret Blink & Outside Click Focus Clear");
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
