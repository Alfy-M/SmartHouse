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
       
        Window window;
        
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
            gasSense.HeatingElementEnabled = true;
            displayT35.SimpleGraphics.BackgroundColor = GT.Color.Purple;
            GT.Timer timer = new GT.Timer(500);
            timer.Tick += my_display;
           timer.Start();
           window = displayT35.WPFWindow;
           window.TouchDown += new Microsoft.SPOT.Input.TouchEventHandler(heating_button);

          
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

        void heating_button(object sender, Microsoft.SPOT.Input.TouchEventArgs e) {
            int x;
            int y;
            e.GetPosition(window, 0, out x, out y);
            if (x >= 259 && x < 340 && y >= 199 && y < 240) {
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
            //bmp.Flush();
            TempHumidSI70.Measurement temp = tempHumidSI70.TakeMeasurement();
            //Debug.Print("Temp: " + temp.Temperature.ToString());
            //Debug.Print("Umidity: " + temp.RelativeHumidity.ToString());
            
           
            double gas = gasSense.ReadProportion();
            String textt = "Temp: " + temp.Temperature.ToString("F2");
            String textu = "Umidity: " + temp.RelativeHumidity.ToString("F2");
            String textg = "Gas: " + gas.ToString("F2")+" ( " +gasSense.ReadVoltage().ToString("F2")+" )";
            Font mfont = Resources.GetFont(Resources.FontResources.NinaB);
            displayT35.SimpleGraphics.Clear();
            displayT35.SimpleGraphics.DisplayText(textt, mfont,GT.Color.Cyan, 10, 10);
            displayT35.SimpleGraphics.DisplayText(textu, mfont, GT.Color.Cyan, 10, 25);
            displayT35.SimpleGraphics.DisplayText(textg, mfont, GT.Color.Cyan, 10, 40);
            displayT35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 1, GT.Color.Blue, 259, 199, 60, 40);
            if (gasSense.HeatingElementEnabled)
            {
                displayT35.SimpleGraphics.DisplayText("ON", mfont, GT.Color.Red, 284, 213);
            }
            else {
                displayT35.SimpleGraphics.DisplayText("OFF", mfont, GT.Color.Red, 284, 213);
            }
            
            
            //Debug.Print("Gas: " + gas);
            

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
