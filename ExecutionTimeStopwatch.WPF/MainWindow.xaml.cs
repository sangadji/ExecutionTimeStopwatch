using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ExecutionTimeStopwatch.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Stopwatch stopwatch = new Stopwatch();
        DispatcherTimer dt = new DispatcherTimer();
        CancellationTokenSource cts;
        CancellationToken token;
        string currentTime = string.Empty;
        long max, current;

        const int HEAVY_TASK_WORKLOAD = 1000000;
        const int MEDIUM_TASK_WORKLOAD = 1000;
        const int EASY_TASK_WORKLOAD = 10;

        enum ExecutionType
        {
            Synchronous,
            Asynchronous
        }

        Task DoHeavyWork(long iteration, ExecutionType executionType)
        {
            max = iteration;

            return Task.Factory.StartNew(() =>
            {
                if (executionType == ExecutionType.Synchronous)
                {
                    for (long i = 0; i < iteration; i++)
                    {
                        for (long j = 0; j < HEAVY_TASK_WORKLOAD; j++) { }
                        ++this.current;
                        if (token.IsCancellationRequested) break;
                    }
                }
                else
                {
                    Parallel.For(0, iteration, ((index, state) =>
                    {
                        for (long j = 0; j < HEAVY_TASK_WORKLOAD; j++) { }
                        Interlocked.Increment(ref current);
                        if (token.IsCancellationRequested) state.Stop();
                    }));
                }
            }, token);
        }

        Task DoMediumWork(long iteration, ExecutionType executionType)
        {
            max = iteration;

            return Task.Factory.StartNew(() =>
            {
                if (executionType == ExecutionType.Synchronous)
                {
                    for (long i = 0; i < iteration; i++)
                    {
                        for (long j = 0; j < MEDIUM_TASK_WORKLOAD; j++) { }
                        ++this.current;
                        if (token.IsCancellationRequested) break;
                    }
                }
                else
                {
                    Parallel.For(0, iteration, ((index, state) =>
                    {
                        for (long j = 0; j < MEDIUM_TASK_WORKLOAD; j++) { }
                        Interlocked.Increment(ref current);
                        if (token.IsCancellationRequested) state.Stop();
                    }));
                }
            }, token);
        }

        Task DoEasyWork(long iteration, ExecutionType executionType)
        {
            max = iteration;

            return Task.Factory.StartNew(() =>
            {
                if (executionType == ExecutionType.Synchronous)
                {
                    for (long i = 0; i < iteration; i++)
                    {
                        for (long j = 0; j < EASY_TASK_WORKLOAD; j++) { }
                        ++this.current;
                        if (token.IsCancellationRequested) break;
                    }
                }
                else
                {
                    var loopResult = Parallel.For(0, iteration, ((index, state) =>
                    {
                        for (long j = 0; j < EASY_TASK_WORKLOAD; j++) { }
                        Interlocked.Increment(ref current);
                        if (token.IsCancellationRequested) state.Stop();
                    }));
                }
            }, token);
        }

        public MainWindow()
        {
            InitializeComponent();
            dt.Tick += Dt_Tick;
            dt.Interval = new TimeSpan(0, 0, 0, 0, 10);
        }

        void StartStopwatch()
        {
            stopwatch.Reset();
            stopwatch.Start();
            dt.Start();
            ButtonStop.IsEnabled = true;
            ButtonStart.IsEnabled = false;
        }

        void StopStopwatch()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();
                ButtonStop.IsEnabled = false;
                ButtonStart.IsEnabled = true;
            });
        }

        #region Event handlers
        private void Dt_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = stopwatch.Elapsed;
            currentTime = String.Format("{0:00}:{1:00}:{2:00}:{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);

            this.Dispatcher.Invoke(() =>
            {
                TextBlockStopwatch.Text = currentTime;
                ProgressBarMain.Minimum = 0;
                ProgressBarMain.Maximum = max;
                ProgressBarMain.Value = this.current;
                LabelProgressBar.Content = this.current + "/" + max;
            });
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            StartStopwatch();

            cts = new CancellationTokenSource();
            token = cts.Token;
            var iteration = Convert.ToInt64(TextBoxIteration.Text);
            var selectedIndex = ComboBoxWorkload.SelectedIndex;
            var executionType = ComboBoxExecutionType.SelectedIndex == 0 ? ExecutionType.Synchronous : ExecutionType.Asynchronous;

            var task = Task.Factory.StartNew(async () =>
            {
                Task toDo = null;
                this.current = 0;

                if (selectedIndex == 0)
                    toDo = DoHeavyWork(iteration, executionType);
                else
                if (selectedIndex == 1)
                    toDo = DoMediumWork(iteration, executionType);
                else
                if (selectedIndex == 2)
                    toDo = DoEasyWork(iteration, executionType);

                await toDo.ContinueWith((a) => StopStopwatch());
            }, token);
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            StopStopwatch();
            cts.Cancel();
        }
        #endregion
    }
}
