using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace YL_Player
{
    /// <summary>
    /// Логика взаимодействия для About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            Version.Content = "v" + versionInfo.ProductVersion; //версия текущего проекта
            Author.Content = versionInfo.CompanyName; //название компании или ФИО автора задается в настройках проекта
            Copyright.Content += (DateTime.Now.Year > 2019) ? DateTime.Now.Year.ToString() : "2019";
        }

        private void Author_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://yinglongx.github.io/");
        }
    }
}
