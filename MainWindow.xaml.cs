using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace 字幕翻译助手
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 控制器
        /* 控制左侧文本 */
        /// <summary>
        /// 左侧文字的显示和修改。用来代替textBox左.Text（当用户主动修改textBox左.Text时请重新调用该Set）。
        /// </summary>
        private String OriginalTexts { get { return originalTexts; } set { originalTexts = value; this.textBox左.Text = value; } }
        private String[] OriginalLines { get { return Regex.Split(OriginalTexts, @"\r\n"); }
            set
            {
                StringBuilder @string = new StringBuilder();
                foreach (var str in value)
                    @string.AppendLine(str);
                OriginalTexts = @string.ToString();
            }
        }
        String originalTexts;

        /* 控制右侧文本 */
        /// <summary>
        /// 右侧文字的显示和修改。用来代替textBox右.Text（当用户主动修改textBox右.Text时请重新调用该Set）
        /// </summary>
        private String TranslatedTexts { get { return translatedTexts; } set { translatedTexts = value; this.textBox右.Text = value; } }
        private String[] TranslatedLines { get { return Regex.Split(TranslatedTexts, @"\r\n"); }
            set
            {
                StringBuilder @string = new StringBuilder();
                foreach (var str in value)
                    @string.AppendLine(str);
                TranslatedTexts = @string.ToString();
            }
        }
        String translatedTexts;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private string ReadAllText(string filename)
        {
            StringBuilder fullText = new StringBuilder();
            using (FileStream file = new FileStream(filename, FileMode.Open))
            {
                StreamReader reader = new StreamReader(file);
                string line;
                while ((line = reader.ReadLine()) != null)
                    fullText.AppendLine(line);
            }
            return fullText.ToString().TrimEnd(new char[] { '\n', '\r' });
        }

        private void TextBox文件名_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void TextBox文件名_DragDrop(object sender, DragEventArgs e)
        {
            this.textBox文件名.Text = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            OriginalTexts = ReadAllText(this.textBox文件名.Text);
            选择的文件 = new FileInfo(this.textBox文件名.Text);
        }

        private FileInfo 选择的文件;
        private void Button选择文件_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "视频字幕文件|*.srt|所有文件|*.*";
            if (选择的文件 != null)
            {
                openFileDialog.InitialDirectory = 选择的文件.DirectoryName;
                openFileDialog.FileName = 选择的文件.Name;
            }
            if (openFileDialog.ShowDialog() == false) 
                return;
            this.textBox文件名.Text = openFileDialog.FileName;
            选择的文件 = new FileInfo(openFileDialog.FileName);
            OriginalTexts = ReadAllText(openFileDialog.FileName);
        }

        private void Button右_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder rightTexts = new StringBuilder();
            var originalLines = OriginalLines;
            // 同步内容
            if (OriginalTexts != this.textBox左.Text)
            {
                if (MessageBoxResult.No == MessageBox.Show("检测到左边已被修改，是否继续本操作？", "修改确认", MessageBoxButton.YesNo))
                    return;
                OriginalTexts = this.textBox左.Text;
            }
            for (int lineIndex = 0; lineIndex < originalLines.Count(); lineIndex++)
            {
                var lineContent = originalLines[lineIndex];
                if (lineContent.Contains(" --> ") || lineContent == "" || Regex.IsMatch(lineContent, @"^\d+$")) 
                    continue;
                rightTexts.AppendLine(originalLines[lineIndex]);
            }
            TranslatedTexts = rightTexts.ToString().Trim();
        }

        private void Button复制右边_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TranslatedTexts);
        }

        private void Button粘贴_Click(object sender, RoutedEventArgs e)
        {
            TranslatedTexts = Clipboard.GetText();
        }

        private void Button左_Click(object sender, RoutedEventArgs e)
        {
            // 减少控制器访问次数，失败时便于回滚
            var returnedLines = OriginalLines;
            var translatedLines = TranslatedLines;
            // 同步内容
            if (TranslatedTexts != this.textBox右.Text)
            {
                if (MessageBoxResult.No == MessageBox.Show("检测到右边已被修改，是否继续本操作？", "修改确认", MessageBoxButton.YesNo))
                    return;
                TranslatedTexts = this.textBox右.Text;
            }

            int leftLineIndex = 0, rightLineIndex = 0;
            for (; leftLineIndex < returnedLines.Count(); leftLineIndex++)
            {
                string leftLine = returnedLines[leftLineIndex];
                if (leftLine.Contains(" --> ") || leftLine == "" || Regex.IsMatch(leftLine, @"^\d+$"))
                    continue;
                try
                {
                    returnedLines[leftLineIndex] = translatedLines[rightLineIndex++];
                }
                catch (IndexOutOfRangeException)
                {
                    MessageBox.Show("检测到右方行数与左边对应行数不一致，请重新检查译文的正确性！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            if (leftLineIndex < OriginalLines.Count() || rightLineIndex < translatedLines.Length) 
                MessageBox.Show("检测到右方行数与左边对应行数不一致，请重新检查译文的正确性！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                OriginalLines = returnedLines;
        }
        
        private void Button存储左边_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "视频字幕文件|*.srt|所有文件|*.*";
            if (选择的文件 != null)
            {
                saveFileDialog.InitialDirectory = 选择的文件.DirectoryName;
                saveFileDialog.FileName = 选择的文件.Name;
            }
            if (saveFileDialog.ShowDialog() == false)
                return;
            File.WriteAllText(saveFileDialog.FileName, OriginalTexts);
        }
    }
}
