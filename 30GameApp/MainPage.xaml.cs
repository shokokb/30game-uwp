using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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
        /// ビューモデル
        /// </summary>
        ViewModel _vm;
        
        /// <summary>
        /// 勝ち負けが決まる数字
        /// </summary>
        private const int COUNT_JOKER = 30;

        /// <summary>
        /// カウント回数(最大回数)
        /// </summary>
        private const int COUNT_LIMIT = 3;

        /// <summary>
        /// ボタンを押せない時の文字列
        /// </summary>
        const String NO_PUSH = "XX";

        /// <summary>
        /// 読上間隔[msec]
        /// </summary>
        private const int READ_INTERVAL = 1000;

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

            // イベントを登録する
            _btnStart.Tapped += _btnStart_Tapped;
            _btnPushLeft.Tapped += _btnPushNumber_Tapped;
            _btnPushCenter.Tapped += _btnPushNumber_Tapped;
            _btnPushRight.Tapped += _btnPushNumber_Tapped;
            _btnNext.Tapped += _btnNext_Tapped;
            _btnHelp.Tapped += _btnHelp_Tapped;
            _btnReplay.Tapped += _btnReplay_Tapped;
            _btnQuit.Tapped += _btnQuit_Tapped;

            // スタート画面を表示する
            SwitchBaseGrid(e_BaseGrid.Start);

            // アセンブリ情報のバージョンを取得し、バージョンを画面に表示する
            var versionInfo = Windows.ApplicationModel.Package.Current.Id.Version;
            _tbcVersion.Text = String.Format(
                               "Ver. {0}.{1}.{2}.{3}",
                               versionInfo.Major, versionInfo.Minor,
                               versionInfo.Build, versionInfo.Revision);

            // ビューモデルを設定する
            _vm = new ViewModel();
            DataContext = _vm;
        }

        /// <summary>
        /// ページ遷移時
        /// </summary>
        /// <param name="e">イベント変数</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // 表示を英語に切り替える
            ApplicationLanguages.PrimaryLanguageOverride = "en";
        }

        /// <summary>
        /// ゲストが「かいし」ボタンを押したとき、ゲームを開始する
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>
        private async void _btnStart_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ReadLoud(_btnStart.Content.ToString());
            await StartGame();
        }

        /// <summary>
        /// ゲーム開始
        /// </summary>
        private async Task StartGame ()
        {
            _vm.Number = "";
            SwitchBaseGrid(e_BaseGrid.Game);            // ゲーム画面に切り替える
            await RobotCount();                         // ボットが数える
        }

        /// <summary>
        /// <br/>ゲストが数字のボタンを押下した場合に、
        /// <br/>現在の数字をカウントアップし、
        /// <br/>ゲスト用のコントロールの表示を切り替える
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>
        private async void _btnPushNumber_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Button btn = (Button)sender;

            // 現在の数字の表示を更新する
            var newNumber = btn.Content.ToString();
            _vm.Number = newNumber;
            await ReadLoud(newNumber);

            if (_tbcCurrentNumber.Text == COUNT_JOKER.ToString())
            {
                // ゲストが負けた場合
                SwitchBaseGrid(e_BaseGrid.Result);
                var resourceLoader = ResourceLoader.GetForCurrentView();
                var message = resourceLoader.GetString("MessageLose");
                _vm.Result = message;
                await ReadLoud(message);
            }
            else
            {
                if (btn.Name == "_btnPushLeft")
                {
                    SwitchGuestControl(e_GameStatus.GuestCountOne);
                }
                else if (btn.Name == "_btnPushCenter")
                {
                    SwitchGuestControl(e_GameStatus.GuestCountTwo);
                }
                else
                {
                    SwitchGuestControl(e_GameStatus.GuestCountThree);
                    await GoNext();
                }
            }
        }

        /// <summary>
        /// 読み上げる
        /// </summary>
        /// <param name="i_content">読み上げる内容</param>
        /// <returns>タスク</returns>
        private async Task ReadLoud(string i_content)
        {
            SpeechSynthesisStream myStream;
            using (var mySynth = new SpeechSynthesizer())
            {
                myStream = await mySynth.SynthesizeTextToStreamAsync(i_content.ToString());
            }
            _mediaElement.Volume = _sldVolume.Value / 100;
            _mediaElement.SetSource(myStream, myStream.ContentType);
            _mediaElement.Play();
            await Task.Delay(READ_INTERVAL);
        }

        /// <summary>
        /// ゲストが「つぎ」を押したときに、ボットが数え始める
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>
        private async void _btnNext_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await GoNext();
        }
        
        /// <summary>
        /// 「つぎ」を呼び出し、ボットのカウントに切り替える
        /// </summary>
        private async Task GoNext()
        {
            // 「つぎ」を読み上げる
            var resourceLoader = ResourceLoader.GetForCurrentView();
            await ReadLoud(resourceLoader.GetString("MessageNext"));

            // ボットによるカウントを開始する
            await RobotCount();
        }

        /// <summary>
        /// ボットが一定間隔で数字を数える
        /// </summary>
        private async Task RobotCount()
        {
            // ゲスト押下用のコントロールを切り替える
            SwitchGuestControl(e_GameStatus.RobotCount);

            // ボットのカウント数(ボットがどこまでカウントしたか(0, 1, 2, 3))を初期化する
            int robotCount = 0;

            // ボットがカウントする回数(1, 2, 3)を決定する
            int robotCountLimit = (new Random()).Next(COUNT_LIMIT) + 1;

            // ボットが数をカウントし始める
            for (robotCount = 1; robotCount <= robotCountLimit; robotCount++)
            {
                // ボットのカウント回数を更新し、
                // カウント回数をオーバーしていた場合はカウントを停止する。
                // 現在の数字の表示を更新する
                var newNumber = (CurrentNumber + 1).ToString();
                _vm.Number = newNumber;
                await ReadLoud(newNumber);


                if (_tbcCurrentNumber.Text == (COUNT_JOKER - 1).ToString())
                {
                    // 29の場合はここで止める
                    break;
                }
                else if (_tbcCurrentNumber.Text == COUNT_JOKER.ToString())
                {
                    // ゲストが勝った場合
                    SwitchBaseGrid(e_BaseGrid.Result);
                    var resourceLoader = ResourceLoader.GetForCurrentView();
                    var message = resourceLoader.GetString("MessageWin");
                    _vm.Result = message;
                    await ReadLoud(message);
                }
            }

            // ゲストの番となる
            GuestCount();
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
        /// ゲストが「ヘルプ」を押したときに、使い方を説明する
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>

        private async void _btnHelp_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var message = resourceLoader.GetString("Explanation");
            MessageDialog dialog = new MessageDialog(message);
            await ReadLoud(message);
            await dialog.ShowAsync();
        }

        /// <summary>
        /// ゲストが「もういちど」を押したときに、ゲームを再開する
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>
        private async void _btnReplay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ReadLoud(_btnReplay.Content.ToString());
            await StartGame();
        }

        /// <summary>
        /// ゲストが「おわり」を押したときに、アプリケーションを終了する
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベント変数</param>

        private void _btnQuit_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}
