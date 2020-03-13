using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Drawing;
using Microsoft.Reporting.WinForms;
using System;
using System.Windows.Controls;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace Ease_Bill
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            fnClearData();
            Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return System.IO.Path.GetDirectoryName(path);
            }
        }

        private void OnPrintBtnClicked(object sender, RoutedEventArgs e)
        {
            Cursor clOldCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            string clCurReportFilePath = System.IO.Path.Combine(AssemblyDirectory, "Bill.rdlc");
            if (!File.Exists(clCurReportFilePath) )
            {
                Mouse.OverrideCursor = clOldCursor;
                MessageBox.Show("Error occurs while printing.\nContact Developer.","Error");
                return;
            }

            LocalReport report = new LocalReport()
            {
                ReportPath = clCurReportFilePath
            };

            try
            {
                DataGridColumn clDescCol = cl_DataTable.ColumnFromDisplayIndex(0);
                DataGridColumn clChallanNoCol = cl_DataTable.ColumnFromDisplayIndex(1);
                DataGridColumn clChallanDateCol = cl_DataTable.ColumnFromDisplayIndex(2);
                DataGridColumn clSACCodeCol = cl_DataTable.ColumnFromDisplayIndex(3);
                DataGridColumn clQuantityCol = cl_DataTable.ColumnFromDisplayIndex(4);
                DataGridColumn clRateCol = cl_DataTable.ColumnFromDisplayIndex(5);
                DataGridColumn clAmountCol = cl_DataTable.ColumnFromDisplayIndex(6);

                DataTable clBillTable = new DataTable("BillInfo");
                clBillTable.Columns.Add("SrNo", typeof(string));
                clBillTable.Columns.Add("Description", typeof(string));
                clBillTable.Columns.Add("ChallanNo", typeof(string));
                clBillTable.Columns.Add("ChallanDate", typeof(string));
                clBillTable.Columns.Add("SACCode", typeof(string));
                clBillTable.Columns.Add("Quantity", typeof(Decimal));
                clBillTable.Columns.Add("Unit", typeof(string));
                clBillTable.Columns.Add("Rate", typeof(string));
                clBillTable.Columns.Add("Amount", typeof(Double));

                double dTotalAmount = 0;
                int iRowCount = cl_DataTable.Items.Count;

                clBillTable.Rows.Clear();

                for (int iRowLoop = 0; iRowLoop < iRowCount; iRowLoop++)
                {
                    Object clCurItem = cl_DataTable.Items.GetItemAt(iRowLoop);
                    var clDescTextBox = clDescCol.GetCellContent(clCurItem) as TextBlock;
                    var clChallanNoTextBox = clChallanNoCol.GetCellContent(clCurItem) as TextBlock;
                    var clChallanDateTextBox = clChallanDateCol.GetCellContent(clCurItem) as TextBlock;
                    var clSACCodeTextBox = clSACCodeCol.GetCellContent(clCurItem) as TextBlock;
                    var clQuantityTextBox = clQuantityCol.GetCellContent(clCurItem) as TextBlock;
                    var clRateTextBox = clRateCol.GetCellContent(clCurItem) as TextBlock;
                    var clAmountTextBox = clAmountCol.GetCellContent(clCurItem) as TextBlock;

                    int iQuantity = 0;
                    int.TryParse(clQuantityTextBox.Text, out iQuantity);

                    double dAmount = 0.0;
                    double.TryParse(clAmountTextBox.Text, out dAmount);

                    clBillTable.Rows.Add((iRowLoop+1).ToString(),
                                            clDescTextBox.Text,
                                            clChallanNoTextBox.Text,
                                            clChallanDateTextBox.Text,
                                            clSACCodeTextBox.Text,
                                            iQuantity,
                                            "Nos",
                                            clRateTextBox.Text,
                                            dAmount
                                            );

                    dTotalAmount += dAmount;
                }

                string clAddress = cl_Address.Text;
                clAddress.Replace('\n', ',');
                report.SetParameters(new ReportParameter("strBuyerName", cl_BuyerName.Text));
                report.SetParameters(new ReportParameter("strBuyerAddress", clAddress));
                report.SetParameters(new ReportParameter("strBuyerCity", cl_City.Text));
                report.SetParameters(new ReportParameter("strBuyerGSTIN", cl_BuyerGSTIN.Text));
                report.SetParameters(new ReportParameter("strInvoiceNo", cl_InvoiceNo.Text));
                report.SetParameters(new ReportParameter("strInvoiceDate", cl_InvoiceDate.Text));
                report.SetParameters(new ReportParameter("strOtherDetails", cl_OtherDetails.Text));
                report.SetParameters(new ReportParameter("strSubTotal", dTotalAmount.ToString("F")));
                report.SetParameters(new ReportParameter("strCGST", (dTotalAmount * 0.025).ToString("F")));
                report.SetParameters(new ReportParameter("strSGST", (dTotalAmount * 0.025).ToString("F")));
                dTotalAmount += (dTotalAmount * 0.05);
                int iForRounding = (int)( dTotalAmount + 0.49 ) ;
                dTotalAmount = iForRounding;
                report.SetParameters(new ReportParameter("strRsInWords", ConvertMyword((int)dTotalAmount)));
                report.SetParameters(new ReportParameter("strTotalAmount", dTotalAmount.ToString("F")));
                report.SetParameters(new ReportParameter("strFirmName", "HARDIK ART"));
                report.SetParameters(new ReportParameter("strBankName", "State Bank Of India"));
                report.SetParameters(new ReportParameter("strAccountNo", "36729467959"));
                report.SetParameters(new ReportParameter("strIFSCCode", "SBIN0005722"));

                report.SetParameters(new ReportParameter("strBillType", "Original"));

                report.DataSources.Clear();

                var reportDataSource1 = new ReportDataSource();
                reportDataSource1.Name = "BillInfo";
                reportDataSource1.Value = clBillTable;

                report.DataSources.Add(reportDataSource1);

                report.PrintToPrinter();

                if(cl_TriplicateBillCheckBox.IsChecked == true)
                {
                    report.SetParameters(new ReportParameter("strBillType", "Duplicate"));
                    report.PrintToPrinter();

                    report.SetParameters(new ReportParameter("strBillType", "Triplicate"));
                    report.PrintToPrinter();
                }

                if (MessageBox.Show(this, "Bill sent to printer. You want to clear bill data ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    fnClearData();
            }
            catch(Exception ex)
            {
                Mouse.OverrideCursor = clOldCursor;
                MessageBox.Show("Error occurs while exporting.\n If you think it's not your error, Contact Developer.\n\nError Message :- " + ex.Message, "Error");
            }

            Mouse.OverrideCursor = clOldCursor;
        }
        
        static string ConvertMyword(int number)
        {
            if (number == 0)
                return "Zero Rupees Only";
            int flag = 0;
            int lflag = 0;
            string words = String.Empty;
            string[] places = { "Ones", "Ten", "Hundred", "Thousand", "Ten Thousand", "Lacs", "Ten Lacs", "Crore", "Ten Crore" };
            string rawnumber = number.ToString();
            char[] a = rawnumber.ToCharArray();
            Array.Reverse(a);
            for (int i = a.Length - 1; i >= 0; i--)
            {
                if (i % 2 == 0 && i > 2)
                {
                    if (int.Parse(a[i].ToString()) > 1)
                    {
                        if (int.Parse(a[i - 1].ToString()) == 0)
                        {
                            words = words + getNumberStringty(int.Parse(a[i].ToString())) + " " + places[i - 1] + " ";
                        }
                        else
                        {
                            words = words + getNumberStringty(int.Parse(a[i].ToString())) + " ";
                        }
                    }
                    else if (int.Parse(a[i].ToString()) == 1)
                    {
                        if (int.Parse(a[i - 1].ToString()) == 0)
                        {
                            words = words + "Ten" + " ";
                        }
                        else
                        {
                            words = words + getNumberStringteen(int.Parse(a[i - 1].ToString())) + " ";
                        }
                        flag = 1;
                    }
                }
                else
                {
                    if (i == 1 || i == 0)
                    {
                        if (int.Parse(a[i].ToString()) > 1)
                        {
                            words = words + getNumberStringty(int.Parse(a[i].ToString())) + " " + getNumberString(int.Parse(a[0].ToString())) + " ";
                            break;
                        }
                        else if (int.Parse(a[i].ToString()) == 1)
                        {
                            if (int.Parse(a[i - 1].ToString()) == 0)
                            {
                                words = words + "Ten" + " ";
                            }
                            else
                            {
                                words = words + getNumberStringteen(int.Parse(a[i - 1].ToString())) + " ";
                            }

                            break;
                        }
                        else if (int.Parse(a[i - 1].ToString()) != 0)
                        {
                            words = words + getNumberString(int.Parse(a[i - 1].ToString())) + " ";
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (flag == 0)
                        {
                            for (int l = i; l >= 0; l--)
                            {
                                if (int.Parse(a[l].ToString()) != 0)
                                {
                                    lflag = 1;
                                }
                            }
                            if (lflag == 1 && int.Parse(a[i].ToString()) != 0)
                            {

                                words = words + getNumberString(int.Parse(a[i].ToString())) + " " + places[i] + " ";
                                lflag = 0;


                            }
                            else if (lflag == 0)
                            {
                                // words = words + getNumberString(int.Parse(a[i].ToString())) + " " + places[i] + " ";
                                lflag = 0;
                                break;
                            }

                        }
                        else
                        {
                            words = words + " " + places[i] + " ";
                            flag = 0;
                        }

                    }
                }
            }
            words += "Rupees Only";
            return words;
        }
        static string getNumberString(int num)
        {
            string Word = String.Empty;
            switch (num)
            {
                case 1:
                    Word = "One";
                    break;
                case 2:
                    Word = "Two";
                    break;

                case 3:
                    Word = "Three";
                    break;

                case 4:
                    Word = "Four";
                    break;

                case 5:
                    Word = "Five";
                    break;

                case 6:
                    Word = "Six";
                    break;
                case 7:
                    Word = "Seven";
                    break;

                case 8:
                    Word = "Eight";
                    break;

                case 9:
                    Word = "Nine";
                    break;


            }
            return Word;
        }
        static string getNumberStringty(int num)
        {
            string Word = String.Empty;
            switch (num)
            {

                case 2:
                    Word = "Twenty";
                    break;

                case 3:
                    Word = "Thirty";
                    break;

                case 4:
                    Word = "Fourty";
                    break;

                case 5:
                    Word = "Fifty";
                    break;

                case 6:
                    Word = "Sixty";
                    break;
                case 7:
                    Word = "Seventy";
                    break;

                case 8:
                    Word = "Eighty";
                    break;

                case 9:
                    Word = "Ninty";
                    break;


            }
            return Word;
        }
        static string getNumberStringteen(int num)
        {
            string Word = String.Empty;
            switch (num)
            {
                case 1:
                    Word = "Eleven";
                    break;
                case 2:
                    Word = "Tewlve";
                    break;

                case 3:
                    Word = "Thirteen";
                    break;

                case 4:
                    Word = "Fourteen";
                    break;

                case 5:
                    Word = "Fifteen";
                    break;

                case 6:
                    Word = "Sixteen";
                    break;
                case 7:
                    Word = "Seventeen";
                    break;

                case 8:
                    Word = "Eighteen";
                    break;

                case 9:
                    Word = "Ninteen";
                    break;


            }
            return Word;
        }

        private void OnClearFormBtnClicked(object sender, RoutedEventArgs e)
        {
            fnClearData();
        }

        public void fnClearData()
        {
            cl_Address.Text = "";
            cl_BuyerGSTIN.Text = "";
            cl_BuyerName.Text = "";
            cl_City.Text = "Jetpur";
            cl_InvoiceDate.Text = "";
            cl_InvoiceNo.Text = "";
            cl_CGSTTotal.Text = "0.00";
            cl_ChallanDateToAdd.Text = "";
            cl_ChallanNoToAdd.Text = "";
            cl_DataTable.Items.Clear();
            cl_DescriptionToAdd.Text = "";
            cl_OtherDetails.Text = "As Per Your Textile Frem Job Work";
            cl_QuantityToAdd.Text = "";
            cl_RateToAdd.Text = "";
            cl_SACCodeToAdd.Text = "998821";
            cl_SGSTTotal.Text = "0.00";
            cl_Subtotal.Text = "0.00";
            cl_Total.Text = "0.00";
        }

        private void OnAddNewRow(object sender, RoutedEventArgs e)
        {
            string strDescription   = cl_DescriptionToAdd.Text;
            string strChallanNo     = cl_ChallanNoToAdd.Text;
            string strChallanDate   = cl_ChallanDateToAdd.Text;
            string strSACCode       = cl_SACCodeToAdd.Text;
            string strQuantity      = cl_QuantityToAdd.Text;
            string strRate          = cl_RateToAdd.Text;

            double dQuantity = 0.0 ;
            double.TryParse(strQuantity, out dQuantity);

            double dRate = 0.0 ;
            double.TryParse(strRate, out dRate);
            string strAmount = (dQuantity * dRate).ToString();

            cl_DataTable.Items.Add(new { clDescripitonCol = strDescription, clChallanNoCol = strChallanNo, clChallanDateCol = strChallanDate, clSACCodeCol = strSACCode, clQuantityCol = strQuantity, clRateCol=strRate, clAmountCol=strAmount });
            
            DataGridColumn clAmountCol = cl_DataTable.ColumnFromDisplayIndex(6);
            double dTotalAmount = (dQuantity * dRate);
            int iRowCount = cl_DataTable.Items.Count;
            for (int iRowLoop = 0; iRowLoop < iRowCount-1; iRowLoop++)
            {
                Object clCurItem = cl_DataTable.Items.GetItemAt(iRowLoop);
                var clAmountTextBox = clAmountCol.GetCellContent(clCurItem) as TextBlock;
                
                if(clAmountTextBox != null)
                {
                    double dAmount = 0.0;
                    double.TryParse(clAmountTextBox.Text, out dAmount);
                    dTotalAmount += dAmount;
                }
            }
            cl_Subtotal.Text = dTotalAmount.ToString("F");
            cl_CGSTTotal.Text = (dTotalAmount * 0.025).ToString("F");
            cl_SGSTTotal.Text = (dTotalAmount * 0.025).ToString("F");
            cl_Total.Text = (dTotalAmount + (dTotalAmount * 0.05)).ToString("F");

            cl_DescriptionToAdd.Text = "";
            cl_ChallanNoToAdd.Text = "";
            cl_ChallanDateToAdd.Text = "";
            cl_SACCodeToAdd.Text = "998821";
            cl_QuantityToAdd.Text = "";
            cl_RateToAdd.Text = "";
        }

        private void OnDoubleClickDataTable(object sender, MouseButtonEventArgs e)
        {
            int iCurRow = cl_DataTable.SelectedIndex ;
            if(iCurRow != -1)
            {
                DataGridColumn clDescCol = cl_DataTable.ColumnFromDisplayIndex(0);
                DataGridColumn clChallanNoCol = cl_DataTable.ColumnFromDisplayIndex(1);
                DataGridColumn clChallanDateCol = cl_DataTable.ColumnFromDisplayIndex(2);
                DataGridColumn clSACCodeCol = cl_DataTable.ColumnFromDisplayIndex(3);
                DataGridColumn clQuantityCol = cl_DataTable.ColumnFromDisplayIndex(4);
                DataGridColumn clRateCol = cl_DataTable.ColumnFromDisplayIndex(5);
                DataGridColumn clAmountCol = cl_DataTable.ColumnFromDisplayIndex(6);

                Object clCurItem = cl_DataTable.Items.GetItemAt(iCurRow);
                var clDescTextBox = clDescCol.GetCellContent(clCurItem) as TextBlock;
                var clChallanNoTextBox = clChallanNoCol.GetCellContent(clCurItem) as TextBlock;
                var clChallanDateTextBox = clChallanDateCol.GetCellContent(clCurItem) as TextBlock;
                var clSACCodeTextBox = clSACCodeCol.GetCellContent(clCurItem) as TextBlock;
                var clQuantityTextBox = clQuantityCol.GetCellContent(clCurItem) as TextBlock;
                var clRateTextBox = clRateCol.GetCellContent(clCurItem) as TextBlock;
                
                cl_DescriptionToAdd.Text = clDescTextBox.Text;
                cl_ChallanNoToAdd.Text = clChallanNoTextBox.Text;
                cl_ChallanDateToAdd.Text = clChallanDateTextBox.Text;
                cl_SACCodeToAdd.Text = clSACCodeTextBox.Text;
                cl_QuantityToAdd.Text = clQuantityTextBox.Text;
                cl_RateToAdd.Text = clRateTextBox.Text;

                string strQuantity = cl_QuantityToAdd.Text;
                string strRate = cl_RateToAdd.Text;

                double dQuantity = 0.0;
                double.TryParse(strQuantity, out dQuantity);

                double dRate = 0.0;
                double.TryParse(strRate, out dRate);
                string strAmount = (dQuantity * dRate).ToString();

                double dTotalAmountToDel = (dQuantity * dRate);

                double dOldSubTotal = 0.0;
                double.TryParse(cl_Subtotal.Text, out dOldSubTotal);

                dOldSubTotal -= dTotalAmountToDel;

                cl_Subtotal.Text = dOldSubTotal.ToString("F");
                cl_CGSTTotal.Text = (dOldSubTotal * 0.025).ToString("F");
                cl_SGSTTotal.Text = (dOldSubTotal * 0.025).ToString("F");
                cl_Total.Text = (dOldSubTotal + (dOldSubTotal * 0.05)).ToString("F");

                cl_DataTable.Items.RemoveAt(iCurRow);
            }
        }
    }
}
