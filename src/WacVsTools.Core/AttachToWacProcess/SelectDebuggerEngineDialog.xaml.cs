namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Windows;

    /// <summary>
    /// Interaction logic for SelectDebuggerEngine.xaml
    /// </summary>
    public partial class SelectDebuggerEngineDialog : Window
    {
        ISelectDebuggerEngineDialogModel model;

        public SelectDebuggerEngineDialog(ISelectDebuggerEngineDialogModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));

            DataContext = this.model;

            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
