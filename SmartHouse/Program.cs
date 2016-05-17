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
        private static Boolean connection;//true=wifi,false=RJ45

        int navigation_delay = 1500;
        
        GT.Timer timerSend = new GT.Timer(1000*10);

        Boolean server_available = false;

        //variablse for main
        private Boolean connected;
        GT.Timer timerMain = new GT.Timer(1000);
        Button back_to_con;//deve essere globale,altrimenti il suo ciclo di vita e' piu' corta del handler! e si incasina tutto!
        Button go_to_menu;
        CheckBox gas_state;
       
        //variablse for menu
        Button back_to_main; 
        Button ask_mday;
        Button ask_mweek;
        Button ask_mmonth;
        Boolean dataready = false;
        Boolean datarequested = false;
        
  
       
       
        
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
            //TODO:Add connection
            timerMain.Tick += DrawMainWindow;
            timerMain.Start(); 
        }

     
        private void ChooseWiFi(object sender)
        {   //connection wifi
            connection = true;
            gasSense.HeatingElementEnabled = true;
            wifiConnect();
           // Thread.Sleep(500);
            timerMain.Tick += DrawMainWindow;
            timerMain.Start();    
        }

        private void GoToMain(object sender){
            
            ask_mday.TapEvent -= SendReqDay;
            ask_mweek.TapEvent -= SendReqWeek;
            ask_mmonth.TapEvent -= SendReqMonth;
            back_to_main.TapEvent -= GoToMain;
            datarequested = false;
            dataready = false;
            timerMain.Tick += DrawMainWindow;
            timerMain.Start();
        }

        private void GoToMenu(object sender){
            timerMain.Tick -= DrawMainWindow;
            timerMain.Stop();
           Thread.Sleep(navigation_delay);
            DrawMenu();
        }

        
        private void GoToConnections(object sender) {
            if (wifiRS21.NetworkInterface.LinkConnected)
            {
                wifiRS21.NetworkInterface.Disconnect();
            }
            wifiRS21.NetworkInterface.Close();
            timerMain.Stop();
            timerSend.Stop(); 
            timerSend.Tick -= sendData;
            timerMain.Tick -= DrawMainWindow;
           Thread.Sleep(navigation_delay);
            ShowConnectionWindow();
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
            //show server status
            TextBlock server_true = (TextBlock)window.GetChildByName("serverstatustrue");
            TextBlock server_false = (TextBlock)window.GetChildByName("serverstatusfalse");
            if (connected)
            {
                server_true.Visible = true;
                server_false.Visible = false;
            }
            else
            {
                server_true.Visible = false;
                server_false.Visible = true; ;
            }
            //gas on off button
            gas_state = (CheckBox)window.GetChildByName("gasonoff");
            gas_state.Checked = gasSense.HeatingElementEnabled;
            gas_state.TapEvent += gasonoff;

            //back to connections button
           back_to_con = (Button)window.GetChildByName("backtocon");
           back_to_con.TapEvent += GoToConnections;
            //go to menu
           go_to_menu = (Button)window.GetChildByName("menu");
           go_to_menu.TapEvent += GoToMenu;

            //if need to write ip of board use
            //string ip=wifiRS21.NetworkInterface.IPAddress;


             Glide.MainWindow = window;
        }

        private void DrawMenu() {
            window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.ComWindow));//carico window da mostrare
            GlideTouch.Initialize();

            TextBlock loading = (TextBlock)window.GetChildByName("loading");
            TextBlock temp_text = (TextBlock)window.GetChildByName("temptext");
            TextBlock hum_text = (TextBlock)window.GetChildByName("humtext");
            TextBlock gas_text = (TextBlock)window.GetChildByName("gastext");
            TextBox temp_data = (TextBox)window.GetChildByName("tempdata");
            TextBox hum_data = (TextBox)window.GetChildByName("humdata");
            TextBox gas_data = (TextBox)window.GetChildByName("gasdata");

            //ch0ose what to show
            if (dataready)
            {
                loading.Visible = false;
                temp_text.Visible = true;
                hum_text.Visible = true;
                gas_text.Visible = true;
                temp_data.Visible = true;
                hum_data.Visible = true;
                gas_data.Visible = true;
                //TODO:riempire campi con valori ricevuti
            }
            else {
                if (datarequested)
                {
                    loading.Visible = true;
                }
                else {
                    loading.Visible = false; ;
                }
                temp_text.Visible = false;
                hum_text.Visible = false;
                gas_text.Visible = false;
                temp_data.Visible = false;
                hum_data.Visible = false;
                gas_data.Visible = false;
            
            }

            //buttons inizialization

            ask_mday = (Button)window.GetChildByName("mday");
            ask_mday.TapEvent +=SendReqDay;

            ask_mweek = (Button)window.GetChildByName("mweek");
            ask_mweek.TapEvent +=SendReqWeek;

            ask_mmonth = (Button)window.GetChildByName("mmonth");
            ask_mmonth.TapEvent +=SendReqMonth;


           back_to_main = (Button)window.GetChildByName("backtomain");
           back_to_main.TapEvent += GoToMain;


            Glide.MainWindow = window;
        
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
            server_available = true;
            timerSend.Tick += sendData;
            timerSend.Start();//TODO: Capire perche' parte dopo tot tempo!!!!
        }


        private void sendData(GT.Timer tim)
        {
            if (server_available && connected)
            {
                try {
                    string url = "http://192.168.43.244:51417/Service1.svc/data/" + ((int)(tempHumidSI70.TakeMeasurement().Temperature * 100)).ToString() + "/" + ((int)(tempHumidSI70.TakeMeasurement().RelativeHumidity * 100)).ToString() + "/" + ((int)(gasSense.ReadProportion() * 100)).ToString();
                    Debug.Print(url);
                    var request = HttpHelper.CreateHttpGetRequest(url);
                    request.ResponseReceived += new HttpRequest.ResponseHandler(req_ResponseReceived);
                    request.SendRequest();     
                }
                catch (System.ObjectDisposedException)
                {
                    server_available = false;
                    return;
                }
            }
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

       private void SendReqDay(object sender){
            datarequested=true;
            //TODO
            DrawMenu();
        }

       private void SendReqWeek(object sender){
            datarequested=true;
            //TODO
            DrawMenu();
        }
        private void SendReqMonth(object sender){
            datarequested=true;
            //TODO
            DrawMenu();
        }

   

    }
}
