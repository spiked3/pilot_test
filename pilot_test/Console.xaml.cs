using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace spiked3
{
    // default color foreground
    // + will be bright green
    // warn is yellow
    // error is red
    // level 1-4 detail level of debugging messages, 4 being lowest

    public partial class Console : UserControl
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion INotifyPropertyChanged

        private static Console _thisInstance;

        static public int MessageLevel
        {
            get { return (int)_thisInstance.GetValue(MessageLevelProperty); }
            set { _thisInstance.SetValue(MessageLevelProperty, value); }
        }

        public static readonly DependencyProperty MessageLevelProperty =
            DependencyProperty.Register("MessageLevel", typeof(int), typeof(Console), new PropertyMetadata(1));

        //public int MessageLevel { get { return _thisInstance._MessageLevel; } set { _thisInstance._MessageLevel = value; _thisInstance.OnPropertyChanged(); } }
        //int _MessageLevel = 1;

        public Console()
        {
            _thisInstance = this;
            InitializeComponent();
            new TraceDecorator(consoleListBox);
        }

        void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MessageLevel = (int)e.NewValue;
        }

        void Clear_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        public void Clear()
        {
            consoleListBox.Items.Clear();
        }

        public void Test()
        {
            Trace.WriteLine("Test_Click");
            Trace.WriteLine("test +", "+");
            Trace.WriteLine("test warn", "warn");
            Trace.WriteLine("test error", "error");
            Trace.WriteLine("test 1", "1");
            Trace.WriteLine("test 2", "2");
            Trace.WriteLine("test 3", "3");
            Trace.WriteLine("test 4", "4");
            Trace.WriteLine("test 5", "5");
        }

        private class TraceDecorator : TraceListener
        {
            private ListBox ListBox;

            public TraceDecorator(ListBox listBox)
            {
                ListBox = listBox;
                System.Diagnostics.Trace.Listeners.Add(this);
            }

            public override void WriteLine(string message, string category)
            {
                if (ListBox == null)
                    return;

                ListBox.Dispatcher.Invoke(() =>
                {
                    int CatagoryLevel;

                    if (int.TryParse(category, out CatagoryLevel))  //if a numeric was specified
                        if (CatagoryLevel > Console.MessageLevel)
                            return;

                    // +++ add timestamp and level to msg like 12:22.78 Warning: xyz is being bad
                    TextBlock t = new TextBlock();
                    t.Text = message;
                    t.Foreground = category.Equals("error") ? Brushes.Red :
                        category.Equals("warn") ? Brushes.Yellow :
                        category.Equals("+") ? Brushes.LightGreen :
                        CatagoryLevel > 0 ? Brushes.Cyan :
                        ListBox.Foreground;

                    int i = ListBox.Items.Add(t);
                    if (ListBox.Items.Count > 1024)
                        ListBox.Items.RemoveAt(0);  // expensive I bet :(
                    var sv = ListBox.TryFindParent<ScrollViewer>();
                    if (sv != null)
                        sv.ScrollToBottom();  //  +++  not doing it
                });
            }

            public override void WriteLine(string message)
            {
                WriteLine(message, "");
            }

            public override void Write(string message)
            {
                throw new NotImplementedException();
            }
        }
    }
}