using System;
using System.IO.Ports;
using System.Windows;

namespace TicTacToeWPF
{
    public partial class ComPortSelectionWindow : Window
    {
        public string SelectedPort { get; private set; }

        public ComPortSelectionWindow()
        {
            InitializeComponent();
            PopulateComPortComboBox();
        }

        private void PopulateComPortComboBox()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comPortComboBox.Items.Add(port);
            }

            if (comPortComboBox.Items.Count > 0)
            {
                comPortComboBox.SelectedIndex = 0;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (comPortComboBox.SelectedItem != null)
            {
                SelectedPort = comPortComboBox.SelectedItem.ToString();
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a COM port.");
            }
        }
    }
}
