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

        //variablse for connection menu
        Button b_rj ;
        Button b_wifi;

        
  
       
       
        
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            
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
            connection = false;
            gasSense.HeatingElementEnabled = true;
            //TODO:Add connection
            timerMain.Tick += DrawMainWindow;
            timerMain.Start();
            return;
        }

     
        private void ChooseWiFi(object sender)
        {   //connection wifi
            connection = true;
            gasSense.HeatingElementEnabled = true;
            DrawMainWindow();//to avoid delay due to timer
            timerMain.Tick += DrawMainWindow;
            timerMain.Start();
            wifiConnect();
            return;
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
            DrawMainWindow();//to avoid delay due to timer
            return;
        }

        private void GoToMenu(object sender){
            timerMain.Tick -= DrawMainWindow;
            timerMain.Stop();
           Thread.Sleep(navigation_delay);
            DrawMenu();
            return;
        }

        
        private void GoToConnections(object sender) {
            if (connection)
            {//close wifi
                if (wifiRS21.NetworkInterface.LinkConnected)
                {
                    wifiRS21.NetworkInterface.Disconnect();
                }
                wifiRS21.NetworkInterface.Close();
                wifiRS21.NetworkInterface.ReleaseDhcpLease();
               
            }
            else { 
                //TODO: close rj45
            }
             
            timerSend.Tick -= sendData;
            timerMain.Tick -= DrawMainWindow;
            timerMain.Stop();
            timerSend.Stop();
           Thread.Sleep(navigation_delay);
            ShowConnectionWindow();
            return;
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
        //this function to draw on start without waiting timer delay
        private void DrawMainWindow()
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
                //riempio campi letti
                temp_data.Text = mtemp;
                hum_data.Text = mhum;
                gas_data.Text = mgas;
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
            return;
        }




        private void gasonoff(object sender)
        {
            gasSense.HeatingElementEnabled = !gasSense.HeatingElementEnabled;
            return;
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
                //if (scanResult[i].Ssid == "mazzancolla_wifi")
                {
                    try
                    {
                        wifiRS21.NetworkInterface.Join(scanResult[i].Ssid, "marevivo");
                        //wifiRS21.NetworkInterface.Join(scanResult[i].Ssid, "N0F4N4C4NN4");
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
            timerSend.Start();//TODO: Capire perche' parte dopo tot tempo!!!!
            return;
        }


        private void sendData(GT.Timer tim)
        {
            if (server_available && connected)
            {
                try
                {
                    TempHumidSI70.Measurement th = tempHumidSI70.TakeMeasurement();
                    // Create the form values
                  // var formValues = "password=" + "armando"/* + "&time=" + DateTime.Now.ToString() */+ "&temp=" + th.Temperature.ToString() + "&humid=" + th.RelativeHumidity.ToString() + "&gas=" + gasSense.ReadProportion().ToString();
                    var reqStart = "<RequestData xmlns=\"http://www.cosacosacosa.com/cosa\"><details>";
                   var formValues = "armando|" +DateTime.Now.ToString()+"|"+ th.Temperature.ToString("F2") + "|" + th.RelativeHumidity.ToString("F2") + "|" + gasSense.ReadProportion().ToString("F2");
                   var reqEnd="</details></RequestData>";
                  

                      Debug.Print(reqStart+formValues+reqEnd);
                    // Create POST content
                    
                      var content = Gadgeteer.Networking.POSTContent.CreateTextBasedContent(reqStart + formValues + reqEnd);
                    // Create the request
                    var request = Gadgeteer.Networking.HttpHelper.CreateHttpPostRequest(
                        @"http://192.168.43.244:51417/Service1.svc/sendData" // the URL to post to
                       // @"http://192.168.1.133:51417/Service1.svc/sendData"
                        , content // the form values
                        , "application/xml" // the mime type for an HTTP form
                    );

                    // Post the form
                    request.ResponseReceived += new HttpRequest.ResponseHandler(SendData_ResponseReceived);
                    request.SendRequest();
                    while (!request.IsReceived) ;
                   // Debug.Print("DEVO FINIRE ThREAD"); 
                    
                }
                catch (Exception e)
                {
                    Debug.Print("Trovato eccezione" + e.Message);
                    server_available = false;
                    timerRetryServer.Tick += RetryServer;
                    timerRetryServer.Start();
                    return;
                }
            }
            return;

        }

        void SendData_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            //Debug.Print("Risposta ricevuta!");           
            if (response.StatusCode != "200")
            {
                Debug.Print("Errore nella comunicazione con il server.Status code: "+response.StatusCode);
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

       private void SendReqDay(object sender){
            datarequested=true;
            if (connected && server_available) {
                try {
                    var reqStart = "<RequestData xmlns=\"http://www.cosacosacosa.com/cosa\"><details>";
                    var formValues = "armando|day";
                    var reqEnd = "</details></RequestData>";
                    var content = Gadgeteer.Networking.POSTContent.CreateTextBasedContent(reqStart + formValues + reqEnd);
                    var request = Gadgeteer.Networking.HttpHelper.CreateHttpPostRequest(
                        @"http://192.168.43.244:51417/Service1.svc/sendData" // the URL to post to
                        , content // the form values
                        , "application/xml" // the mime type for an HTTP form
                    );
                    request.ResponseReceived += new HttpRequest.ResponseHandler(GetData_ResponseReceived);
                    request.SendRequest();
                    while (!request.IsReceived) ;
                }
                catch (Exception) { }
            }
         
            DrawMenu();
            return;
        }

       private void SendReqWeek(object sender){
            datarequested=true;
            //TODO
            DrawMenu();
            return;
        }
        private void SendReqMonth(object sender){
            datarequested=true;
            //TODO
            DrawMenu();
            return;
        }

        void GetData_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            if (response.StatusCode != "200")
            {
                Debug.Print("Errore nella comunicazione con il server.Status code: " + response.StatusCode);
                server_available = false;
                dataready = false;
                datarequested = false;
                timerRetryServer.Tick += RetryServer;
                timerRetryServer.Start();
                return;

            }
            else
            {
                //TODO:READ DATA FROM XML String
           /*****TEST*****/
        ///////////read xml
       // MemoryStream rms = new MemoryStream(response.Text.ToCharArray());
 
        XmlReaderSettings ss = new XmlReaderSettings();
        ss.IgnoreWhitespace = true;
        ss.IgnoreComments = false;
        //XmlException.XmlExceptionErrorCode.
        XmlReader xmlr = XmlReader.Create(rms,ss);
        while (!xmlr.EOF)
        {
            xmlr.Read();
            switch (xmlr.NodeType)
            {
                case XmlNodeType.Element:
                    Debug.Print("element: " + xmlr.Name);
                    break;
                case XmlNodeType.Text:
                    Debug.Print("text: " + xmlr.Value);
                    break;
                case XmlNodeType.XmlDeclaration:
                    Debug.Print("decl: " + xmlr.Name + ", " + xmlr.Value);
                    break;
                case XmlNodeType.Comment:
                    Debug.Print("comment " + xmlr.Value);
                    break;
                case XmlNodeType.EndElement:
                    Debug.Print("end element");
                    break;
                case XmlNodeType.Whitespace:
                    Debug.Print("white space");
                    break;
                case XmlNodeType.None:
                    Debug.Print("none");
                    break;
                default:
                    Debug.Print(xmlr.NodeType.ToString());
                    break;
            }
        }
                /***** END TEST******/
                
                
                datarequested = false;
                dataready = true;
            }
            return;
        
        }
        /*
            public Stream GenerateStreamFromString(string s)
{
    MemoryStream stream = new MemoryStream();
    StreamWriter writer = new StreamWriter(stream);
    writer.Write(s);
    writer.Flush();
    stream.Position = 0;
    return stream;
}*/

        private void  RetryServer(GT.Timer t) {
            server_available = true;
            timerRetryServer.Tick -= RetryServer;
            timerRetryServer.Stop();
            return;
        }

   

    }
}
