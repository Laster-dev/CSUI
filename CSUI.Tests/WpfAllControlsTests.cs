using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
}
