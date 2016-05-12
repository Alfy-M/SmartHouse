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
using GHI.Networking;


namespace SmartHouse
{
   
    public partial class Program
    {
     
        private static Window window;
        private Boolean connected;
        private static Boolean connection;//true=wifi,false=RJ45
        GT.Timer timerMain = new GT.Timer(1000);
        GT.Timer timerSend = new GT.Timer(15000);

  
       
       
        
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            ShowConnectionWindow();

           // GT.Timer timer = new GT.Timer(1000);
           // timer.Tick += my_display_managment;
           // timer.Start();

     

            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.*/
            Debug.Print("Program Started");
        }

        private void ShowConnectionWindow(){
             window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.StartWindow));//carico window da mostrare
            GlideTouch.Initialize();
            Button b_rj = (Button)window.GetChildByName("rjbutton");
            Button b_wifi = (Button)window.GetChildByName("wifibutton");

            b_rj.TapEvent += ChooseRJ;
            b_wifi.TapEvent += ChooseWiFi;

            Glide.MainWindow = window;

            connected = false;
        
        }


        private void ChooseRJ(object sender)
        {
            connection = false;
            gasSense.HeatingElementEnabled = true;
            timerMain.Tick += DrawMainWindow;
            timerMain.Start(); 
        }

     
        private void ChooseWiFi(object sender)
        {   //connection wifi
            connection = true;
            gasSense.HeatingElementEnabled = true;
            timerMain.Tick += DrawMainWindow;
            timerMain.Start(); 
            wifiConnect();
           
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

            //show connection type
            TextBlock conn_type = (TextBlock)window.GetChildByName("context");
            if (connection)
            {
                conn_type.Text = "WiFi";
               
            }
            else {
                conn_type.Text = "RJ45";
            }
            //show connection status
            TextBlock conn_true = (TextBlock)window.GetChildByName("constatustrue");
            TextBlock conn_false = (TextBlock)window.GetChildByName("constatusfalse");
            if (connected)
            {
                conn_true.Visible = true;
                conn_false.Visible = false;
            }
            else {
                conn_true.Visible = false;
                conn_false.Visible = true; ;
            }
            //gas on off button
            CheckBox gas_state = (CheckBox)window.GetChildByName("gasonoff");
            gas_state.Checked = gasSense.HeatingElementEnabled;
            gas_state.TapEvent += gasonoff;

            //back to connections button
           // Button back = (Button)window.GetChildByName("backtocon");
           // back.TapEvent += GoToConnections;

            //if need to write ip of board use
            //string ip=wifiRS21.NetworkInterface.IPAddress;


             Glide.MainWindow = window;
        }

        private void GoToConnections(object sender) {
            wifiRS21.NetworkInterface.Disconnect();
            wifiRS21.NetworkInterface.Close();
            //timerMain.Stop();
           // timerMain.Tick += new GT.Timer.TickEventHandler((object sender) => { return; });
            ShowConnectionWindow();
        }


        private void gasonoff(object sender)
        {
            gasSense.HeatingElementEnabled = !gasSense.HeatingElementEnabled;
        }

        private void wifiConnect()
        {
            wifiRS21.NetworkUp += wifiRS21_NetworkUp;
            wifiRS21.NetworkDown += wifiRS21_NetworkDown;
            wifiRS21.NetworkInterface.Open();
            wifiRS21.NetworkInterface.EnableDhcp();
            wifiRS21.NetworkInterface.EnableDynamicDns();
            WiFiRS9110.NetworkParameters[] scanResult = wifiRS21.NetworkInterface.Scan();
            for (int i = 0; i < scanResult.Length; i++)
            {
                if (scanResult[i].Ssid == "PucciH")
                {
                    try
                    {
                        wifiRS21.NetworkInterface.Join(scanResult[i].Ssid, "marevivo");
                    }
                    catch (WiFiRS9110.JoinException e)
                    {
                        Debug.Print("Error Message: " + e.Message);
                    }
                    break;
                }
            }
            /*
            while (wifiRS21.NetworkInterface.IPAddress == "0.0.0.0")
            {
                Thread.Sleep(500);
            }*/
           // Debug.Print("Ip-Address = " + wifiRS21.NetworkInterface.IPAddress);
            return;
                
        }
        void wifiRS21_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network is down!");
            connected = false;
            
        }
        void wifiRS21_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            //Quando la connessione è "up" inizamo a trasmettere i dati al server
            Debug.Print("Network is up!");
            connected = true;
            timerSend.Tick += sendData;
            timerSend.Start();//TODO: Capire perche' parte dopo tot tempo!!!!
            //sendData();
        }


        private void sendData(GT.Timer tim)
        {



            string url = "http://192.168.43.244:51417/Service1.svc/data/" + ((int)(tempHumidSI70.TakeMeasurement().Temperature * 100)).ToString() + "/" + ((int)(tempHumidSI70.TakeMeasurement().RelativeHumidity * 100)).ToString() + "/" + ((int)(gasSense.ReadProportion() * 100)).ToString();
            Debug.Print(url);
            var request = HttpHelper.CreateHttpGetRequest(url);
            request.ResponseReceived += new HttpRequest.ResponseHandler(req_ResponseReceived);
            request.SendRequest();
            return;

        }

        void req_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
           
            if (response.StatusCode != "200")
            {
                Debug.Print("Errore nella comunicazione con il server");
             
            }
            else
            {
                Debug.Print("Invio avvenuto con successo (Code: "+response.StatusCode+")");
                Debug.Print("Valore: " + response.Text+ ")");
             
            }
        }
   

    }
}
