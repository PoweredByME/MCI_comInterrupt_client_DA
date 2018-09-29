using System;
using System.Net;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.IO;
using System.Collections.Generic;


namespace comInterpt
{
	class MainClass
	{
		static string d_comPort = "COM3", d_p_comPort = "COM4", d_webPort = "3070"; // default comports

		static string webPort = "";
		static string Prefix = "";
		static HttpListener Listener = null;
		static int RequestNumber = 0;
		static readonly DateTime StartupDate = DateTime.UtcNow;
		const int comport_baudrate = 230400;
		const Parity comport_parity = Parity.None;
		const int comport_databits = 8;
		const StopBits comport_stopbit = StopBits.One;
		const Handshake comport_handshake = Handshake.None;
		const string position_comPort_ack = "a";
		const string pressure_comPort_ack = "b";
		static string comPort = ""; // com port for position input controller
		static string p_comPort = ""; // com port for pressure input controller
		static SerialPort _serialport, _p_serialport;
        //static bool initPortsDone = false;
        //static bool shouldInitPorts = false;

		const short X_DAT = 0, Y_DAT = 1, Z_DAT = 2, U_DAT = 3, V_DAT = 4, W_DAT = 5, LP_DAT = 6, RP_DAT = 7;  // *P_DAT = Pressure input data index(-L- left or -R- right).
		const short x_ASCII = 120, y_ASCII = 121, z_ASCII = 122,
					u_ASCII = 117, v_ASCII = 118, w_ASCII = 119,
					X_ASCII = 88, Y_ASCII = 89, Z_ASCII = 90,
					U_ASCII = 85, V_ASCII = 86, W_ASCII = 87, zero_ASCII = 79,
                    S_ASCII = 83, H_ASCII = 72,


					left_p_ASCII = 113,   // *_p_ASCII = pressure input decrease ASCII (left or right)
					right_p_ASCII = 112,
					left_P_ASCII = 81,   // *_P_ASCII = pressure input increase ASCII (left or right)
					right_P_ASCII = 80,
					p_zero_ACII = 79
					;
		static int[] ctrl_data = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

        static int[] set_ctrl_data = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

