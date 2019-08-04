using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            Version.Content = "v." + versionInfo.ProductVersion; //версия текущего проекта
            Author.Content = versionInfo.CompanyName; //название компании или ФИО автора задается в настройках проекта
        }
    }
}
