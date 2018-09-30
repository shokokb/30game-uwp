using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace _30GameApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// ボットカウント用のカウンタ
        /// </summary>
        private DispatcherTimer _timer;

        /// <summary>
        /// ボットがどこまでカウントしたか(0, 1, 2, 3)
        /// </summary>
        private int _robotCount = 0;

        /// <summary>
        /// ボットがカウントする回数(1, 2, 3)
        /// </summary>
        private int _robotCountLimit = 0;

        private ResourceLoader _resourceLoader = null;

        /// <summary>
        /// カウント回数(最大回数)
        /// </summary>
        private const int COUNT_LIMIT = 3;

        /// <summary>
        /// ボットカウント間隔[sec]
        /// </summary>
        private const int COUNT_TIMER_INTERVAL = 1;

        /// <summary>
        /// グリッド種別
        /// </summary>
        private enum e_BaseGrid
        {
            /// <summary>
            /// ゲーム開始前
            /// </summary>
            Start,
            /// <summary>
            /// ゲーム中
            /// </summary>
            Game,
            /// <summary>
            /// ゲーム結果表示
            /// </summary>
            Result
        }

        /// <summary>
        /// ゲームでのカウントの状態
        /// </summary>
        private enum e_GameStatus
        {
            /// <summary>
            /// ボットがカウント中の状態
            /// </summary>
            RobotCount,
            /// <summary>
            /// ゲストがまだカウントしていない状態
            /// </summary>
            GuestCountNone,
            /// <summary>
            /// ゲストが1つカウントした状態
            /// </summary>
            GuestCountOne,
            /// <summary>
            /// ゲストが2つカウントした状態
            /// </summary>
            GuestCountTwo,
            /// <summary>
            /// ゲストが3カウントした状態
            /// </summary>
            GuestCountThree
        }

        /// <summary>
        /// 現在の数字
        /// </summary>
        /// <returns>現在の数字</returns>
        private int CurrentNumber
        {
            get
            {
                int ret = 0;
                int.TryParse(_tbcCurrentNumber.Text, out ret);
                return ret;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // 全画面で表示する
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

            _btnStart.Click += _btnStart_Click;
            _btnStart.Tapped += _btnStart_Click;
            _btnPushLeft.Click += _btnPushNumber_Click;
            _btnPushLeft.Tapped += _btnPushNumber_Click;
            _btnPushCenter.Click += _btnPushNumber_Click;
            _btnPushCenter.Tapped += _btnPushNumber_Click;
            _btnPushRight.Click += _btnPushNumber_Click;
            _btnPushRight.Click += _btnPushNumber_Click;
            _btnNext.Click += _btnNext_Click;

            // スタート画面を表示する
            SwitchBaseGrid(e_BaseGrid.Start);
        }

        /// <summary>
        /// ページ遷移時
        /// </summary>
        /// <param name="e">イベント変数</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _timer = new DispatcherTimer();
            _resourceLoader = ResourceLoader.GetForCurrentView();
        }

        /// <summary>
        /// ゲーム開始
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>
        private void _btnStart_Click(object sender, RoutedEventArgs e)
        {
            // ゲーム画面に切り替える
            SwitchBaseGrid(e_BaseGrid.Game);

            // ボットが数える
            RobotCount();
        }

        /// <summary>
        /// <br/>ゲストが数字のボタンを押下した場合に、
        /// <br/>現在の数字をカウントアップし、
        /// <br/>ゲスト用のコントロールの表示を切り替える
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>
        private void _btnPushNumber_Click(object sender, RoutedEventArgs e)
        {
            // 現在の数字の表示を更新する
            _tbcCurrentNumber.Text = (CurrentNumber + 1).ToString();

            String name = ((Button)sender).Name;
            if (name == "_btnPushLeft") SwitchGuestControl(e_GameStatus.GuestCountOne);
            else if (name == "_btnPushCenter") SwitchGuestControl(e_GameStatus.GuestCountTwo);
            else SwitchGuestControl(e_GameStatus.GuestCountThree);
        }

        /// <summary>
        /// ゲストが「つぎ」を押したときに、ボットが数え始める
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>
        private void _btnNext_Click(object sender, RoutedEventArgs e)
        {
            RobotCount();
        }

        /// <summary>
        /// ボットが一定間隔で数字を数える
        /// </summary>
        private void RobotCount()
        {
            // ゲスト押下用のコントロールを切り替える
            SwitchGuestControl(e_GameStatus.RobotCount);

            // ボットが何回カウントするか決定する
            Random rnd = new Random();
            _robotCountLimit = rnd.Next(COUNT_LIMIT) + 1;

            // ボットが数をカウントし始める
            _timer.Interval = TimeSpan.FromSeconds(COUNT_TIMER_INTERVAL);
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }

        /// <summary>
        /// ゲストが数字をカウントする
        /// </summary>
        private void GuestCount()
        {
            // ゲスト押下用のコントロールを切り替える
            SwitchGuestControl(e_GameStatus.GuestCountNone);
        }

        /// <summary>
        /// ゲスト押下用のコントロールを切り替える
        /// </summary>
        /// <param name="i_status">ゲームでのカウントの状態</param>
        private void SwitchGuestControl(e_GameStatus i_status)
        {
            const String NO_PUSH = "XX";
            switch (i_status)
            {
                case e_GameStatus.RobotCount:
                    _btnPushLeft.Content = NO_PUSH;
                    _btnPushCenter.Content = NO_PUSH;
                    _btnPushRight.Content = NO_PUSH;
                    _btnPushLeft.IsEnabled = false;
                    _btnPushCenter.IsEnabled = false;
                    _btnPushRight.IsEnabled = false;
                    _btnNext.IsEnabled = false;
                    break;
                case e_GameStatus.GuestCountNone:
                    _btnPushLeft.Content = (CurrentNumber + 1).ToString();
                    _btnPushCenter.Content = (CurrentNumber + 2).ToString();
                    _btnPushRight.Content = (CurrentNumber + 3).ToString();
                    _btnPushLeft.IsEnabled = true;
                    _btnPushCenter.IsEnabled = false;
                    _btnPushRight.IsEnabled = false;
                    _btnPushLeft.Focus(FocusState.Pointer);
                    _btnNext.IsEnabled = false;
                    break;
                case e_GameStatus.GuestCountOne:
                    _btnPushLeft.IsEnabled = false;
                    _btnPushCenter.IsEnabled = true;
                    _btnPushRight.IsEnabled = false;
                    _btnPushCenter.Focus(FocusState.Pointer);
                    _btnNext.IsEnabled = true;
                    break;
                case e_GameStatus.GuestCountTwo:
                    _btnPushLeft.IsEnabled = false;
                    _btnPushCenter.IsEnabled = false;
                    _btnPushRight.IsEnabled = true;
                    _btnPushRight.Focus(FocusState.Pointer);
                    _btnNext.IsEnabled = true;
                    break;
                case e_GameStatus.GuestCountThree:
                    _btnPushLeft.IsEnabled = false;
                    _btnPushCenter.IsEnabled = false;
                    _btnPushRight.IsEnabled = false;
                    _btnNext.Focus(FocusState.Pointer);
                    _btnNext.IsEnabled = true;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 表示切替
        /// </summary>
        /// <param name="i_grid">グリッド種別</param>
        private void SwitchBaseGrid(e_BaseGrid i_grid = e_BaseGrid.Start)
        {
            _grdStart.Visibility = (i_grid == e_BaseGrid.Start ? Visibility.Visible : Visibility.Collapsed);
            _grdGame.Visibility = (i_grid == e_BaseGrid.Game ? Visibility.Visible : Visibility.Collapsed);
            _grdResult.Visibility = (i_grid == e_BaseGrid.Result ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// ボットがカウントする際に発生するタイマイベント
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント変数</param>
        private void _timer_Tick(object sender, object e)
        {
            // ボットのカウント回数を更新し、
            // カウント回数をオーバーしていた場合はカウントを停止する。
            _robotCount += 1;
            if (_robotCount >= _robotCountLimit)
            {
                _timer.Stop();
                _robotCount = 0;

                // ゲストの番となる
                GuestCount();
            }
            else
            {
                // 現在の数字の表示を更新する
                _tbcCurrentNumber.Text = (CurrentNumber + 1).ToString();
            }
        }
    }
}