        public static void searchControllerPort(){
            webPort = "3070";//Console.ReadLine().Trim();
            webPort = webPort == "" ? d_webPort : webPort;
            Prefix = "http://localhost:" + webPort + "/";



            var portsList = SerialPort.GetPortNames();
            foreach (var _port in portsList)
            {
                // for position controller
                var _p = new SerialPort(_port, comport_baudrate, comport_parity, comport_databits, comport_stopbit);
                _p.Handshake = comport_handshake;
                _p.DataReceived += new SerialDataReceivedEventHandler(_positionControl_testPort_dataReceived);
                try
                {
                    _p.Open();
                    if (_p.IsOpen)
                    {
                        _positionControler_portAck = false;
                        _p.Write("a");
                        for (int i = 0; i < 100; i++)
                        {
                            Thread.Sleep(1);
                            if(_positionControler_portAck){
                                comPort = _port;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }

                _p.Close();
            }

            foreach(var _port in portsList){
                // for pressure controller
                var _p = new SerialPort(_port, comport_baudrate, comport_parity, comport_databits, comport_stopbit);
                _p.Handshake = comport_handshake;
                _p.DataReceived += new SerialDataReceivedEventHandler(_positionControl_testPort_dataReceived);
                try
                {
                    _p.Open();
                    if (_p.IsOpen)
                    {
                        _pressureControler_portAck = false;
                        _p.Write("b");
                        for (int i = 0; i < 100; i++)
                        {
                            Thread.Sleep(1);
                            if (_pressureControler_portAck)
                            {
                                p_comPort = _port;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                _p.Close();

            }
        }

        static bool _positionControler_portAck = false;
        static void _positionControl_testPort_dataReceived(object sender, SerialDataReceivedEventArgs e){
            string s_data = _serialport.ReadExisting();
            if(s_data == "a"){
                _positionControler_portAck = true;
            }
        }

        static bool _pressureControler_portAck = false;
        static void _pressureControl_testPort_dataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string s_data = _serialport.ReadExisting();
            if (s_data == "b")
            {
                _pressureControler_portAck = true;
            }
        }


        public static void Main(string[] args)
		{
            searchControllerPort();
            if(!_positionControler_portAck || !_pressureControler_portAck){
                Console.WriteLine("Could not seach the controller on comports.");
                Console.WriteLine("PLEASE MANUALLY ENTER THE COMPORTS");
                getPorts();
            }

            initPorts();





			if (!HttpListener.IsSupported)
			{
				print("Http Listener is not supported on this platform.");
				Console.ReadLine();
				return;
			}
			else
			{
				print("Http Listner is supported. ATTEMPTING to START localhost");
			}
			try
			{
				using (Listener = new HttpListener())
				{
					Listener.Prefixes.Add(Prefix);
					Listener.Start();
					// Begin waiting for requests.
					Listener.BeginGetContext(GetContextCallback, null);
                    print("Local server init done...");
                    print("Listening to http://localhost:" + webPort + "/");
                    save_ctrl_data_To_TextFile();

					for (;;)
					{
						Console.WriteLine("Type 'end' to exit application : ");
						if (Console.ReadLine().Trim() == "end") { break; }
					};
				}
			}
			catch (Exception ex)
			{
				print(ex.ToString());
			}
		}

		static void GetContextCallback(IAsyncResult ar)
		{
            /*
            if(shouldInitPorts && !initPortsDone){
                initPorts();
                print("Com Ports init..");
                printLocalHostAndPortData(); 
                initPortsDone = true;
            }*/


			try
			{
                /*if (!shouldInitPorts){
                    shouldInitPorts = true;
                    return;
                }*/
				int req = ++RequestNumber;

				// Get the context
				var context = Listener.EndGetContext(ar);

				// listen for the next request
				Listener.BeginGetContext(GetContextCallback, null);

				// get the request
				var NowTime = DateTime.UtcNow;

				var responseString = string.Format(ctrl_data[X_DAT] + "," +
											   		ctrl_data[Y_DAT] + "," +
											   		ctrl_data[Z_DAT] + "," +
											   		ctrl_data[U_DAT] + "," +
											   		ctrl_data[V_DAT] + "," +
											   		ctrl_data[W_DAT] + "," +
													   ctrl_data[LP_DAT] + "," +
													   ctrl_data[RP_DAT]
											  );

				byte[] buffer = Encoding.UTF8.GetBytes(responseString);
				// and send it
				var response = context.Response;
				response.ContentType = "text/html";
				response.ContentLength64 = buffer.Length;
				response.StatusCode = 200;
				response.Headers.Remove("Access-Control-Allow-Origin");
				response.AddHeader("Access-Control-Allow-Origin", "*"/*, Listener.GetContext().Request.UrlReferrer.GetLeftPart(UriPartial.Authority)*/);

				//response.Headers.Remove("Access-Control-Allow-Credentials");
				//response.AddHeader("Access-Control-Allow-Credentials", "true");

				//response.Headers.Remove("Access-Control-Allow-Methods");
				//response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
				response.OutputStream.Write(buffer, 0, buffer.Length);
				response.OutputStream.Close();
			}
			catch (Exception ex)
			{
				print(ex.Message);
			}
		}

		// when input is recieved from the position input controller
		static void sp_DataRecieved(object sender, SerialDataReceivedEventArgs e)
		{
			try
			{
				string s_data = _serialport.ReadExisting();
				print("Processing POSITION CONTROLLER Input ...");
				foreach (char c in s_data)
				{
					int data = c;
					//print("Data Read (position input controller) = " + data.ToString());
					if (data == zero_ASCII)
					{
						ctrl_data = new int[8] { 0, 0, 0, 0, 0, 0, ctrl_data[LP_DAT], ctrl_data[RP_DAT] };
					}
					else if (data == x_ASCII)
					{
						ctrl_data[X_DAT]--;
					}
					else if (data == X_ASCII)
					{
						ctrl_data[X_DAT]++;
					}
					else if (data == y_ASCII)
					{
						ctrl_data[Y_DAT]--;
					}
					else if (data == Y_ASCII)
					{
						ctrl_data[Y_DAT]++;
					}
					else if (data == z_ASCII)
					{
						ctrl_data[Z_DAT]--;
					}
					else if (data == Z_ASCII)
					{
						ctrl_data[Z_DAT]++;
					}
					else if (data == u_ASCII)
					{
						ctrl_data[U_DAT]--;
					}
					else if (data == U_ASCII)
					{
						ctrl_data[U_DAT]++;
					}
					else if (data == v_ASCII)
					{
						ctrl_data[V_DAT]--;
					}
					else if (data == V_ASCII)
					{
						ctrl_data[V_DAT]++;
					}
					else if (data == w_ASCII)
					{
						ctrl_data[W_DAT]--;
					}
					else if (data == W_ASCII)
					{
						ctrl_data[W_DAT]++;
					}
                    else if (data == p_zero_ACII){
                        ctrl_data = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                        ctrl_data[X_DAT] = 0;
                        ctrl_data[Y_DAT] = 0;
                        ctrl_data[Z_DAT] = 0;
                        ctrl_data[U_DAT] = 0;
                        ctrl_data[V_DAT] = 0;
                        ctrl_data[W_DAT] = 0;
                    }else if (data == S_ASCII){
                        set_ctrl_data[X_DAT] = ctrl_data[X_DAT];
                        set_ctrl_data[Y_DAT] = ctrl_data[Y_DAT];
                        set_ctrl_data[Z_DAT] = ctrl_data[Z_DAT];
                        set_ctrl_data[U_DAT] = ctrl_data[U_DAT];
                        set_ctrl_data[V_DAT] = ctrl_data[V_DAT];
                        set_ctrl_data[W_DAT] = ctrl_data[W_DAT];
                    }else if (data == H_ASCII){
                        ctrl_data[X_DAT] = set_ctrl_data[X_DAT];
                        ctrl_data[Y_DAT] = set_ctrl_data[Y_DAT];
                        ctrl_data[Z_DAT] = set_ctrl_data[Z_DAT];
                        ctrl_data[U_DAT] = set_ctrl_data[U_DAT];
                        ctrl_data[V_DAT] = set_ctrl_data[V_DAT];
                        ctrl_data[W_DAT] = set_ctrl_data[W_DAT];
                    }
                    else
					{
					   	// do something	
					}

					//save_ctrl_data_To_TextFile();
				}
                printCTRLData();
			}
			catch (Exception ex)
			{
				print(ex.Message);
			}

		}


		// when input is recieved from the pressure input controller
		static void p_sp_DataRecieved(object sender, SerialDataReceivedEventArgs e)
		{
			try
			{
				string s_data = _p_serialport.ReadExisting();

				print("Processing PRESSURE CONTROLLER input..."); 

				foreach(char c in s_data){
					int data = c;
					//print("Data Read (pressure input controller) = " + data.ToString());//_p_serialport.ReadLine());//
					if (data == left_p_ASCII)
					{
						ctrl_data[LP_DAT]--;
					}
					else if (data == right_p_ASCII)
					{
						ctrl_data[RP_DAT]--;
					}
					else if (data == left_P_ASCII)
					{
						ctrl_data[LP_DAT]++;
					}
					else if (data == right_P_ASCII)
					{
						ctrl_data[RP_DAT]++;
					}
					else if (data == p_zero_ACII)
					{
						ctrl_data[RP_DAT] = 0;
						ctrl_data[LP_DAT] = 0;
                    }else if(data == S_ASCII){
                        set_ctrl_data[RP_DAT] = ctrl_data[RP_DAT];
                        set_ctrl_data[LP_DAT] = ctrl_data[LP_DAT];
                    }
                    else if (data == H_ASCII)
                    {
                        ctrl_data[RP_DAT] = set_ctrl_data[RP_DAT];
                        ctrl_data[LP_DAT] = set_ctrl_data[LP_DAT];
                    }
                    else
					{
						// do something
					}

				}
                printCTRLData();
				//save_ctrl_data_To_TextFile();
			}
			catch (Exception ex)
			{
				print(ex.Message);
			}
		}


		// get required port information
		static void getPorts()
		{
			// Ask for local server (localhost) port numeber 
			//Console.WriteLine("Enter the local server port - defualt "+d_webPort+" - (Press Enter) : ");
			webPort = "3070";//Console.ReadLine().Trim();
			webPort = webPort == "" ? d_webPort : webPort;
			Prefix = "http://localhost:" + webPort + "/";

            // Ask for position input controller comport
			Console.WriteLine("Enter the COM port to listen POSITION CONTROLLER (default " + d_comPort + "): ");
			comPort = Console.ReadLine().Trim();
			comPort = comPort == "" ? d_comPort : comPort;

			// Ask for pressure input controller comport
			Console.WriteLine("Enter the COM port to listen for PRESSURE CONTROLLER (default " + d_p_comPort + "): ");
			p_comPort = Console.ReadLine().Trim();
			p_comPort = p_comPort == "" ? d_p_comPort : p_comPort;
		}

        /*static string releventString = "";
        static bool releventStringRecieved = false;
        static SerialPort sp;
        static void checkReleventDevice(object sender, SerialDataReceivedEventArgs e){
            string s = sp.ReadExisting();
            if (s != releventString) { releventStringRecieved = false; }
            else { releventStringRecieved = true; }
        }

        static SerialPort searchPort(string charForInit, string portDevice = "device"){
            print("Searching for device: " + portDevice);
            releventString = charForInit;
            bool portFound = false;
            foreach(var port in System.IO.Ports.SerialPort.GetPortNames()){
                releventStringRecieved = false;
                sp = new SerialPort(port, comport_baudrate, comport_parity, comport_databits, comport_stopbit);
                sp.Handshake = comport_handshake;
                sp.DataReceived += new SerialDataReceivedEventHandler(checkReleventDevice);
                sp.Write(charForInit);
                for (int c = 0; c < 100; c++){
                    Thread.Sleep(1);
                }
                if (releventStringRecieved) {
                    portFound = true;
                    break;
                }
            }
            if(!portFound){
                throw new Exception("Relevent devices not found on any of the com ports");
            }
            return sp;
        }*/

        static void initPorts(){

            // Create comport connections
            _serialport = new SerialPort(comPort, comport_baudrate, comport_parity, comport_databits, comport_stopbit);
            _serialport.Handshake = comport_handshake;
            _serialport.DataReceived += new SerialDataReceivedEventHandler(sp_DataRecieved);
            _p_serialport = new SerialPort(p_comPort, comport_baudrate, comport_parity, comport_databits, comport_stopbit);
            _p_serialport.Handshake = comport_handshake;
            _p_serialport.DataReceived += new SerialDataReceivedEventHandler(p_sp_DataRecieved);



            try
            {
                // opening position input comport
                _serialport.Open();
                if (_serialport.IsOpen)
                {
                    _serialport.Write(position_comPort_ack);
                }
                else
                {
                    print(comPort + " could not be opened check connection");
                    Console.ReadLine();
                    return;
                }

                // opening pressure input comport 
                _p_serialport.Open();
                if (_p_serialport.IsOpen)
                {
                    _p_serialport.Write(pressure_comPort_ack);
                }
                else
                {
                    print(p_comPort + " could not be opened check connection");
                    Console.ReadLine();
                    return;
                }

            }
            catch (IOException ex)
            {
                print("ComPorts not working! \nError : " + ex.Message + "\nCheck if you HAVE connected controllers correctly to : \n" +
                      "1. Position Input Controller : " + comPort + "\n2. Pressure Input Controller : " + p_comPort);

            }
            catch (Exception ex)
            {
                print(ex.Message);
            }

        }



		// print the data in the control data list
		static void printCTRLData()
		{
			Console.WriteLine("X = " + ctrl_data[X_DAT]);
			Console.WriteLine("Y = " + ctrl_data[Y_DAT]);
			Console.WriteLine("Z = " + ctrl_data[Z_DAT]);
			Console.WriteLine("U = " + ctrl_data[U_DAT]);
			Console.WriteLine("V = " + ctrl_data[V_DAT]);
			Console.WriteLine("W = " + ctrl_data[W_DAT]);
			Console.WriteLine("Left Pressure = " + ctrl_data[LP_DAT]);
			Console.WriteLine("Right Pressure = " + ctrl_data[RP_DAT]);
		}

		static void printLocalHostAndPortData()
		{
			print("Local Server : localhost:" + webPort);
			print("Listening to the port (position input controller)... " + comPort);
			print("Listening to the port (pressure input controller)... " + p_comPort);
			print("\nListening to http://localhost:" + webPort + "/");
			print("Close the application to end the session.");
			print("Listening Now : ");
		}

		static void print(string msg)
		{
			Console.WriteLine(msg);
		}

		static void save_ctrl_data_To_TextFile()
		{
			var responseString = string.Format(ctrl_data[X_DAT] + "," +
				ctrl_data[Y_DAT] + "," +
				ctrl_data[Z_DAT] + "," +
				ctrl_data[U_DAT] + "," +
				ctrl_data[V_DAT] + "," +
				ctrl_data[W_DAT] + "," +
				ctrl_data[LP_DAT] + "," +
				ctrl_data[RP_DAT]
			);

			string fn = "MCI_control_data_text_bin.txt";
			StreamWriter sw = new StreamWriter(fn);
			sw.WriteLine(responseString);
			sw.Close();
		}


	}
}
