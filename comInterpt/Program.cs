using System;
using System.Net;
using System.Text;
using System.IO.Ports;
using System.Threading;


namespace comInterpt
{
	class MainClass
	{
		const string webPort = "3070";
		const string Prefix = "http://+:"+webPort+"/";
		static HttpListener Listener = null;
		static int RequestNumber = 0;
		static readonly DateTime StartupDate = DateTime.UtcNow;

		static string comPort = "COM3";
		static SerialPort _serialport;
		const  short X_DAT = 0, Y_DAT = 1, Z_DAT = 2, U_DAT = 3, V_DAT = 4, W_DAT = 5;
		const short x_ASCII = 120, y_ASCII = 121, z_ASCII = 122,
					u_ASCII = 117, v_ASCII = 118, w_ASCII = 119,
					X_ASCII = 88, Y_ASCII = 89, Z_ASCII = 90,
					U_ASCII = 85, V_ASCII = 86, W_ASCII = 87, zero_ASCII = 79;
		static int[] ctrl_data = new int[6] {0,0,0,0,0,0};
		public static void Main(string[] args)
		{
			
			Console.WriteLine("Enter the COM port to listen for input: ");
			comPort = Console.ReadLine().Trim();
			comPort = comPort == "" ? "COM3" : comPort;
			Console.WriteLine("Listening to the port ... " + comPort);
			_serialport = new SerialPort(comPort, 230400, Parity.None, 8, StopBits.One);
			_serialport.Handshake = Handshake.None;
			_serialport.DataReceived += new SerialDataReceivedEventHandler(sp_DataRecieved);
			try{
				_serialport.Open();
				if(_serialport.IsOpen){
					_serialport.Write("a");
				}
			}catch(Exception ex){
				Console.WriteLine(ex.Message);
			}
			if (!HttpListener.IsSupported)
			{
				Console.WriteLine("HttpListener is not supported on this platform.");
				return;
			}
			using (Listener = new HttpListener())
			{
				Listener.Prefixes.Add(Prefix);
				Listener.Start();
				// Begin waiting for requests.
				Listener.BeginGetContext(GetContextCallback, null);
				Console.WriteLine("Listening to http://localhost:" + webPort + "/");
				Console.WriteLine("Close the application to end the session.");
				Console.WriteLine ("Listening Now : ");
				for (;;) {};
			}
		}

		static void GetContextCallback(IAsyncResult ar)
		{
			try
			{
				int req = ++RequestNumber;

				// Get the context
				var context = Listener.EndGetContext(ar);

				// listen for the next request
				Listener.BeginGetContext(GetContextCallback, null);

				// get the request
				var NowTime = DateTime.UtcNow;

				//Console.WriteLine("{0}: {1}", NowTime.ToString("R"), context.Request.RawUrl);


				var responseString = string.Format(ctrl_data[X_DAT] + "," +
											   ctrl_data[Y_DAT] + "," +
											   ctrl_data[Z_DAT] + "," +
											   ctrl_data[U_DAT] + "," +
											   ctrl_data[V_DAT] + "," +
											   ctrl_data[W_DAT]
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
			}catch(Exception ex){
				Console.WriteLine(ex.Message);
			}
		}

		static void sp_DataRecieved(object sender, SerialDataReceivedEventArgs e){
			try
			{
				int data = _serialport.ReadChar();
				Console.WriteLine("Data Read = " + data);
				if (data == zero_ASCII)
				{
					ctrl_data = new int[6] { 0, 0, 0, 0, 0, 0 };
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
				else{
					// do something	
				}


				Console.WriteLine("X = " + ctrl_data[X_DAT]);
				Console.WriteLine("Y = " + ctrl_data[Y_DAT]);
				Console.WriteLine("Z = " + ctrl_data[Z_DAT]);
				Console.WriteLine("U = " + ctrl_data[U_DAT]);
				Console.WriteLine("V = " + ctrl_data[V_DAT]);
				Console.WriteLine("W = " + ctrl_data[W_DAT]);
			}catch(Exception ex){
				Console.WriteLine(ex.Message);
			}

		}

	}
}
