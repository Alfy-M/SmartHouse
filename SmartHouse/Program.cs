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

            displayT35.SimpleGraphics.BackgroundColor = GT.Color.Purple;
            GT.Timer timer = new GT.Timer(2000);
            timer.Tick += my_display;
           timer.Start();
          
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

        void my_display(GT.Timer timer)
        {
            //bmp.Flush();
            TempHumidSI70.Measurement temp = tempHumidSI70.TakeMeasurement();
            //Debug.Print("Temp: " + temp.Temperature.ToString());
            //Debug.Print("Umidity: " + temp.RelativeHumidity.ToString());
            gasSense.HeatingElementEnabled = true;
            double gas = gasSense.ReadProportion();
            String textt = "Temp: " + temp.Temperature.ToString("F2");
            String textu = "Umidity: " + temp.RelativeHumidity.ToString("F2");
            String textg = "Gas: " + gas.ToString("F2")+" ( " +gasSense.ReadVoltage().ToString("F2")+" )";
            Font mfont = Resources.GetFont(Resources.FontResources.NinaB);
            displayT35.SimpleGraphics.Clear();
            displayT35.SimpleGraphics.DisplayText(textt, mfont,GT.Color.Blue, 10, 10);
            displayT35.SimpleGraphics.DisplayText(textu, mfont, GT.Color.Brown, 10, 25);
            displayT35.SimpleGraphics.DisplayText(textg, mfont, GT.Color.Cyan, 10, 40);
            
            
            //Debug.Print("Gas: " + gas);
            

        }
       
    }
   

}
