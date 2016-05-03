using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;
using System.IO;



using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace SmartHouse
{
   
    public partial class Program
    {
        //double factor;
        private Text txtSerial;
        private Text txtblank;
  
        private Canvas canvas;
      
        private Font baseFont;
       
       
        
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                GT.Timer timer = new GT.Timer(1000); // every second (1000ms)
                timer.Tick +=<tab><tab>
                timer.Start();
            *******************************************************************************************/
            //factor = gasCalibration();

            //define canvas
            baseFont = Resources.GetFont(Resources.FontResources.NinaB);
           canvas = new Canvas();
           displayT35.WPFWindow.Child = canvas;
           
            displayT35.WPFWindow.TouchDown += new Microsoft.SPOT.Input.TouchEventHandler(screen_click);
            gasSense.HeatingElementEnabled = true;
          
           // displayT35.SimpleGraphics.Clear();
           // displayT35.SimpleGraphics.BackgroundColor = GT.Color.Purple;
            //var bc = new BrushConverter();
            
            //show_states_page();
           GT.Timer timer = new GT.Timer(500);
          timer.Tick += my_display;
           timer.Start();

           display_state_page();
          
            /*

           TempHumidSI70.Measurement temp = tempHumidSI70.TakeMeasurement();
           //Debug.Print("Temp: " + temp.Temperature.ToString());
           //Debug.Print("Umidity: " + temp.RelativeHumidity.ToString());
           gasSense.HeatingElementEnabled = true;
           double gas = gasSense.ReadProportion();
           String textt = "Temp: " + temp.Temperature.ToString("F2");
           String textu = "Umidity: " + temp.RelativeHumidity.ToString("F2");
           String textg = "Gas: " + gas.ToString("F2");
           Font mfont = Resources.GetFont(Resources.FontResources.NinaB);
        
           displayT35.SimpleGraphics.DisplayText(textt, mfont, Color.White, 10, 10);
           displayT35.SimpleGraphics.DisplayText(textu, mfont, Color.White, 10, 25);
           displayT35.SimpleGraphics.DisplayText(textg, mfont, Color.White, 10, 40);
            */
            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.*/
            Debug.Print("Program Started");
        }

        void screen_click(object sender, Microsoft.SPOT.Input.TouchEventArgs e) {
            int x;
            int y;
            e.GetPosition(displayT35.WPFWindow, 0, out x, out y);
            if (x >= 259 && x < 340 && y >= 199 && y < 240) {
                //gas on of button pressed
                if (gasSense.HeatingElementEnabled)
                {
                    gasSense.HeatingElementEnabled = false;

                }
                else {
                    gasSense.HeatingElementEnabled = true;
                }
               
            }
        }
        
        void my_display(GT.Timer timer)
        {       
            TempHumidSI70.Measurement temp = tempHumidSI70.TakeMeasurement();
            double gas = gasSense.ReadProportion();
            String textt = temp.Temperature.ToString("F2");
            String textu = temp.RelativeHumidity.ToString("F2");
            String textg = gas.ToString("F2")+" (V: " +gasSense.ReadVoltage().ToString("F2")+" )";
            
            var window = displayT35.WPFWindow;
            baseFont = Resources.GetFont(Resources.FontResources.NinaB);
            //canvas_dyn = new Canvas();
            //window.Child = canvas_dyn;


            String txt = "        ";
            txtblank = new Text(baseFont, txt);
            txtSerial = new Text(baseFont, textt);
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtblank, 40);
            Canvas.SetLeft(txtblank, 150);
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


            /*
           
            if (gasSense.HeatingElementEnabled)
            {
                displayT35.SimpleGraphics.DisplayText("ON", mfont, GT.Color.Red, 284, 213);
            }
            else {
                displayT35.SimpleGraphics.DisplayText("OFF", mfont, GT.Color.Red, 284, 213);
            }
            */
            
            //Debug.Print("Gas: " + gas);
            

        }

        void show_states_page() {
            Font mfont = Resources.GetFont(Resources.FontResources.NinaB);
           displayT35.WPFWindow.DisplayModule.SimpleGraphics.DisplayText("Temp: ", mfont, GT.Color.Cyan, 10, 10);
           displayT35.WPFWindow.DisplayModule.SimpleGraphics.DisplayText("Umidity: ", mfont, GT.Color.Cyan, 10, 25);
           displayT35.WPFWindow.DisplayModule.SimpleGraphics.DisplayText("Gas: ", mfont, GT.Color.Cyan, 10, 40);
           displayT35.WPFWindow.DisplayModule.SimpleGraphics.DisplayRectangle(GT.Color.Black, 1, GT.Color.Blue, 259, 199, 60, 40);
        
        }
        private void display_state_page()
        {
            
           

            //
            // The following displays as intended: (OK)
            //
            txtSerial = new Text(baseFont, "Temp: ");
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 40);
            Canvas.SetLeft(txtSerial, 60);

            // The following displays as: The serial device is...
            //
            txtSerial = new Text(baseFont, "Humidity: ");
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 60);
            Canvas.SetLeft(txtSerial, 60);

            // The following displays as: The serial...
            //
            txtSerial = new Text(baseFont, "Gas: ");
            canvas.Children.Add(txtSerial);
            Canvas.SetTop(txtSerial, 80);
            Canvas.SetLeft(txtSerial, 60);
        }



        private double gasCalibration()
        {
            double sensorValue=0;
            double sensor_volt;
            double RS_air;
            for (int x = 0; x < 100; x++)
            {
                sensorValue = sensorValue + gasSense.ReadProportion(); 
            }
            sensorValue = sensorValue / 100.0;
            sensor_volt = sensorValue / 1024 * 5.0;
            RS_air = (5.0 - sensor_volt) / sensor_volt;
            return RS_air / 60.0;
        }
       
    }
   

}
