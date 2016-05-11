using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
/*using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;*/
using System.IO;

using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;



using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace SmartHouse
{
   
    public partial class Program
    {
      /*
       // private Text txtSerial;
       // private Rectangle button_gas;//UIElement type
        private Canvas canvas;
      
        private Font baseFont;
        private static Boolean connection;//true=wifi,false=RJ45;
        private int window_id;//current menu:0-connection,1-main
        private Boolean connected;
        */
        private static Window window;
        private Boolean connected;
        private static Boolean connection;//true=wifi,false=RJ45
        GT.Timer timerMain = new GT.Timer(1000);

  
       
       
        
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.StartWindow));//carico window da mostrare
            GlideTouch.Initialize();
            Button b_rj = (Button)window.GetChildByName("rjbutton");
            Button b_wifi = (Button)window.GetChildByName("wifibutton");

            b_rj.TapEvent += ChooseRJ;
            b_wifi.TapEvent += ChooseWiFi;

            Glide.MainWindow = window;

            connected = false;

           // GT.Timer timer = new GT.Timer(1000);
           // timer.Tick += my_display_managment;
           // timer.Start();

     

            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.*/
            Debug.Print("Program Started");
        }


        private void ChooseRJ(object sender)
        {
            connection = false;
            gasSense.HeatingElementEnabled = true;
            timerMain.Tick += DrawMainWindow;
            timerMain.Start(); 
        }

     
        private void ChooseWiFi(object sender)
        {
            connection = true;
            gasSense.HeatingElementEnabled = true;
            timerMain.Tick += DrawMainWindow;
            timerMain.Start();  
           
        }

        private void DrawMainWindow(GT.Timer timer)
        {
            window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.MainWindow));//carico window da mostrare
            GlideTouch.Initialize();
            //read sensors
            TempHumidSI70.Measurement temp = tempHumidSI70.TakeMeasurement();
            double gas = gasSense.ReadProportion();
            //fill stuff
            TextBox temp_box = (TextBox)window.GetChildByName("tempvalue"); 
            temp_box.Text = temp.Temperature.ToString("F2");

            TextBox umid_box = (TextBox)window.GetChildByName("umidvalue");
            umid_box.Text = temp.RelativeHumidity.ToString("F2");

            TextBox gas_box = (TextBox)window.GetChildByName("gasvalue");
            gas_box.Text = gas.ToString("F2");

            TextBlock conn_type = (TextBlock)window.GetChildByName("context");
            if (connection)
            {
                conn_type.Text = "WiFi";
            }
            else {
                conn_type.Text = "RJ45";
            }

            CheckBox gas_state = (CheckBox)window.GetChildByName("gasonoff");
            gas_state.Checked = gasSense.HeatingElementEnabled;
            gas_state.TapEvent += gasonoff;


             Glide.MainWindow = window;
        }


        private void gasonoff(object sender)
        {
            gasSense.HeatingElementEnabled = !gasSense.HeatingElementEnabled;
        }
  
        /*

       void my_display_managment(GT.Timer timer){
           if (connected)
           {
               if (window_id == 1) { my_display_main(); }
           }
           else {
               my_connection_menu();
           }
        
        }

        void screen_click(object sender, Microsoft.SPOT.Input.TouchEventArgs e) {
            int x;
            int y;
            e.GetPosition(displayT35.WPFWindow, 0, out x, out y);
            if (window_id == 1)
            {//se pressed on main menu
                if (x >= 259 && x < 340 && y >= 199 && y < 240)
                {
                    //gas on of button pressed
                    if (gasSense.HeatingElementEnabled)
                    {
                        gasSense.HeatingElementEnabled = false;

                    }
                    else
                    {
                        gasSense.HeatingElementEnabled = true;
                    }
                    my_display_main();//TODO: see why not working faster

                }
            }
            if (window_id == 0) {
                if ((y >= 60 && y < 120) ||( y >= 160 && y < 220))
                {
                    if (y >= 60 && y < 120)
                    {//wifi chosen
                        connection = true;
                    }
                    if (y >= 160 && y < 220)
                    {//rj45 chosen
                        connection = false;
                    }
                    //pressed on connecton menu
                    connected = true;
                    window_id = 1;//pass to next menu
                    gasSense.HeatingElementEnabled = true;//turn on gas sensor
                    //TODO: manage connections
                }//else do nothing
            }
        }
        
        //void my_display_main(GT.Timer timer)
        void my_display_main()
        {

            baseFont = Resources.GetFont(Resources.FontResources.NinaB);
            canvas = new Canvas();
            displayT35.WPFWindow.Background = new SolidColorBrush(GT.Color.Cyan);
            displayT35.WPFWindow.Child = canvas;
            TempHumidSI70.Measurement temp = tempHumidSI70.TakeMeasurement();

            double gas = gasSense.ReadProportion();
            String textt = temp.Temperature.ToString("F2");
            String textu = temp.RelativeHumidity.ToString("F2");
            String textg = gas.ToString("F2")+" (V: " +gasSense.ReadVoltage().ToString("F2")+" )";
                  
            baseFont = Resources.GetFont(Resources.FontResources.NinaB);
      

            //text
            txtSerial = new Text(baseFont, "Temp: ");
            txtSerial.ForeColor = GT.Color.Purple;
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 40);
            Canvas.SetLeft(txtSerial, 60);

         
            txtSerial = new Text(baseFont, "Humidity: ");
            txtSerial.ForeColor = GT.Color.Purple;
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 60);
            Canvas.SetLeft(txtSerial, 60);

   
            txtSerial = new Text(baseFont, "Gas: ");
            txtSerial.ForeColor = GT.Color.Purple;
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 80);
            Canvas.SetLeft(txtSerial, 60);
            //data from sensors
            txtSerial = new Text(baseFont, textt);
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 40);
            Canvas.SetLeft(txtSerial, 150);

            txtSerial = new Text(baseFont, textu);
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 60);
            Canvas.SetLeft(txtSerial, 150);

            txtSerial = new Text(baseFont, textg);
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 80);
            Canvas.SetLeft(txtSerial, 150);

            //connection type
                //draw frame
            Rectangle button_gas = new Rectangle(154, 40);
            button_gas.Stroke = new Pen(GT.Color.Purple);//colore di bordo
            canvas.Children.Add(button_gas);
            Canvas.SetTop(button_gas, 140);
            Canvas.SetLeft(button_gas, 165);

            txtSerial = new Text(baseFont, "Connection type: ");
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 150);
            Canvas.SetLeft(txtSerial, 170);

            if (connection) {
                txtSerial = new Text(baseFont, "WiFi");
                canvas.Children.Add(txtSerial);
                Canvas.SetTop(txtSerial, 150);
                Canvas.SetLeft(txtSerial, 285);

            } else {
                txtSerial = new Text(baseFont, "RJ45");
                canvas.Children.Add(txtSerial);
                Canvas.SetTop(txtSerial, 150);
                Canvas.SetLeft(txtSerial, 285);

            
            }

            //gas on/of
            txtSerial = new Text(baseFont, "Gas Sensor: ");
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 213);
            Canvas.SetLeft(txtSerial,170);

            button_gas = new Rectangle(100,60);
            button_gas.Stroke = new Pen(GT.Color.Purple);//colore di bordo
            canvas.Children.Add(button_gas);
            Canvas.SetTop(button_gas, 199);
            Canvas.SetLeft(button_gas, 259);
    
           //manage gas button state
            if (gasSense.HeatingElementEnabled)
            {
                
                txtSerial = new Text(baseFont, "ON");
                canvas.Children.Add(txtSerial);
                Canvas.SetTop(txtSerial,213);
                Canvas.SetLeft(txtSerial, 280);
                button_gas.Fill = new SolidColorBrush(GT.Color.Green);
            }
            else {
               
                txtSerial = new Text(baseFont, "OFF");
                canvas.Children.Add(txtSerial);
                Canvas.SetTop(txtSerial, 213);
                Canvas.SetLeft(txtSerial, 280);
                button_gas.Fill = new SolidColorBrush(GT.Color.Red);
            }
            
            
            //Debug.Print("Gas: " + gas);
            

        }

        void my_connection_menu() {
           
            canvas = new Canvas();
            displayT35.WPFWindow.Child = canvas;
            displayT35.WPFWindow.Background = new SolidColorBrush(GT.Color.Blue);

            txtSerial = new Text(baseFont, "Choose connection type");
            txtSerial.ForeColor=GT.Color.Red;
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial,20);
            Canvas.SetLeft(txtSerial, 80);

            Rectangle button_wifi = new Rectangle(320, 60);
            button_wifi.Fill = new SolidColorBrush(GT.Color.Green); ;//colore di bordo
            canvas.Children.Add(button_wifi);
            Canvas.SetTop(button_wifi, 60);
            Canvas.SetLeft(button_wifi, 0);

            txtSerial = new Text(baseFont, "WiFi");
            txtSerial.ForeColor = GT.Color.White;
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 80);
            Canvas.SetLeft(txtSerial, 145);

            Rectangle button_rj = new Rectangle(320, 60);
            button_rj.Fill = new SolidColorBrush(GT.Color.Green); ;//colore di bordo
            canvas.Children.Add(button_rj);
            Canvas.SetTop(button_rj, 160);
            Canvas.SetLeft(button_rj, 0);

            txtSerial = new Text(baseFont, "RJ45");
            txtSerial.ForeColor = GT.Color.White;
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 180);
            Canvas.SetLeft(txtSerial, 145);
        
        
        
        }

       */
    }
   

}
