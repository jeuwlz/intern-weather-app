using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataSorter
{
    public partial class Form1 : Form
    {
        string connectionString = "Server=DESKTOP-IK9RFT0\\SQLEXPRESS;Database=WeatherData;Trusted_Connection=True;";

        public Form1()
        {
            InitializeComponent();
            LoadComboBox();
        }

        private void button1_Click(object sender, EventArgs e)
        {


            //dont display ID's
            //display station name using data aware control. 
            //use a combo box with binding source and data bindings. 
            //user will select station name which will translate to station_ID which can then be displayed. 
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedItem = comboBox1.SelectedItem.ToString();
            string query = string.Empty;

            switch (selectedItem)
            {
                case "Display All":
                    query = "SELECT Measurement_Time, Temperature, Humidity, Wind_Speed FROM Station_Measurements";
                    break;
                case "Temperature":
                    query = "SELECT Measurement_Time, Temperature FROM Station_Measurements";
                    break;
                case "Humidity":
                    query = "SELECT Measurement_Time, Humidity FROM Station_Measurements";
                    break;
                case "Wind Speed":
                    query = "SELECT Measurement_Time, Wind_Speed FROM Station_Measurements";
                    break;
            }

            LoadData(query);
        }

        private void LoadComboBox()
        {
            comboBox1.Items.Add("Display All");
            comboBox1.Items.Add("Temperature");
            comboBox1.Items.Add("Humidity");
            comboBox1.Items.Add("Wind Speed");
        }

        private void LoadData(string query)
        {
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                SqlDataAdapter sqlDa = new SqlDataAdapter(query, sqlCon);
                DataTable dtbl = new DataTable();
                sqlDa.Fill(dtbl);
                this.dtbl.DataSource = dtbl;
            }

        }

        private void dtbl_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}

