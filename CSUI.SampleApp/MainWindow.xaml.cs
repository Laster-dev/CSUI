using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CSUI.SampleApp
{
    public partial class MainWindow : Window
    {
        private int _counter;

        public MainWindow()
        {
            InitializeComponent();
            WireInteractions();
        }

        private void WireInteractions()
        {
            // 1. Button interactions
            if (BtnCounter != null)
            {
                BtnCounter.Click += (s, e) =>
                {
                    _counter++;
                    BtnCounter.Content = $"点击计数按钮 ({_counter})";
                    Log($"[Button.Click] 计数器递增为: {_counter}");
                };
            }

            if (BtnReset != null)
            {
                BtnReset.Click += (s, e) =>
                {
                    _counter = 0;
                    if (BtnCounter != null)
                    {
                        BtnCounter.Content = "点击计数按钮 (0)";
                    }
                    Log("[Button.Click] 计数器已重置为 0");
                };
            }

            // 2. ToggleButton interaction
            if (ToggleCycle != null)
            {
                ToggleCycle.Checked += (s, e) => Log("[ToggleButton.Checked] 状态已流转为: Checked (true)");
                ToggleCycle.Unchecked += (s, e) => Log("[ToggleButton.Unchecked] 状态已流转为: Unchecked (false)");
                ToggleCycle.Indeterminate += (s, e) => Log("[ToggleButton.Indeterminate] 状态已流转为: Indeterminate (null)");
            }

            // 3. CheckBox interactions
            if (ChkFeatureA != null)
            {
                ChkFeatureA.Checked += (s, e) => Log("[CheckBox] 硬件光线追踪: 开启");
                ChkFeatureA.Unchecked += (s, e) => Log("[CheckBox] 硬件光线追踪: 关闭");
            }

            if (ChkFeatureB != null)
            {
                ChkFeatureB.Checked += (s, e) => Log("[CheckBox] 独立合成器线程: 开启");
                ChkFeatureB.Unchecked += (s, e) => Log("[CheckBox] 独立合成器线程: 关闭");
            }

            if (ChkFeatureC != null)
            {
                ChkFeatureC.Checked += (s, e) => Log("[CheckBox] 高精度手势预测: 开启 (Checked)");
                ChkFeatureC.Unchecked += (s, e) => Log("[CheckBox] 高精度手势预测: 关闭 (Unchecked)");
                ChkFeatureC.Indeterminate += (s, e) => Log("[CheckBox] 高精度手势预测: 系统动态推断 (Indeterminate)");
            }

            // 4. RadioButton group interactions
            if (RbThemeDark != null)
            {
                RbThemeDark.Checked += (s, e) => Log("[RadioButton.Group] 主题单选切换为: 深色模式 (Dark)");
            }
            if (RbThemeLight != null)
            {
                RbThemeLight.Checked += (s, e) => Log("[RadioButton.Group] 主题单选切换为: 浅色模式 (Light)");
            }
            if (RbThemeMica != null)
            {
                RbThemeMica.Checked += (s, e) => Log("[RadioButton.Group] 主题单选切换为: 原生云母材质 (Mica Alt)");
            }

            // 5. Slider & ProgressBar sync
            if (DemoSlider != null)
            {
                DemoSlider.ValueChanged += (s, e) =>
                {
                    if (DemoProgressBar != null)
                    {
                        DemoProgressBar.Value = e.NewValue;
                    }
                    if (TxtSliderVal != null)
                    {
                        TxtSliderVal.Text = $"{e.NewValue:F0}%";
                    }
                    Log($"[Slider.ValueChanged] 步进数值: {e.NewValue:F1} (已自动同步到 ProgressBar)");
                };
            }

            // 6. TextBox interactions
            if (TxtSampleInput != null)
            {
                TxtSampleInput.TextChanged += (s, e) =>
                {
                    string currentText = TxtSampleInput.Text ?? string.Empty;
                    if (TxtClusterCount != null)
                    {
                        TxtClusterCount.Text = $"当前字符长度: {currentText.Length}";
                    }
                    Log($"[TextBox.TextChanged] 文本更新: \"{currentText}\"");
                };
            }

            // 7. PasswordBox interactions
            if (PwdSampleInput != null)
            {
                PwdSampleInput.PasswordChanged += (s, e) =>
                {
                    int length = PwdSampleInput.Password?.Length ?? 0;
                    if (TxtPwdMask != null)
                    {
                        TxtPwdMask.Text = $"安全密码长度: {length} 位";
                    }
                    Log($"[PasswordBox.PasswordChanged] 密码更新 (已掩码隐藏，长度={length})");
                };
            }
        }

        private void Log(string message)
        {
            if (TxtEventLog != null)
            {
                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                TxtEventLog.Text = $"[{time}] {message}";
            }
        }
    }
}
