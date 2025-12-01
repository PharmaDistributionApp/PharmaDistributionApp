using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PharmaDistributionApp.Views
{
    /// <summary>
    /// Interaction logic for VerifyCodeWindow.xaml
    /// </summary>
    public partial class VerifyCodeWindow : Window
    {
        private string _systemCode; 
        private string _userEmail;
        public VerifyCodeWindow(string code, string email)
        {
            InitializeComponent();
            _systemCode = code; 
            _userEmail = email;
        }

        private void TxtCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox currentBox = sender as TextBox;

            if (currentBox.Text.Length == 1)
            {
                var request = new TraversalRequest(FocusNavigationDirection.Next);
                currentBox.MoveFocus(request);
            }

        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string inputCode = txtC1.Text + txtC2.Text + txtC3.Text + txtC4.Text + txtC5.Text + txtC6.Text;

            if (inputCode == _systemCode)
            {
                ResetPasswordWindow resetWindow = new ResetPasswordWindow(_userEmail); 
                resetWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Mã xác minh không chính xác!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
