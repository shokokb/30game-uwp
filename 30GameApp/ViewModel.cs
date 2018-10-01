using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace _30GameApp
{
    /// <summary>
    /// モデルビュークラス
    /// </summary>
    /// <remarks>
    /// データバインディング用
    /// </remarks>
    public class ViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// プロパティ変更が発生した時のハンドラ
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// プロパティ変更が検出された時のイベント関数
        /// </summary>
        /// <param name="i_propertyName">変更されたプロパティ名</param>
        private void NotifyPropertyChanged([CallerMemberName] String i_propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(i_propertyName));
            }
        }

        /// <summary>
        /// 数字
        /// </summary>
        private String _number = "";

        private String _result = "";

        /// <summary>
        /// 数字
        /// </summary>
        public String Number
        {
            get { return this._number; }
            set
            {
                if (value != this._number)
                {
                    this._number = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public String Result
        {
            get { return _result; }
            set
            {
                if (value != this._result)
                {
                    this._result = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
