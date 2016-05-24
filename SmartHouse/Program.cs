using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Net;
using System.IO;
using System.Diagnostics;

using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;



using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Networking;

using System.Xml;




namespace SmartHouse
{
   
    public partial class Program
    {
     
         Window window;
         Boolean connection;//true=wifi,false=RJ45

        int navigation_delay = 1500;

        String pwd = "armando";
        private Object myLock = new Object();

        //variables for send data
        GT.Timer timerSend = new GT.Timer(60*1000);//1 minute
        GT.Timer timerRetryServer = new GT.Timer(10 * 60 * 1000);//riprovare mandare dati dopo 10 minuti
        Boolean server_available = false;

        //variables for main
        private Boolean connected;
        GT.Timer timerMain = new GT.Timer(9000);//9 secondi per tenere asincrono con timerSend e non avere lanci di 2 thread temporaneamente
        Button back_to_con;//deve essere globale,altrimenti il suo ciclo di vita e' piu' corta del handler! e si incasina tutto!
        Button go_to_menu;
        CheckBox gas_state;
       
        //variables for menu
        Button back_to_main; 
        Button ask_mday;
        Button ask_mweek;
        Button ask_mmonth;
        Boolean dataready = false;
        Boolean datarequested = false;
        String mtemp;
        String mhum;
        String mgas;

       // variable for alarm
        Button alarm_button_back;
        Boolean noAlarm;//to disactivate alarm check after one read it
        GT.Timer ResetNoAlarm = new GT.Timer(90 * 1000);//ogni 1.5 reshow alarm if error stil active;

        //variable bad connectors
        Button reset;

        //variablse for connection menu
        Button b_rj ;
        Button b_wifi;

        
  
       
       
        
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            noAlarm = false;
            ShowConnectionWindow();
                
           
            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.*/
            Debug.Print("Program Started");
            return;
        }

        private void ShowConnectionWindow(){
             window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.StartWindow));//carico window da mostrare
            GlideTouch.Initialize();
             b_rj = (Button)window.GetChildByName("rjbutton");
             b_wifi = (Button)window.GetChildByName("wifibutton");

            b_rj.TapEvent += ChooseRJ;
            b_wifi.TapEvent += ChooseWiFi;

            Glide.MainWindow = window;

            connected = false;
            return;
        
        }


        private void ChooseRJ(object sender)
        {
            try
            {
                lock (myLock)
                {
                    connection = false;
                    gasSense.HeatingElementEnabled = true;
                    SetupEthernet();
                    ethernetJ11D.NetworkUp += OnNetworkUp;
                    ethernetJ11D.NetworkDown += OnNetworkDown;
                    DrawMainWindow();//to avoid delay due to timer
                    ListNetworkInterfaces();
                    timerMain.Tick += DrawMainWindow;
                    timerMain.Start();
                    return;
                }
            }
            catch (System.ApplicationException)
            {  
                    DrawConnetorsAlarmWindow();
                    return;
            }
            catch (System.Exception)
            {
                    DrawConnetorsAlarmWindow();
                    return;

            }
        }

     
        private void ChooseWiFi(object sender)
        {   //connection wifi
            try
            {
                lock (myLock)
                {
                    connection = true;
                    gasSense.HeatingElementEnabled = true;
                    DrawMainWindow();//to avoid delay due to timer
                    timerMain.Tick += DrawMainWindow;
                    timerMain.Start();
                    wifiConnect();
                    return;
                }
            }
            catch (System.ApplicationException a)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
            catch (System.Exception)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
        }

        private void GoToMainFromAlarm(object sender) {
         
                timerMain.Tick += DrawMainWindow;
                timerMain.Start();
                DrawMainWindow();//to avoid delay due to timer
                return;
          
        }

        private void GoToMain(object sender){
            lock (myLock)
            {
                ask_mday.TapEvent -= SendReqDay;
                ask_mweek.TapEvent -= SendReqWeek;
                ask_mmonth.TapEvent -= SendReqMonth;
                back_to_main.TapEvent -= GoToMain;
                datarequested = false;
                dataready = false;
                timerMain.Tick += DrawMainWindow;
                timerMain.Start();
                DrawMainWindow();//to avoid delay due to timer
                return;
            }
        }

        private void GoToMenu(object sender){
            lock (myLock)//per finire draw main e solo dopo andare a menu
            {
                timerMain.Tick -= DrawMainWindow;
                timerMain.Stop();
                //Thread.Sleep(navigation_delay);
                DrawMenu();
                return;
            }
        }

        private void resetAll(object sender) {
            lock (myLock)//altrimenti puo provare lanciare thread mentre tolgo handler
            {
                timerSend.Tick -= sendData;
                timerMain.Tick -= DrawMainWindow;
                timerRetryServer.Tick -= RetryServer;
                ResetNoAlarm.Tick -= ActivateAlarm;
                timerMain.Stop();
                timerRetryServer.Stop();
                timerSend.Stop();
                ResetNoAlarm.Stop();
                Mainboard.PostInit();
                ShowConnectionWindow();
            }
            
        }

        
        private void GoToConnections(object sender) {
            try
            {
                lock (myLock)//per evitare che schiaccio back mentre scrivo window
                {
                    if (connection)
                    {//close wifi
                        wifiRS21.NetworkUp -= wifiRS21_NetworkUp;//altrimenti lancia piu' thread quando si conette
                        wifiRS21.NetworkDown -= wifiRS21_NetworkDown;
                        if (wifiRS21.NetworkInterface.LinkConnected)
                        {
                            wifiRS21.NetworkInterface.Disconnect();
                        }
                        wifiRS21.NetworkInterface.Close();
                        wifiRS21.NetworkInterface.ReleaseDhcpLease();

                    }
                    else
                    {
                        ethernetJ11D.NetworkUp -= OnNetworkUp;
                        ethernetJ11D.NetworkDown -= OnNetworkDown;
                        ethernetJ11D.NetworkInterface.Close();
                    }
                    connected = false;
                    server_available = false;
                    timerSend.Tick -= sendData;
                    timerMain.Tick -= DrawMainWindow;
                    timerMain.Stop();
                    timerSend.Stop();
                    //Thread.Sleep(navigation_delay);
                    ShowConnectionWindow();
                }
            }
            catch (System.ApplicationException a)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
            catch (System.Exception)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
            return;
        }

        private void DrawMainWindow(GT.Timer timer)
        {
            DrawMainWindow();
            return;
        }

        private void DrawAlarmWindow(double atemp, double ahumid, double agas) {

            lock (myLock)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.AttentionWindow));
                GlideTouch.Initialize();
                TextBox temp_box = (TextBox)window.GetChildByName("atemp");
                temp_box.Text = atemp.ToString("F2");

                TextBox umid_box = (TextBox)window.GetChildByName("ahum");
                umid_box.Text = ahumid.ToString("F2");

                TextBox gas_box = (TextBox)window.GetChildByName("agas");
                gas_box.Text = agas.ToString("F2");

                alarm_button_back = (Button)window.GetChildByName("back");
                alarm_button_back.TapEvent += GoToMainFromAlarm;

                Glide.MainWindow = window;
                return;
            }
        
        }

        private void DrawConnetorsAlarmWindow(){
            lock (myLock)
            {
                window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.CAlarmWindow));
                GlideTouch.Initialize();
                reset = (Button)window.GetChildByName("reset");
                reset.TapEvent += resetAll;
                Glide.MainWindow = window;
                return;
            }
        }
      
        private void DrawMainWindow()
        {
            try
            {
                lock (myLock)
                {
                    window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.MainWindow));//carico window da mostrare
                    GlideTouch.Initialize();
                    //read sensors

                    TempHumidSI70.Measurement temp = tempHumidSI70.TakeMeasurement();
                    double gas = gasSense.ReadProportion();
                    if (!noAlarm)
                    {
                        //check data on anomalia
                        if (gas > 1 || temp.Temperature > 70 || temp.RelativeHumidity > 100)
                        {
                            //ALARM
                            //n.b. Se durante attention message lui si connete/disconnette alla rete sara revisualizzato Main!!!!
                            //mandare subito errore
                            sendData();
                            timerMain.Tick -= DrawMainWindow;
                            timerMain.Stop();
                            Thread.Sleep(navigation_delay);
                            DrawAlarmWindow(temp.Temperature, temp.RelativeHumidity, gas);
                            noAlarm = true;
                            ResetNoAlarm.Tick += ActivateAlarm;
                            ResetNoAlarm.Start();
                            return;

                        }
                    }

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
                    else
                    {
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
                    else
                    {
                        conn_true.Visible = false;
                        conn_false.Visible = true; ;
                    }
                    //show server status
                    TextBlock server_true = (TextBlock)window.GetChildByName("serverstatustrue");
                    TextBlock server_false = (TextBlock)window.GetChildByName("serverstatusfalse");
                    if (server_available)
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
                    return;
                }
            }
            catch (System.ApplicationException ) {
                DrawConnetorsAlarmWindow();
                return;

            }
            catch (System.Exception)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
        }

        private void DrawMenu() {
            try
            {
                lock (myLock)
                {
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
                    if (dataready && !datarequested)
                    {
                        loading.Visible = false;
                        temp_text.Visible = true;
                        hum_text.Visible = true;
                        gas_text.Visible = true;
                        temp_data.Visible = true;
                        hum_data.Visible = true;
                        gas_data.Visible = true;
                        //riempio campi letti
                        temp_data.Text = mtemp;
                        hum_data.Text = mhum;
                        gas_data.Text = mgas;
                    }
                    else
                    {
                        if (datarequested)
                        {
                            loading.Visible = true;
                        }
                        else
                        {
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
                    ask_mday.TapEvent += SendReqDay;

                    ask_mweek = (Button)window.GetChildByName("mweek");
                    ask_mweek.TapEvent += SendReqWeek;

                    ask_mmonth = (Button)window.GetChildByName("mmonth");
                    ask_mmonth.TapEvent += SendReqMonth;


                    back_to_main = (Button)window.GetChildByName("backtomain");
                    back_to_main.TapEvent += GoToMain;


                    Glide.MainWindow = window;
                    return;
                }
            }
            catch (System.ApplicationException a)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
            catch (System.Exception)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
        }




        private void gasonoff(object sender)
        {
            try
            {
                gasSense.HeatingElementEnabled = !gasSense.HeatingElementEnabled;
                return;
            }
            catch (System.ApplicationException a)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
            catch (System.Exception)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
        }
        /*** per ethernet****/
        void ListNetworkInterfaces()
        {
            var settings = ethernetJ11D.NetworkSettings;

            Debug.Print("------------------------------------------------");
            //Debug.Print("MAC: " + ByteExtensions.ToHexString(settings.PhysicalAddress, "-"));
            Debug.Print("IP Address:   " + settings.IPAddress);
            Debug.Print("DHCP Enabled: " + settings.IsDhcpEnabled);
            Debug.Print("Subnet Mask:  " + settings.SubnetMask);
            Debug.Print("Gateway:      " + settings.GatewayAddress);
            Debug.Print("------------------------------------------------");
        }



        void SetupEthernet()
        {
            ethernetJ11D.NetworkInterface.Open();
            ethernetJ11D.UseThisNetworkInterface();
            ethernetJ11D.UseStaticIP(
                "192.168.43.10",
               "255.255.255.0",
                "192.168.43.240");
        }


        void OnNetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network down.");
            connected = false;
            return;
        }

        void OnNetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network up.");
            connected = true;
            server_available = true;
            DrawMainWindow();
            timerSend.Tick += sendData;
            timerSend.Start();
            //sendData();
            return;
        }
    /**end per ethernet**/

        private void wifiConnect()
        {
            try
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
            catch (System.ApplicationException a)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
            catch (System.Exception)
            {
                DrawConnetorsAlarmWindow();
                return;

            }
                
        }
        void wifiRS21_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network is down!");
            connected = false;
            return;
            
        }
        void wifiRS21_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            //Quando la connessione è "up" inizamo a trasmettere i dati al server
            Debug.Print("Network is up!");
            connected = true;
            server_available = true;
            DrawMainWindow();
            timerSend.Tick += sendData;
            timerSend.Start();//TODO: Capire perche' parte dopo tot tempo!!!!solution:problema stava in gestione della coda di thread
            return;
        }

        private void sendData(GT.Timer timer) {
            sendData();
        }

        private void sendData()
        {
            if (server_available && connected)
            {
                try
                {
                    TempHumidSI70.Measurement th = tempHumidSI70.TakeMeasurement();
                    // Create the form values
                  // var formValues = "password=" + "armando"/* + "&time=" + DateTime.Now.ToString() */+ "&temp=" + th.Temperature.ToString() + "&humid=" + th.RelativeHumidity.ToString() + "&gas=" + gasSense.ReadProportion().ToString();
                    var reqStart = "<RequestData xmlns=\"http://www.cosacosacosa.com/cosa\"><details>";
                   var formValues = pwd+/*"|" +DateTime.Now.ToString()+*/"|"+ th.Temperature.ToString("F2") + "|" + th.RelativeHumidity.ToString("F2") + "|" + gasSense.ReadProportion().ToString("F2");
                   var reqEnd="</details></RequestData>";
                  

                      Debug.Print(reqStart+formValues+reqEnd);
                    // Create POST content
                    
                      var content = Gadgeteer.Networking.POSTContent.CreateTextBasedContent(reqStart + formValues + reqEnd);
                    // Create the request
                    var request = Gadgeteer.Networking.HttpHelper.CreateHttpPostRequest(
                        @"http://192.168.43.244:51417/Service1.svc/sendData" // the URL to post to
                       // @"http://169.254.121.227:51417/Service1.svc/sendData"
                        , content // the form values
                        , "application/xml" // the mime type for an HTTP form
                    );

                    // Post the form
                    request.ResponseReceived += new HttpRequest.ResponseHandler(SendData_ResponseReceived);
                    request.SendRequest();
                    while (!request.IsReceived) ;
                   // Debug.Print("DEVO FINIRE ThREAD"); 

                }
                catch (System.ApplicationException a)
                {
                    DrawConnetorsAlarmWindow();
                    return;

                }
                catch (System.Exception)
                {
                    DrawConnetorsAlarmWindow();
                    return;

                }/* //ingestibile
                catch (Exception e)
                {
                    Debug.Print("Trovato eccezione" + e.Message);
                    server_available = false;
                    timerRetryServer.Tick += RetryServer;
                    timerRetryServer.Start();
                    return;
                }*/
                
               
            }
            return;

        }

        void SendData_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            //Debug.Print("Risposta ricevuta!");           
            if (response.StatusCode != "200")
            {
                Debug.Print("Errore nella comunicazione con il server durante caricamento data sul server.Status code: "+response.StatusCode);
                server_available = false;
                timerRetryServer.Tick += RetryServer;
                timerRetryServer.Start();
                return;
             
            }
            else
            {
                Debug.Print("Invio avvenuto con successo (Code: "+response.StatusCode+")");
                Debug.Print("Valore: " + response.Text+ ")");
             
            }
            return;
        }

        private void SendReq(String param) {
            if (connected && server_available)
            {
                try
                {
                    var reqStart = "<RequestData xmlns=\"http://www.cosacosacosa.com/cosa\"><details>";
                    var formValues = pwd+"|"+param;
                    var reqEnd = "</details></RequestData>";
                    var content = Gadgeteer.Networking.POSTContent.CreateTextBasedContent(reqStart + formValues + reqEnd);
                    var request = Gadgeteer.Networking.HttpHelper.CreateHttpPostRequest(
                        @"http://192.168.43.244:51417/Service1.svc/getData" // the URL to post to
                        , content // the form values
                        , "application/xml" // the mime type for an HTTP form
                    );
                    Debug.Print(reqStart + formValues + reqEnd);
                    request.ResponseReceived += new HttpRequest.ResponseHandler(GetData_ResponseReceived);
                    request.SendRequest();
                    while (!request.IsReceived) ;
                }
                catch (System.ApplicationException a)
                {
                    DrawConnetorsAlarmWindow();
                    return;

                }
                catch (System.Exception)
                {
                    DrawConnetorsAlarmWindow();
                    return;

                }/* //ingestibile
                catch (Exception) {
                    //TODO:Anche se non funziona 
                }*/
            }
            return;
        }

       private void SendReqDay(object sender){
            datarequested=true;
            DrawMenu();
            SendReq("today");//sta dopo drawmenu()cosi' questa funzione lavarera in background
            return;
        }

       private void SendReqWeek(object sender){
            datarequested=true;
            DrawMenu();
            SendReq("week");
            return;
        }
        private void SendReqMonth(object sender){
            datarequested=true;
            DrawMenu();
            SendReq("month");
            return;
        }

        void GetData_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            if (response.StatusCode != "200")
            {
                Debug.Print("Errore nella comunicazione con il server durante invio richiesta per medie.Status code: " + response.StatusCode);
                server_available = false;
                dataready = false;
               // datarequested = false; //Lascio true cosi' tengo loading sempre attivo(bloccato finche' non vado back)
                timerRetryServer.Tick += RetryServer;
                timerRetryServer.Start();
                return;

            }
            else
            {   //se sono tornato indietro inutile entrarci quando arriva risposta;
                if (datarequested)
                {
                    String r = response.Text;
                    var data = r.Split('|');
                    mtemp=data[1];
                    mhum = data[2];
                    mgas = data[3];

                    datarequested = false;
                    dataready = true;
                    DrawMenu();
                }
            }
            return;
        
        }
        

        private void  RetryServer(GT.Timer t) {
            server_available = true;
            timerRetryServer.Tick -= RetryServer;
            timerRetryServer.Stop();
            return;
        }

        private void ActivateAlarm(GT.Timer t) {
            noAlarm = false;
            ResetNoAlarm.Tick -= ActivateAlarm;
            ResetNoAlarm.Stop();
        }

   

    }
}
